# RimLogging: RimWorld log viewer and bug report sharing

Read your RimWorld log inside the game. Filter it down to the red error you actually care about, then share it as a link with one click. RimLogging is also a structured logging framework for mod authors. It replaces vanilla `Verse.Log` and `UnityEngine.Debug.Log` with one pipeline every mod shares.

## How to share your RimWorld log

Open the log viewer, press the share button, and RimLogging uploads a bundle and copies a shareable link to your clipboard. Paste it into Discord or a GitHub issue. The bundle carries your mod list, the game version, and the captured entries. RimLogging trims it to fit the upload limit, so a long session still goes through.

## What is wrong with the vanilla log window?

It is one flat list, it truncates, and there is no way to hide the noise. A red error from the mod you are chasing sits between four thousand lines you do not need.

RimLogging replaces it with a three-pane viewer: a channel tree on the left, live level toggles, and an expression filter DSL. The detail pane docks to the bottom, docks to the right, or pops out into its own window.

Because it hijacks `Verse.Log`, it captures output from mods that never opted in. Nothing slips past.

## How is this different from HugsLib's publish log?

HugsLib uploads the whole log file as one wall of text and hands you a link. That works, and then you scroll.

RimLogging lets you narrow first. Pick one mod's channel, drop everything below `Warn`, type a filter expression, and see what is left. Then send that. The person reading your bug report gets the log and the mod list without having to search a file for the one line that matters.

## For players

Install this as a shared dependency. If you are subscribed to a mod that requires RimLogging, this is what gets installed. It captures and organizes log output so bug reports are clean and easy to share.

## For mod authors

- Hierarchical channels (XML defs or transient) with prefix-based resolution.
- Serilog-style templated messages with named placeholders and positional args.
- Anonymous-object structured context attached to any entry.
- Six levels: `Trace`, `Debug`, `Info`, `Warn`, `Error`, `Fatal`.
- Multi-sink output: Verse log writeback, rolling text file, rolling NDJSON file, in-memory, plus a plugin sink API.
- Lock-free MPSC queue with a background drain. Synchronous bypass for `Error` / `Fatal`.
- A Roslyn analyzer that flags leftover `Verse.Log` calls at build time, with a code fix.

Add it to your project from NuGet:

```
dotnet add package RimWorks.RimLogging
```

Then declare the Workshop item as a dependency in your `About.xml` so subscribers get the shared runtime DLL automatically.

## Links

- Source, docs, and issues: https://github.com/RimWorks/rimworld-logging-framework

## More modding tools from RimWorks

- [RimObs](https://steamcommunity.com/sharedfiles/filedetails/?id=3733585062): performance profiler that finds which mod is eating your TPS.
- [Pickle](https://steamcommunity.com/sharedfiles/filedetails/?id=3791648678): run Gherkin tests against a live RimWorld session.
- [Quickstarts](https://steamcommunity.com/sharedfiles/filedetails/?id=3793646067): boot straight into a configured colony from the dev quicktest menu.

Licensed under MIT.
