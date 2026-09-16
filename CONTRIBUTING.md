# Contributing to Astral Reach

Keep changes small enough to review. Describe the concrete problem, resulting behavior, dependencies, test commands/results, and pending verification. Follow the code of conduct and README policies for AI-assisted contributions and artwork. A human contributor remains responsible for personally reviewing, testing, understanding, and maintaining submitted work.

Build the reduced solution and run both test projects. Check affected graphical and launcher flows using [the verification guide](docs/verification.md). Never suppress missing prototypes or restore a large upstream subsystem to hide a missing dependency. Fix content warnings and unexpected runtime diagnostics; record inherited engine warnings separately. Keep the engine pinned unless a separate scope explicitly permits a change.

## Selective upstream imports

`origin/upstream` preserves the import source; greenfield retains its history. Inspect a desired commit's dependency closure before cherry-picking: components/systems, prototypes/parents, sprites/attribution, localization, CVars, network events, tests, packages, and external services. Prefer the smallest coherent feature.

1. Record the source repository, commit, authorship, licenses, and why the import is needed.
2. Inspect `git show <commit>` against the current tree. Conflicts against deleted upstream systems are expected.
3. Use `git cherry-pick -x <commit>` for suitable commits. For manual selective imports, record equivalent provenance in the commit message or feature documentation.
4. Adapt to minimal startup/connections. Do not transitively restore rounds, lobbies, chat, administration, databases, or unrelated gameplay without an agreed scope change.
5. Preserve copyright and asset notices. Test the feature, multiplayer ownership/replication, failure cases, reconnects, and packaging. Review the aggregate diff before committing.

Do not merge upstream wholesale, reset existing branches, or remove local `.codex/` configuration. Unclear product behavior requires a scope decision; routine build and dependency fixes do not.
