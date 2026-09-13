# Greenfield implementation record

Validated on 2026-09-13. Branch: `dev/initial/greenfield`.
Base: `docs/ReadMe` at `fe0601f31f388adfe4d7b4b6a76c098eee83053c`.
Import source: `origin/upstream` at `eadcd567fd8ea801cb346aaed4ff44ab42b6928c`.
Robust Toolbox remains unchanged at `edf061e7450a4074f173e3000bf1552b6f54082f` / `v289.0.0`.

## Checkpoints

- Branch/baseline inventory: complete. Existing branches, history, and untracked `.codex/` configuration preserved.
- Replacement/removal and sandbox/connections: complete in local commit `4756c8d320`.
- Packaging, review fixes, CI, and developer instructions: complete in local commit `8f48a8ebf8`. The build/test evidence below corresponds to this source; the subsequent record commit changes documentation only.
- Executable local validation and independent review: complete, with the inherited engine limitation and unavailable manual gates recorded below. No skipped gate is a pass.
- Delivery is local. No push, PR, merge, deployment, engine patch, or upstream history rewrite was performed.

## Removal and retention

42,572 tracked upstream files are deleted relative to `docs/ReadMe`. Upstream gameplay, databases, rounds/lobbies, administration, chat, integrations, maps, audio, obsolete tools, publishing/bot workflows, and their resource trees physically leave the branch. Content compilation does not hide the removed systems or tolerate missing prototypes.

Retained dependencies and reasons:

- `Content.Client`, `Content.Server`, and `Content.Shared`: minimal entry points, connection interface, arena, pawns, movement, and replicated switch state.
- `Content.Packaging`: the existing Robust asset pipeline for Magic ACZ and Hybrid ACZ, including the server-side development packaging adapter. Client delivery contains only `Content.Client.dll` and `Content.Shared.dll`; no database assembly assumptions remain.
- `Content.Tests` and `Content.IntegrationTests`: NUnit, NUnit adapter, and Microsoft.NET.Test.Sdk for behavioral checks; Robust's integration harness for actual content startup and deterministic multiplayer transport.
- Robust engine projects and their required solution configuration mappings: rendering, UI, input, prediction, physics, networking, authentication, serialization, packaging, generators, and the test harness. Transitive engine dependencies remain pinned; removing content databases does not remove engine-internal SQLite or other engine dependencies.
- `Robust.Shared.AuthLib`: existing username/authentication behavior. `JetBrains.Annotations`: compile-time annotations required by content/engine tooling. Package versions come from the pinned engine's central package file.
- Checker-piece RSI, including its four existing states: CC-BY-SA-3.0, Zumorica/Fishfish458/Capnsockless. White floor texture: CC0. Original asset notices and upstream MIT copyright remain; server archives also include Robust's MIT, GPLv3, asset-license, and legal notices.
- Existing solution name, launch scripts, Rider configuration, and reduced VS Code configurations. README project description and AI/contribution/artwork policies are preserved.

The sandbox creates 256 floor tiles inside a 16×16 footprint, perimeter walls, one interior obstacle, and one switch. Movement uses Robust input/prediction/physics at four units per second. Interactions check live session, attached pawn ownership, map, range, obstruction, and press edges. Disconnect deletes the pawn; reconnect creates a fresh pawn; world/switch state lasts until shutdown. No game-mode, persistence, inventory, combat, or general interaction framework was added.

## Build and behavioral evidence

Host SDK: .NET `10.0.302`, runtime `10.0.10`. Windows x64 was tested directly. Linux x64 was tested in Ubuntu under WSL using an isolated source copy and SDK, without reusing Windows build outputs.

| Platform | Configurations | Unit tests per configuration | Integration tests per configuration | Content warnings / build errors |
| --- | --- | --- | --- | --- |
| Windows x64 | Debug, DebugOpt, Release | 40 passed | 4 passed | 0 / 0 |
| Linux x64 | Debug, DebugOpt, Release | 40 passed | 4 passed | 0 / 0 |

Windows final run: `./Tools/verify.ps1 -SkipPackaging`, with fresh content `bin/` before each restore/build. Logs, binlogs, and TRX files: `artifacts/verification/`; command summary: `artifacts/verify-windows-final-tests.log`. The preceding full script run also passed Windows packaging; its production source matches the final code, with the later restart/map test additions validated by the final build/test run.

Linux run: `wsl -d Ubuntu -- bash artifacts/run-linux-validation.sh`. This local evidence script takes a fresh source snapshot, runs `dotnet build SpaceStation14.slnx -c <configuration>` and both `dotnet test <project>/<project>.csproj -c <configuration> --no-build` commands for all three configurations, then packages and probes Linux natively. Logs/TRX: `artifacts/linux-validation/`; summary: `artifacts/verify-linux-final.log`. On another Linux host, the checked-in `./Tools/verify.ps1` supplies the reproducible equivalent, including clean output and warning checks.

The 40 unit cases cover all movement input combinations, normalized speed, endpoint parsing, and packaging arguments. The four integration tests cover:

- Two clients through real content startup: distinct ownership, local prediction before server acknowledgement, owner/observer convergence, opposing/diagonal/released input, wall collision, and pawns passing through each other.
- Replicated toggles and rejection of held duplicates, wrong ownership, null/disconnected sessions, distance, obstruction, another map, and deleted targets. Three reconnects verify pawn deletion, physical held-key cleanup, fresh input, continued movement, and replicated interaction.
- Engine sandbox validation for both client content assemblies, and two independent server starts verifying arena dimensions and initial switch reset.
- A real UDP connection-screen test: validation, late engine username override, three cancellation/retry cycles, timeout/failure, and retry controls.

Additional actual-process networking: simultaneous cold clients in independent executable/resource directories reached `InGame` over IPv4 and bracketed IPv6, with no warnings/errors. Reproduction script: `artifacts/check-isolated-clients.ps1`; result: `artifacts/network-isolated-result.log`; client/server logs: `artifacts/network-isolated-d3c9d0c87a48427a85f452d900036862/`. These are headless networking checks, not graphical observations.

Inherited engine warnings are recorded separately. Baseline client DebugOpt build: 432 warnings, zero errors. Final solution warning counts (Debug / DebugOpt / Release): Windows **59 / 121 / 123**; Linux **125 / 125 / 127**. Every warning line in these final build logs originates under `RobustToolbox`; counts vary with configuration and incremental engine compilation. The engine was not patched to suppress them. Unexpected runtime warnings fail the integration and package checks.

## Packaging and download evidence

Reproduce with:

```sh
dotnet run --project Content.Packaging -c DebugOpt -- server --hybrid-acz --platform win-x64 --platform linux-x64 --log-build
python Tools/verify_packages.py --package release/SS14.Server_win-x64.zip --extract /path/to/fresh-directory --serve --check-update --evidence artifacts/package-check
```

Choose the host's native package for `--serve`. Each platform package was extracted outside the source resource tree, started successfully, and served all 12 client files with hashes matching its BLAKE2b manifest. The probe modifies only the extracted client ZIP, restarts the server, verifies a changed manifest and complete download, then restores that extracted ZIP. Original release archives are unchanged. These are actual HTTP download-protocol checks; they do not exercise the launcher application.

| Delivery | Result and local evidence |
| --- | --- |
| Development Magic ACZ | Passed metadata, manifest, and 12 downloaded files; `artifacts/magic-acz-final/`, `artifacts/verify-magic.log` |
| Windows Release Hybrid ACZ | 145 archive entries; external startup, download, and changed-build checks passed; `artifacts/verification/package-win-x64/` |
| Native Linux Release Hybrid ACZ | 144 archive entries; external startup, download, and changed-build checks passed; `artifacts/linux-validation/package-linux-x64/` |

All checked metadata reports engine `289.0.0`, fork `astral-reach`, and the actual manifest hash as content version. Loopback net/status bindings, normal Required authentication with localhost allowance, disabled hub/UPnP, assembly/resource closure, and included licenses are checked.

Final local archives and SHA-256:

- `release/SS14.Client.zip`: `3E504A2167FDFA9F4BF079300468B62E552B90D924B195B1630DBB2B1B8A6BDF`
- `release/SS14.Server_win-x64.zip`: `C49D1ECFEB4088C61EC2ABEB9CC67C79D5409BFFC54929810E57ED3F039B1123`
- `release/SS14.Server_linux-x64.zip`: `242EF4FA0DAF911C62FD92684066FA220F31DF62273AAA699E60DBE9142B6E25`

Archives, raw logs, and local runner scripts are ignored build evidence, not committed binaries. Checked-in verification tools and this record preserve reproducible commands and results.

## Independent review

Authorized runtime/networking and connection/cleanup/packaging reviewers examined bounded changes and then the aggregate final diff against `docs/ReadMe`. All actionable findings were resolved and affected checks rerun. Fixes included viewport input forwarding, reconnect-safe tile registration, meaningful ownership/prediction/observer assertions, cancellation/retry state, pre-socket server configuration, sandbox-safe endpoint handling, late launcher username initialization, redial handling, solution configuration mappings, engine license packaging, and Linux path casing. Both final aggregate reviews reported **no remaining actionable correctness findings**. `git diff --check` passed; the engine gitlink and source remain unchanged.

## Limitations and pending gates

1. **Inherited engine cache race — reproduced, with tested workaround.** Two simultaneous cold direct clients sharing the same executable directory can fail in `RobustMappedStringSerializer.WriteStringCache` with `System.IO.IOException: ... because it is being used by another process.` Failed attempt: `artifacts/NetworkBobby.log`. To reproduce, use a fresh common client output with no `strings-*` cache and start two direct clients concurrently. A sequential retry reached `InGame`; independent executable directories also passed concurrently. Robust's launcher-loader path disables this cache internally. Content has no public setting for it, and patching the pinned engine is outside scope. See [the development workaround](docs/verification.md#pinned-engine-development-cache-limitation).
2. **Graphical smoke test — pending.** A client process started, but the computer-use window inspection returned `Computer Use app approval timed out`. No rendered menu/arena, camera, visible collision, toggle feedback, or graphical reconnect result was observed. Start the graphical client in an accessible desktop session and execute steps 1–3 of the [manual checklist](docs/verification.md#manual-graphical-and-launcher-gates), recording screenshots/observations. Headless passes do not satisfy this gate.
3. **Actual launcher/authentication/redial — pending.** No accessible installed/configured launcher was available. Complete the standard launcher's development Magic ACZ and extracted-package Hybrid ACZ download → connection → sandbox → reconnect flows, including a changed content build and authenticated launch, using checklist steps 4–5. Protocol integrity and localhost authentication tests do not establish this pass.
4. **Remote Windows/Linux CI — pending.** The checked-in workflow runs all configurations, both test projects, native packaging/download checks, and uploads diagnostics. Local WSL and Windows passes do not imply the workflow has run remotely; delivery deliberately stops at local commits.

Setup, controls, direct connect, launcher commands, packaging, and selective upstream import guidance are in [README.md](README.md), [CONTRIBUTING.md](CONTRIBUTING.md), and [docs/verification.md](docs/verification.md). Preserve import provenance and licenses, inspect dependency closure, expect conflicts with deleted systems, and test each imported feature.
