# Astral Reach

Astral Reach is a collection of game modes inspired by Space Station 13 and built on [Robust Toolbox](https://github.com/space-wizards/RobustToolbox), our homegrown engine written in C#.

Robust Toolbox separates the engine from server-specific game content through content packs loaded by the client and server. A content pack contains the code and assets necessary to play on a particular server.

This repository contains both Robust Toolbox and the Astral Reach content pack for development. The greenfield branch currently provides a small multiplayer sandbox: a bounded arena, one pawn per player, and a shared toggle object. Game modes are future work.

## Setup and direct connect

Install the .NET 10 SDK and Git. Clone this fork with `--recurse-submodules`, or initialize an existing clone:

```sh
git submodule update --init --recursive
git -C RobustToolbox rev-parse HEAD
dotnet restore SpaceStation14.slnx
dotnet build SpaceStation14.slnx -c DebugOpt --no-restore
```

The engine must remain at `edf061e7450a4074f173e3000bf1552b6f54082f` (v289.0.0). Do not run `submodule update --remote`.

From the repository root, run these in separate terminals:

```sh
dotnet run --project Content.Server -c DebugOpt --no-build
dotnet run --project Content.Client -c DebugOpt --no-build
```

The `runserver` and `runclient` scripts offer the same defaults. Rider's `Content Server+Client` configuration and VS Code's `Server/Client` compound are retained. On Linux, a graphical session and the engine's SDL/OpenGL/audio native dependencies are needed for the client; see the [engine setup guide](https://docs.spacestation14.com/en/general-development/setup.html).

Enter a username (3–32 letters, numbers, or underscores) and `localhost`. Hostnames, IPv4, and bracketed IPv6 accept an optional port, for example `127.0.0.1:1212` or `[::1]:1212`. The interface provides connection progress, Cancel, failure reasons, Retry, Disconnect, Reconnect, and Quit. `--username` and `--connect` engine arguments are supported.

WASD moves at four world units per second, with normalized diagonals and wall collision. Pawns pass through each other. E toggles the nearest unobstructed object within two world units; orange means off and green means on. The camera follows your pawn. Disconnect removes your pawn; reconnect creates a fresh one. The world persists until server shutdown and resets on restart.

The development preset explicitly binds game UDP and HTTP status to loopback on port 1212, disables UPnP and hub advertisement, and retains standard Robust authentication with its localhost development allowance. The server entry point loads this preset before opening sockets. Pass `--config-file <path>` explicitly to select another configuration. Public deployment is outside this branch's scope.

## Launcher and packaging

Use the standard SS14 launcher Direct Connect with `ss14://localhost:1212`. Launcher-provided endpoints and authentication stay under Robust's control. Authenticated names are read-only in the content menu; launcher reconnect/redial is supported. This fork does not modify the launcher.

A development server serves the current built client and retained resources through [Magic ACZ](https://docs.spacestation14.com/en/robust-toolbox/acz.html). Build the client before starting the server. Restart the server after changing client content so its download manifest is rebuilt.

Create Release server archives with bundled client content from the repository root:

```sh
dotnet run --project Content.Packaging -c DebugOpt -- server --hybrid-acz --platform win-x64 --platform linux-x64
```

The packager runs in DebugOpt while building Release content. Keep those configurations separate to avoid rebuilding the running packager. Filenames remain compatible: `release/SS14.Client.zip`, `release/SS14.Server_win-x64.zip`, and `release/SS14.Server_linux-x64.zip`. Both server archives contain `Content.Client.zip` for Hybrid ACZ. Engine metadata is `289.0.0`, fork identifier `astral-reach`; Robust supplies the content manifest hash/version.

Extract a server archive to a fresh directory outside the checkout and run `dotnet Robust.Server.dll` from that directory with .NET 10 installed. The packaged `server_config.toml` has the same loopback defaults. Stop any development server using port 1212 first. On Linux, `dotnet Robust.Server.dll` also avoids depending on ZIP extraction preserving executable bits.

`client` packages the client alone. `--no-wipe-release` preserves other archives; `--log-build` writes diagnostic binlogs. `--skip-build` is only for already matching content and platform publish outputs, never a fresh checkout. By default, packaging replaces generated client/server outputs and `release/`; it refuses an unrelated working directory or redirected output directory.

## Verification and contributions

See [verification commands and manual gates](docs/verification.md), the [implementation record](GREENFIELD.md), and [contribution guidelines](CONTRIBUTING.md). Build all three configurations, run both test projects, inspect fresh packages, and review the complete diff before submitting. Windows/Linux CI performs builds, tests, and native-platform packaging; a local run does not establish a remote CI pass.

Contributions and translations are welcome. Discuss substantial scope changes with the Astral Reach maintainers before implementation. Review the contribution guidelines and retain the policies below.

## AI-generated contributions

Our position and rationale for this policy can be read here: [Our Stance on Generative AI in Development](#).

In line with precedent set by many large and reputable open-source communities, this project permits responsible AI-assisted development, but does not accept low-effort or unreviewed AI-generated contributions. AI-generated artwork is not accepted.

AI-assisted tools may be used when developing code, documentation, or other non-art contributions. However, the human contributor remains the author and is fully responsible for everything they submit. Contributing means vouching for the quality, correctness, license compliance, and suitability of the contribution for inclusion in the project.

AI-generated output must be treated as a suggestion rather than a finished contribution. Contributors are expected to personally review, test, and understand everything they submit. You must be able to maintain, modify, debug, explain, and defend the technical decisions in your contribution without relying on AI to do so on your behalf.

Review is expected to remain a human-to-human process. Contributors must be able to respond to review comments, answer questions about their work, and make requested changes themselves. A contribution may be rejected or closed if its contributor cannot adequately explain or maintain the submitted work.

When generative AI has been used to create or substantively modify a contribution, that use should be disclosed in the pull request or other location where authorship is normally described. Routine assistive use, such as spelling, grammar, translation, or language clarification, does not require disclosure.

AI tools must not be listed as authors, co-authors, contributors, or commit signatories. AI assistance does not transfer authorship, responsibility, or accountability away from the human contributor. For the purposes of this project, the human submitting the work is always the Contributor.

### Artwork

AI-generated visual artwork is not accepted.

This includes sprites, textures, illustrations, icons, concept art, promotional artwork, and other visual assets generated in whole or in substantial part by generative AI systems. Generative AI may not be used as a substitute for an artist when producing visual assets intended for inclusion in the project.

Minor assistive tools that do not generate the underlying artwork may be considered separately. Contributors remain responsible for establishing the authorship, provenance, licensing, and attribution of every submitted asset.

## License and attribution

Content code remains under the [MIT license](LICENSE.TXT), including the upstream copyright notice. Robust Toolbox and its dependencies retain their own licenses in the pinned submodule and distributed engine files.

The retained checker-piece sprites are CC-BY-SA-3.0, attributed to Zumorica, Fishfish458, and Capnsockless in their [RSI metadata](Resources/Textures/Objects/Fun/Tabletop/checker_pieces.rsi/meta.json). The white floor texture retains its [CC0 attribution](Resources/Textures/Tiles/Basic/White/attributions.yml). No other upstream content artwork is included. Preserve these notices when redistributing or selectively importing assets.
