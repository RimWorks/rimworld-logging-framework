# Verse.Log analyzer

The NuGet package ships a Roslyn analyzer. It flags `Verse.Log` writes in a project that already
references RimLogging, so you find them at build time instead of in review.

Both rules stay silent unless the compilation can see `RimWorks.RimLogging.Log`. A project that
never took the dependency is never warned. A project that references the DLL directly, without the
package, does not get the analyzer at all.

## Rules

| ID | Applies to | Default |
|---|---|---|
| `RIMLOG001` | `Log.ErrorOnce`, `Log.WarningOnce` | Warning |
| `RIMLOG002` | `Log.Error`, `Log.Warning`, `Log.Message` | Warning |

### RIMLOG001: the once-key is shared with every other mod

`Verse.Log.ErrorOnce` and `Verse.Log.WarningOnce` record their `int` key in one static
`HashSet<int>` on `Verse.Log`. Every mod in the load order writes to that same set. If another mod
already used your number, your message never prints and nothing tells you.

`Log.ErrorOnceTo` and `Log.WarnOnceTo` key off a string in RimLogging's own table.

### RIMLOG002: the write loses its channel and context

RimLogging patches `Verse.Log.Error`, `Verse.Log.Warning` and `Verse.Log.Message`, so these calls do
reach the pipeline. The entry arrives as plain text. RimLogging infers the channel from the calling
assembly rather than reading the one you meant. There is no message template and no structured
context.

The `Once` methods and the `Message(object)` overload each call one of those three internally, so
they are captured too.

## What to call instead

| Vanilla | RimLogging |
|---|---|
| `Log.Message(text)` | `Log.InfoTo(channel, text)` |
| `Log.Warning(text)` | `Log.WarnTo(channel, text)` |
| `Log.Error(text)` | `Log.ErrorTo(channel, text)` |
| `Log.WarningOnce(text, key)` | `Log.WarnOnceTo(channel, key, text)` |
| `Log.ErrorOnce(text, key)` | `Log.ErrorOnceTo(channel, key, text)` |

**Watch the `Once` rows.** The key moves in front of the message. It also changes from an `int` to a
`string`. Both parameters are strings on the RimLogging side, so swapping them still compiles.

```csharp
// before
Verse.Log.ErrorOnce("worldgen blew up", 8823);

// after
Log.ErrorOnceTo("MPF.Worldgen", "worldgen-blew-up", "worldgen blew up");
```

The once-key is global across channels. The same key on two channels still fires once in total.

## Changing the severity

Set either rule in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.RIMLOG002.severity = suggestion
```

Use `none` to turn a rule off. To keep one call, suppress it in place:

```csharp
#pragma warning disable RIMLOG002 // re-raising a vanilla message, the guessed channel is fine here
Verse.Log.Error(text);
#pragma warning restore RIMLOG002
```
