# Settings

The in-game mod settings page exposes:

- **Global minimum level** (`globalMinLevel`) - drops every entry below this level before any sink sees it.
- **Log directory** (`logDirectory`) - where rolling files are written; normalized to a default under the game's persistent data path when left blank.
- **Retention count** (`retentionCount`) - number of rotated log files kept.
- **Bundle proxy URL** (`proxyUrl`) - upload endpoint for bug-report bundles.
- **Combine message and stack trace** (`logViewerCombinedDetail`) - shows the message and stack trace as one block in the detail pane instead of two.
- **Detail placement** (`logViewerDetailPlacement`) - `Bottom`, `Right`, or `Popout`. Cycled from the viewer's own toolbar, not from this page.
- **Filter presets** - saved name/expression pairs for the viewer's filter DSL.

All settings persist across restarts via RimWorld's Scribe system.

## Bug bundle

The viewer's "share bundle" button serializes a JSON payload and uploads it through the configured proxy, then copies the returned URL to your clipboard (with a toast confirmation). The payload contains:

- RimWorld version and framework version.
- The active mod list (name, packageId, version, active flag).
- Recent log entries (timestamp, level, channel, source, message, structured context, stack trace).

Override the upload endpoint with the `proxyUrl` setting if you self-host the proxy. The proxy itself is a Cloudflare Worker; see [worker/README.md](../worker/README.md).
