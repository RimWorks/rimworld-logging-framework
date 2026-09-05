# RimLogging

[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=RimWorks_rimworld-logging-framework&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=RimWorks_rimworld-logging-framework)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=RimWorks_rimworld-logging-framework&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=RimWorks_rimworld-logging-framework)

<img src="https://raw.githubusercontent.com/RimWorks/rimworld-logging-framework/main/About/ModIcon.png" alt="RimLogging icon" width="96" align="right">

A public, structured logging framework for RimWorld 1.6+ mods. It replaces vanilla `Verse.Log` and `UnityEngine.Debug.Log` with one structured, filterable pipeline that every dependent mod shares.

- Hierarchical channels (XML defs or transient) with prefix-based resolution.
- Serilog-style templated messages plus anonymous-object structured context.
- Six levels: `Trace`, `Debug`, `Info`, `Warn`, `Error`, `Fatal`.
- Multi-sink output (Verse writeback, rolling text/NDJSON files, in-memory) with a plugin sink API.
- Lock-free MPSC queue with a background drain; synchronous bypass for `Error` / `Fatal`.
- Three-pane in-game log viewer with a channel tree and an expression filter DSL. The detail pane docks to the bottom, docks to the right, or pops out into its own window.
- A Roslyn analyzer in the NuGet package that flags leftover `Verse.Log` calls at build time.

![RimLogging preview card](https://raw.githubusercontent.com/RimWorks/rimworld-logging-framework/main/About/Preview.png)

## Install

As a Steam Workshop dependency (recommended for shipped mods): end users install the Workshop mod once and every consumer mod shares one dll. Declare it in your `About/About.xml`:

```xml
<modDependencies>
    <li>
        <packageId>RimWorks.RimLogging</packageId>
        <displayName>RimLogging</displayName>
        <steamWorkshopUrl>steam://url/CommunityFilePage/REPLACE_WITH_WORKSHOP_ID</steamWorkshopUrl>
    </li>
</modDependencies>
<loadAfter>
    <li>RimWorks.RimLogging</li>
</loadAfter>
```

Or bundle it from NuGet. The dll (and its `System.Text.Json` runtime dependencies) are copied into your mod's `Assemblies/` at build time, so you don't need the Workshop dependency:

```
dotnet add package RimWorks.RimLogging
```

## Usage

```csharp
using RimWorks.RimLogging;

// Templated message with positional args.
Log.Info("colony {Name} founded at {Tile}", colony.Name, colony.Tile);

// Structured context from an anonymous object.
Log.Warn("low food", new { pawn, days_left = 2 });

// Exceptions: pass the exception first, or fold it into the context.
Log.Error(ex, "save failed");

// Tick-loop throttling: once ever, or at most once per interval. The key is global.
Log.WarnOnce("no-power", "the reactor has no power");
Log.WarnEvery("no-power", TimeSpan.FromMinutes(1), "the reactor still has no power");

// Explicit channel.
Log.Info("Cosmere.Roshar.Surgebinding", "bond formed", new { spren = spren.Label });

// Explicit channel + templated args: pass an explicit array so the channel
// overload is selected (a bare trailing value binds as structured context).
Log.Info("Cosmere.Roshar.Surgebinding", "bond formed with {Spren}", new object?[] { spren.Label });
```

The first string argument is the channel only when a later argument disambiguates the overload; `Log.Info("text", arg)` treats `"text"` as the message. Formatting is lazy: if no registered sink accepts the entry's level, the template is never rendered.

## Docs

- [Channels](docs/channels.md): ChannelDef XML, field reference, prefix resolution, built-in channels.
- [Filter DSL](docs/filter-dsl.md): grammar and examples for the viewer's filter box.
- [Custom sinks](docs/sinks.md): the `ILogSink` API, SinkDef XML, built-in sinks.
- [Verse.Log analyzer](docs/analyzer.md): the `RIMLOG001` and `RIMLOG002` build warnings, and what to call instead.
- [Settings and bug bundles](docs/settings.md): the mod settings page and the share-bundle payload.
- [Bundle upload worker](worker/README.md): the Cloudflare Worker behind bug-report uploads.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for build commands, commit conventions, and translation notes.

MIT licensed.
