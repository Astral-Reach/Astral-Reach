"""Inspect Hybrid ACZ archives and verify the real download protocol. Python 3.9+, no packages.

This is a protocol/package smoke test, not an end-to-end launcher or graphical test.
"""
import argparse
import configparser
import ctypes
import hashlib
import io
import json
from pathlib import Path, PurePosixPath
import struct
import subprocess
import sys
import time
import urllib.request
import zipfile


def checked_names(archive):
    names = archive.namelist()
    assert len(names) == len(set(names)), "Duplicate archive entries"
    for name in names:
        path = PurePosixPath(name)
        assert not path.is_absolute() and ".." not in path.parts and "\\" not in name and ":" not in name
    return set(names)


def check_client(archive):
    names = checked_names(archive)
    assemblies = {n for n in names if n.endswith(".dll")}
    assert assemblies == {"Assemblies/Content.Client.dll", "Assemblies/Content.Shared.dll"}, assemblies
    assert {"manifest.yml", "keybinds.yml", "Prototypes/__merged.yml", "LICENSE.TXT"} <= names
    assert "Textures/Objects/Fun/Tabletop/checker_pieces.rsic" in names
    assert not any(n.startswith(("Maps/", "Audio/", "Changelog/", "ServerInfo/")) for n in names)
    return names


def get(url, body=None, headers=None):
    request = urllib.request.Request(url, data=body, headers=headers or {})
    with urllib.request.urlopen(request, timeout=15) as response:
        return response.read()


def download(url, evidence, native_dir):
    evidence.mkdir(parents=True, exist_ok=True)
    info_bytes = get(url + "/info")
    info = json.loads(info_bytes)
    build = info["build"]
    assert build["engine_version"] == "289.0.0" and build["fork_id"] == "astral-reach" and build["acz"]
    manifest = get(url + "/manifest.txt")
    manifest_hash = hashlib.blake2b(manifest, digest_size=32).hexdigest().upper()
    assert build["manifest_hash"].upper() == manifest_hash == build["version"].upper()
    lines = manifest.decode("utf-8-sig").splitlines()
    assert lines.pop(0) == "Robust Content Manifest 1"
    entries = [line.split(" ", 1) for line in lines]
    assert len(entries) == len({name for _, name in entries})
    response = get(url + "/download", b"".join(struct.pack("<i", i) for i in range(len(entries))),
                   {"Content-Type": "application/octet-stream", "X-Robust-Download-Protocol": "1"})
    stream = io.BytesIO(response)
    flags, = struct.unpack("<i", stream.read(4))
    assert flags in (0, 1)
    zstd = None
    with zipfile.ZipFile(evidence / "downloaded-client.zip", "w", zipfile.ZIP_DEFLATED) as downloaded:
        for expected_hash, name in entries:
            size, = struct.unpack("<i", stream.read(4))
            compressed, = struct.unpack("<i", stream.read(4)) if flags else (0,)
            data = stream.read(compressed or size)
            assert len(data) == (compressed or size)
            if compressed:
                if zstd is None:
                    runtime_dir = native_dir / "runtimes" / ("win-x64" if sys.platform == "win32" else "linux-x64") / "native"
                    search_dir = runtime_dir if runtime_dir.exists() else native_dir
                    candidates = [p for p in search_dir.rglob("*zstd*") if p.name in ("zstd.dll", "libzstd.dll") or p.name.startswith("libzstd.so")]
                    assert candidates, f"No native zstd library under {native_dir}"
                    zstd = ctypes.CDLL(str(candidates[0].resolve()))
                    zstd.ZSTD_decompress.argtypes = [ctypes.c_void_p, ctypes.c_size_t, ctypes.c_void_p, ctypes.c_size_t]
                    zstd.ZSTD_decompress.restype = ctypes.c_size_t
                output = ctypes.create_string_buffer(size)
                assert zstd.ZSTD_decompress(output, size, data, len(data)) == size
                data = output.raw
            assert hashlib.blake2b(data, digest_size=32).hexdigest().upper() == expected_hash.upper(), name
            downloaded.writestr(name, data)
        assert not stream.read(), "Trailing download data"
    with zipfile.ZipFile(evidence / "downloaded-client.zip") as downloaded:
        check_client(downloaded)
    (evidence / "info.json").write_bytes(info_bytes)
    (evidence / "manifest.txt").write_bytes(manifest)
    print(f"Verified {len(entries)} downloaded files; manifest {manifest_hash}", flush=True)
    return manifest_hash


def serve(directory, evidence):
    evidence.mkdir(parents=True, exist_ok=True)
    with (evidence / "server.log").open("w", encoding="utf-8") as log:
        process = subprocess.Popen(["dotnet", "Robust.Server.dll", "--data-dir", "test-data"],
                                   cwd=directory, stdout=log, stderr=subprocess.STDOUT,
                                   creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0))
        try:
            for _ in range(120):
                assert process.poll() is None, "Packaged server exited; inspect server.log"
                try:
                    get("http://localhost:1212/status")
                    break
                except (OSError, TimeoutError):
                    time.sleep(0.25)
            else:
                raise AssertionError("Packaged server status did not start")
            result = download("http://localhost:1212", evidence, directory)
        finally:
            process.terminate()
            process.wait(timeout=15)
    text = (evidence / "server.log").read_text(encoding="utf-8")
    assert "[ERRO]" not in text and "[WARN]" not in text, "Unexpected runtime diagnostic; inspect server.log"
    assert "found client zip:" in text, "Hybrid ACZ was not used"
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--package", type=Path)
    parser.add_argument("--extract", type=Path)
    parser.add_argument("--serve", action="store_true")
    parser.add_argument("--check-update", action="store_true")
    parser.add_argument("--server-url")
    parser.add_argument("--native-dir", type=Path)
    parser.add_argument("--evidence", type=Path, required=True)
    args = parser.parse_args()
    if args.server_url:
        assert args.native_dir is not None
        download(args.server_url.rstrip("/"), args.evidence, args.native_dir)
        return
    assert args.package and args.extract
    assert not args.extract.exists(), "Extract into a fresh directory"
    with zipfile.ZipFile(args.package) as server:
        names = checked_names(server)
        assert {"Robust.Server.dll", "Content.Client.zip", "server_config.toml", "LICENSE.TXT"} <= names
        assert {"Licenses/RobustToolbox/" + notice for notice in ("LICENSE-MIT.TXT", "LICENSE-GPLv3.TXT", "LICENSE-ASSETS.TXT", "legal.md")} <= names
        assert {"Resources/Assemblies/Content.Server.dll", "Resources/Assemblies/Content.Shared.dll"} <= names
        assert not any("Content." in n and "Database" in n for n in names)
        # The retained preset uses only flat sections with scalar values, an INI-compatible TOML subset.
        config = configparser.ConfigParser(interpolation=None)
        config.read_string(server.read("server_config.toml").decode("utf-8-sig"))
        assert config["net"]["bindto"].strip('"') == "127.0.0.1,::1" and not config["net"].getboolean("upnp")
        assert config["status"]["bind"].strip('"') == "localhost:1212" and not config["hub"].getboolean("advertise")
        assert config["auth"].getint("mode") == 1 and config["auth"].getboolean("allowlocal")
        with zipfile.ZipFile(io.BytesIO(server.read("Content.Client.zip"))) as client:
            check_client(client)
        server.extractall(args.extract)
    args.evidence.mkdir(parents=True, exist_ok=True)
    (args.evidence / "archive-entries.txt").write_text("\n".join(sorted(names)), encoding="utf-8")
    print(f"Inspected {args.package}: {len(names)} entries", flush=True)
    if args.serve:
        first = serve(args.extract, args.evidence / "initial")
        if args.check_update:
            client_path = args.extract / "Content.Client.zip"
            original = client_path.read_bytes()
            try:
                with zipfile.ZipFile(io.BytesIO(original)) as old, zipfile.ZipFile(client_path, "w") as changed:
                    for entry in old.infolist():
                        data = old.read(entry)
                        if entry.filename == "manifest.yml":
                            data += b"\n# ACZ changed-content verification\n"
                        changed.writestr(entry, data)
                second = serve(args.extract, args.evidence / "updated")
                assert first != second, "Changed content did not invalidate its download manifest"
            finally:
                client_path.write_bytes(original)


if __name__ == "__main__":
    main()
