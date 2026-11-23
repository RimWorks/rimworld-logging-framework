# Settings

The in-game mod settings page exposes:

- **Global minimum level** (`globalMinLevel`) - drops every entry below this level before any sink sees it.
- **Log directory** (`logDirectory`) - where the rolling sinks write; normalized to a default under the game's persistent data path when left blank. **Open folder** opens it in your file manager, creating it first if nothing has written to a hand-typed path yet.
- **Retention count** (`retentionCount`) - number of rotated log files kept.
- **rimworks.org upload URL** (`rimworksUrl`) - endpoint that receives bug-report bundles.
- **Print stack traces in text logs from** (`printStackAt`) - the lowest level whose stored stack trace a text log prints under the line. The default is `Warn`. The picker sits right below the **Capture stack traces** checkbox. It changes two outputs, the in-game log and the rolling text file. It does not change the **Capture stack traces** setting, the log viewer, the JSON file, or the bug report bundle. An exception attached to a line prints at every level. A sink can set its own level, see [Custom sinks](sinks.md).
- **Combine message and stack trace** (`logViewerCombinedDetail`) - shows the message and stack trace as one block in the detail pane instead of two.
- **Detail placement** (`logViewerDetailPlacement`) - `Bottom`, `Right`, or `Popout`. Cycled from the viewer's own toolbar, not from this page.
- **Filter presets** - saved name/expression pairs for the viewer's filter DSL.
- **Log destinations** - a checkbox per `SinkDef`. Toggling one rebuilds the sink set immediately, no restart. `Rolling JSON File` ships off; turn it on to get the NDJSON files the viewer can load back.

All settings persist across restarts via RimWorld's Scribe system.

## Bug bundle

The viewer's "share bundle" button serializes a JSON payload and posts it to rimworks.org. It then copies the returned URL to your clipboard and shows a toast. The payload contains:

- RimWorld version and framework version.
- The active mod list (name, packageId, version, active flag).
- Recent log entries (timestamp, level, channel, source, message, structured context, stack trace).

Change the `rimworksUrl` setting to point the mod at a different endpoint, such as a worker running on your own machine.
