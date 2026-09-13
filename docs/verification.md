# Greenfield verification

Run `./Tools/verify.ps1` from the repository root with .NET 10, PowerShell 7, and Python 3.9+. It verifies the exact engine pin, restores/builds Debug, DebugOpt, and Release from fresh content output directories, runs both test projects in every configuration, and packages the host platform in Release. It extracts the package to a fresh temporary directory, verifies every Hybrid ACZ download against the BLAKE2b manifest, changes one packaged resource, and checks the new manifest/download. Evidence goes in `artifacts/verification/`; archives go in `release/`. Stop game processes before executing it. Extracted directories remain for diagnosis. `-SkipPackaging` runs only build/test checks; record that limitation.

Individual checks:

```sh
dotnet build SpaceStation14.slnx -c DebugOpt
dotnet test Content.Tests/Content.Tests.csproj -c DebugOpt --no-build
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj -c DebugOpt --no-build
dotnet run --project Content.Packaging -c DebugOpt -- server --hybrid-acz --platform win-x64 --platform linux-x64
python Tools/verify_packages.py --package release/SS14.Server_win-x64.zip --extract /path/to/fresh-directory --serve --check-update --evidence artifacts/package-check
```

Choose a native package for `--serve`; omitting it performs archive inspection only. While a development server runs, use `python Tools/verify_packages.py --server-url http://localhost:1212 --native-dir bin/Content.Server --evidence artifacts/magic-acz` to verify Magic ACZ. The native directory supplies the engine's zstd library; no Python packages are needed.

Tests cover movement math and endpoint parsing; two clients through actual content startup with deterministic test transport; movement, stopping, collision, replication, ownership, interaction rejection, and repeated reconnect cleanup; engine client sandbox checks; and a separate real-UDP menu test for invalid input, username overrides, cancellation, failure, and retry. Unexpected runtime warnings fail tests. Engine build warnings remain visible in logs. These checks do not prove graphical behavior or the launcher's authentication/download/redial flow.

## Manual graphical and launcher gates

1. Start the development server and two graphical clients. Inspect the menu at normal display scale. Test typing, selection, paste, invalid name/address messages, an unavailable endpoint, cancellation, retry, quit, and connection-loss reasons.
2. Connect both clients. Verify the enclosed 16×16 arena, interior wall, checker pawns, readable controls, camera follow, four-unit speed, normalized diagonals, immediate release, wall collision, and pawns passing through each other.
3. Approach the orange object and press E once: both clients see green. Holding E must not repeat. Beyond two units or behind a wall, E has no effect. Disconnect while moving and reconnect three times: old pawns disappear, new pawns have no held input, and toggle state persists. Restart the server and confirm the initial world returns.
4. In the standard launcher, Direct Connect to `ss14://localhost:1212`. Confirm engine 289.0.0 and fork `astral-reach`, enter the sandbox, and reconnect through the launcher. Preserve normal authentication; localhost allowance belongs to the engine.
5. Stop the development server. Extract the native Release package outside the checkout, run `dotnet Robust.Server.dll` there, and repeat launcher download → connection → sandbox → reconnect. Modify and rebuild retained content, restart, and verify the launcher downloads the new version. Inspect `/info`, `/manifest.txt`, downloaded resources, and logs.

Record OS, commit, engine pin, commands, screenshots or observations, test counts, warnings, and failures. Keep blocked gates explicitly pending with the exact error and reproduction. Local CI configuration and tests do not mean remote CI executed.

### Pinned-engine development cache limitation

Two direct clients started simultaneously from the same executable directory with an empty string-table cache can race in Robust's `RobustMappedStringSerializer.WriteStringCache`, producing an `IOException` and incomplete handshake. Start the first direct client through its initial connection before starting the second, or use separate executable directories. This cache is disabled by Robust's actual launcher loader. Content cannot access its internal cache setting, and this branch does not patch the pinned engine. Preserve the failed log when this occurs; do not call that attempt a pass.

## Review gates

Review runtime/networking and cleanup/packaging/tests independently, then review the aggregate final diff after fixes. Resolve actionable correctness findings and rerun affected checks. Verify no database/gameplay dependencies remain in content archives, the engine gitlink is unchanged, and `.codex/` is untouched. Do not push, publish, merge, or deploy as part of local verification.
