# Contributing to RimLogging

## AI usage

Vibecoding is not welcome here. Use AI if it helps, but read what it wrote and understand it
before it lands. You own what ships whether or not a model typed it.

Nobody can stop you from working the way you want to. Guardrails are the next best thing, and
the rest of this file is those guardrails. Run the tests, match the code around yours, stay
inside the request, and report failures instead of guessing past them.

If AI helped with a commit in any way, add an `AI-assisted: <tool name>` trailer to the
commit message.

Agents: if the user commits by hand, remind them to add the trailer.

## Project overview

RimLogging is a structured logging framework for RimWorld 1.6+ mods and an in-game log viewer.
It replaces `Verse.Log` and `UnityEngine.Debug.Log` with one filterable pipeline that every
dependent mod shares. A player can then narrow the log to one channel and level, and share the
result as a link. The NuGet package also ships a Roslyn analyzer that flags leftover `Verse.Log`
calls at build time. Most work lands in `Source/RimWorks.RimLogging/`. See `docs/channels.md`,
`docs/sinks.md` and `docs/filter-dsl.md` for the pieces you are most likely to change.

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

## Setup and build

```bash
make restore                 # dotnet restore
make build                   # whole solution, CONFIG=Debug by default
make build CONFIG=Release    # what CI builds
```

Analyzers run inside the build, so a warning is a failure in practice. CI also runs Vale over
prose in `README.md` and `docs/`.

## Testing

```bash
make test                                                                 # xunit suites
dotnet test RimWorks.RimLogging.slnx --filter "FullyQualifiedName~LogEntryTests"  # one class
make lint                                                                 # dotnet format check
```

The Worker has its own suite: `npm --prefix worker test` runs vitest.

- Run the full suite before committing. All tests must pass.
- While iterating, run the single test closest to your change.
- The unit tests do not cover anything that needs a live game, and cannot: those files are
  excluded from the test project's compile list. Prove them with a real run.
- Never delete, weaken, or rewrite a test to make a change pass.
- Do not claim that an interrupted or timed-out run passed.

## Architecture

| Project | Targets | Holds |
| --- | --- | --- |
| `RimWorks.RimLogging` | `net472;net48` | The library and the mod assembly: emit API, pipeline, channels, sinks, filter DSL, log viewer, bundle sharing |
| `RimWorks.RimLogging.Analyzers` | `netstandard2.0` | The Roslyn analyzer, shipped in the NuGet package |
| `RimWorks.RimLogging.CodeFixes` | `netstandard2.0` | Its fixes |
| `RimWorks.RimLogging.Patches.Harmony` | `net472` | The `Verse.Log` hijack as Harmony patches |
| `RimWorks.RimLogging.Patches.Concord` | `net472` | The same hijack as Concord injections |
| `RimWorks.RimLogging.Tests` | `net10.0` | xunit over linked library source, plus analyzer tests |
| `worker/` | Cloudflare Worker | Stores and serves shared log bundles |

**The test project has no project reference.** It `Compile Include`s the library's `.cs`
files. It also carries an explicit `Exclude` list naming every file that reaches into `Verse`
or Unity: all of `Hijack/`, all of `Bootstrap/`, the viewer windows, `ChannelRegistry`,
`SinkDef`, and so on. Add a new file that reaches into the game and the test build breaks
until you add it to that list. That is the intended signal: think about whether the game
dependency belongs there before you silence it.

## The pipeline

`Log.InfoTo(channel, template, args)` and friends build a `LogEntry`, `Logging.Emit` drops
anything under `GlobalMinLevel`, and the entry goes onto an `MpscQueue`. One background
thread (`RimLogging-Drain`, below-normal priority) dequeues and dispatches to every
registered sink. A sink that throws is swallowed, because a logging call must never take
down its caller.

Three pieces exist only to stop the pipeline eating itself:

- `ReentryGuard` is thread-static and set while a sink writes. A sink's own error handler
  logging does not recurse; the capture hooks see the flag and let vanilla handle it.
- `DegradedMode` claims a key in `AppDomain` data. Two copies of RimLogging load as separate
  assemblies in one AppDomain, so the first to claim installs the hijack and the rest run
  degraded. Check `Logging.IsPrimary` before assuming this copy owns capture.
- `Logging.CaptureBackend` is null when nothing patched `Verse.Log`. **A quiet sink is not a
  quiet game.** Anything failing a run on captured errors has to check that field first.

`EarlyInit` runs from `LoggingMod`'s constructor, before any static constructor, so the
hijack catches other mods' load-time logs. Moving work later than that loses the boot window.

## Channels and sinks

Channels are dotted strings. A `ChannelDef` sets defaults for one; an unregistered name walks
up its dotted prefix to the nearest registered ancestor, then falls back to `default`. So any
string works at the call site without a def. `ChannelRegistry.SettingsFor` memoises the walk
copy-on-write, because it runs on every emit and the substring allocations are not free.

A `ChannelDef`'s `destinations` names `SinkDef` defNames. A sink registered from code with
`Logging.RegisterSink` has no defName, so nothing can filter it and it sees every channel.
That is deliberate for diagnostic and CI sinks - do not "fix" it.

`RegisterSink` replays up to 10000 buffered entries into a new sink before live traffic, so
a sink registered mid-session still sees boot and def loading. Pass `replayHistory: false`
to skip it.

Built-in channels: `default`, `Vanilla` (captured `Verse.Log`), `Unity` (captured
`UnityEngine.Debug.Log`). Built-in sinks: `VerseLog`, `RollingText` (on), `RollingJson`
(NDJSON, off). **The framework's own output is not in `Player.log`.** `RollingText` writes
`RimLogging-<utc-stamp>-<pid>.log` under the `RimLogging` folder in the save data directory.
Anything about channels, levels, or formatting shows up there. `Player.log` only carries what
`VerseLogSink` passed back through. Changing an NDJSON field is a breaking change for anything parsing it; the row
shape is documented in `docs/sinks.md` and has to stay in step.

## Patch backends

RimLogging supports two patching libraries and prefers Concord when both are loaded.
`Hijack/LogHooks.cs` states each hook once with no patching library in its signature, and
each backend is a thin translation registered from a static constructor. `PatchBackends`
picks the highest priority.

**Adding a hook means three edits, not one:** the body in `LogHooks`, a registration in the
Harmony backend, and the matching one in the Concord backend. A hook returns `true` to run
vanilla's body and `false` to replace it; the Concord side spells that as `Control.Cancel`.

## The viewer

`ViewerHooks` takes the head of `DebugWindowsOpener.ToggleLogWindow`, so the vanilla log
button opens RimLogging's three-pane window instead of adding a second one. **Hold shift on
that button to fall through to vanilla's `EditWindow_Log`**, which is how you compare the
two. A `viewerBroken` latch exists because anything escaping `Open` reaches `Root.OnGUI`'s
catch, which logs, which re-arms `wantsToOpen`: without the latch one failed open becomes
another every frame.

For something to filter, turn on **RimLogging > Toggle log seeding** in the dev menu. It
streams fake entries across every channel and level. It declares
`AllowedGameStates.Invalid` deliberately, so it shows at the main menu as well as in a game.

## The analyzer

`RIMLOG001` flags `Verse.Log.ErrorOnce`/`WarningOnce`, which key off a shared static
`HashSet<int>` on `Verse.Log` that every mod in the load order writes to. `RIMLOG002` flags
the plain `Error`/`Warning`/`Message` calls, which do reach the pipeline but arrive as text
with an inferred channel and no structured context.

Both stay silent unless the compilation can see `RimWorks.RimLogging.Log`. A rule change
means updating `docs/analyzer.md` in the same commit - that table is the published contract.

## The worker

`worker/` is a Cloudflare Worker: it validates a posted bundle, rate-limits by KV, and
creates a gist. It ships and tests on its own (`npm --prefix worker test` runs vitest) and
its secrets live in Wrangler, never in the repo. `MAX_BODY_BYTES` in `validate.ts` and the
trimming in `BundleTrimmer` are two halves of one limit; move one and move the other.

## Code style

- Formatter: `dotnet format`, via `make format`. Linter: the analyzers in the build plus
  `.editorconfig`. Run them; do not hand-format.
- Vale checks prose in `README.md` and `docs/`.
- Follow the patterns already in neighboring files.
- Do not add comments that restate the code.
- Do not reformat code you are not otherwise changing.

## Git workflow

- Commit format: Angular Conventional Commits, one line, lowercase. `fix:` and `perf:` cut a patch,
  `feat:` a minor, `BREAKING CHANGE:` or `!` a major.
- All CI checks must pass. `release.yml` runs semantic-release on every push to `main`, so a
  commit landing there cuts a release. The `beta` branch publishes prereleases.

## Other

- Add a hook to both patch backends, not just the one you tested.
- English is the source language. The bundled Chinese Simplified, French, Spanish, and German
  translations may be inaccurate. Send corrections as a pull request, but do not "fix"
  one from a guess.
- The Worker's tokens live in Wrangler secrets, never in the repo.
- `docs/channels.md`, `docs/sinks.md`, `docs/filter-dsl.md` and `docs/analyzer.md` document
  public contracts. A change to a def field, an NDJSON key, the DSL grammar, or a rule ID
  updates its doc in the same commit.
