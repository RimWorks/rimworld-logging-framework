# AGENTS.md

## AI usage

We don't vibecode here. Use AI if it helps, but read what it wrote and understand it before
it lands. You own what ships, whether or not a model typed it.

We can't stop anyone from working the way they want to. We can set guardrails so what lands
is as good as it can be. The rest of this file is those guardrails. Run the tests, match the
code around yours, stay inside the request, and report failures instead of guessing past them.

If AI helped with a commit in any way, add an `AI-assisted: <tool name>` trailer to the
commit message.

Agents: if the user commits by hand, remind them to add the trailer.

## Project overview

RimLogging is a structured logging framework for RimWorld 1.6+ mods and an in-game log viewer.
It replaces `Verse.Log` and `UnityEngine.Debug.Log` with one filterable pipeline that every
dependent mod shares, then lets a player narrow the log to one channel and level and share it
as a link. The NuGet package also ships a Roslyn analyzer that flags leftover `Verse.Log`
calls at build time. Most work lands in `Source/RimWorks.RimLogging/`. See `docs/channels.md`,
`docs/sinks.md` and `docs/filter-dsl.md` for the pieces you are most likely to touch.

## Project structure

- `Source/RimWorks.RimLogging/` - the core library and mod assembly: channels, sinks, the
  queue, and the three-pane log viewer
- `Source/RimWorks.RimLogging.Analyzers/`, `.CodeFixes/` - the Roslyn analyzer and its fixes,
  shipped in the NuGet package
- `Source/RimWorks.RimLogging.Patches.Harmony/`, `.Patches.Concord/` - the two patch backends
- `Source/RimWorks.RimLogging.Tests/` - xunit suites
- `worker/` - the Cloudflare Worker that stores and serves shared log bundles
- `docs/` - contributor documentation
- `About/`, `Defs/`, `Languages/`, `Styles/`, `Textures/` - RimWorld mod content

## Setup & build

```bash
make restore                 # dotnet restore
make build                   # whole solution, CONFIG=Debug by default
make build CONFIG=Release    # what CI builds
```

## Testing

```bash
make test                                                                 # xunit suites
dotnet test RimWorks.RimLogging.sln --filter "FullyQualifiedName~LogEntryTests"  # one class
make lint                                                                 # dotnet format check
```

The Worker has its own suite: `npm --prefix worker test` runs vitest.

- Run the full suite before committing. All tests must pass.
- While iterating, run the single test closest to your change.
- The unit tests do not cover anything that needs a live game. Prove those with a real run.
- Never delete, weaken, or rewrite a test to make a change pass.
- Do not claim that an interrupted or timed-out run passed.

## Code style

- Formatter: `dotnet format`, via `make format`. Linter: the analyzers in the build plus
  `.editorconfig`. Run them; do not hand-format.
- Vale checks prose in `README.md` and `docs/`.
- Follow the patterns already in neighboring files.
- Do not add comments that restate the code.
- Do not reformat code you are not otherwise changing.

## Git workflow

- Work on `main`. Outside contributions come as pull requests targeting `main`.
- Commit format: Conventional Commits, one line, lowercase. `fix:` and `perf:` cut a patch,
  `feat:` a minor, `BREAKING CHANGE:` or `!` a major.
- Never commit, push, or open a PR unless asked.
- All CI checks must pass. `release.yml` runs semantic-release on every push to `main`, so a
  commit landing there cuts a release. The `beta` branch publishes prereleases.

## Boundaries

- Do not modify unrelated files or widen scope beyond the request.
- Do not add dependencies without asking.
- Never commit secrets, API keys, or .env files. The Worker's tokens live in Wrangler secrets.
- A hook has to be added to both patch backends, not just the one you tested.
- English is the source language. The bundled translations may be wrong; do not "fix" them
  from a guess.
- If a command fails, report the failure. Do not guess or present assumptions as confirmed
  results.
