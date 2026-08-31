# RimLogging

A public, structured logging framework for RimWorld 1.6+ mods.

RimLogging replaces vanilla `Verse.Log` and `UnityEngine.Debug.Log` with a single structured pipeline, so every mod that depends on it logs through one consistent, filterable system.

## For players

Install this as a shared dependency. If you are subscribed to a mod that requires RimLogging, this is what gets installed. It captures and organizes log output so bug reports are clean and easy to share.

Pair it with [Lightweave](https://github.com/RimworldCosmere/Lightweave) to get the three-pane in-game log viewer: live channel filtering, an expression filter DSL, and a one-click bug-report bundle that uploads and copies a shareable link to your clipboard. Without Lightweave, logging still works fully. You just use the vanilla log window.

## For mod authors

- Hierarchical channels (XML defs or transient) with prefix-based resolution.
- Serilog-style templated messages with named placeholders and positional args.
- Anonymous-object structured context attached to any entry.
- Six levels: `Trace`, `Debug`, `Info`, `Warn`, `Error`, `Fatal`.
- Multi-sink output: Verse log writeback, rolling text file, rolling NDJSON file, in-memory, plus a plugin sink API.
- Lock-free MPSC queue with a background drain. Synchronous bypass for `Error` / `Fatal`.

Add it to your project from NuGet:

```
dotnet add package RimWorks.RimLogging
```

Then declare the Workshop item as a dependency in your `About.xml` so subscribers get the shared runtime DLL automatically.

## Links

- Source, docs, and issues: https://github.com/RimWorks/rimworld-logging-framework
- Companion UI framework: https://github.com/RimworldCosmere/Lightweave

Licensed under MIT.
