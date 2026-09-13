# Greenfield implementation record

Base: `docs/ReadMe` at `fe0601f31f388adfe4d7b4b6a76c098eee83053c`.
Import source: `origin/upstream`. Engine: `edf061e7450a4074f173e3000bf1552b6f54082f` / `v289.0.0`.

## Checkpoints

- Branch created; original branches and untracked `.codex/` preserved.
- Removed 42,579 tracked upstream files; replacement implementation in progress.
- Baseline client DebugOpt build: passed, 432 existing warnings, zero errors.

## Retained dependencies

- Robust Toolbox: engine, rendering, input, networking, prediction, physics, UI, packaging and test harness.
- Content.Client / Server / Shared: reduced entry points and sandbox.
- Content.Packaging: launcher content delivery and portable server archives.
- Content.Tests / IntegrationTests: behavioral and real-content regression checks.
- Existing checker pieces (CC-BY-SA-3.0) and white floor (CC0): sandbox visualization, with attribution.

## Verification

Implementation and validation are in progress. No unrun gate is considered passed.
Remote Windows/Linux CI will remain pending at the local-only handoff.
