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

## Applying the fix automatically

Every squiggle has a code fix attached, in both the IDE lightbulb and `dotnet format` style
fix-all tools. It rewrites the call in place:

| Vanilla | Fixed to |
|---|---|
| `Log.Message(text)` | `Log.Info(text)` |
| `Log.Warning(text)` | `Log.Warn(text)` |
| `Log.Error(text)` | `Log.Error(text)` |
| `Log.ErrorOnce(text, key)` | `Log.ErrorOnce(key, text)` |
| `Log.WarningOnce(text, key)` | `Log.WarnOnce(key, text)` |

The fix always writes the fully qualified `RimWorks.RimLogging.Log`, then lets Roslyn shorten it
to `Log` where that stays unambiguous. It never adds a `using` alias, so a file that also calls
`Log.Clear()` or another vanilla-only method keeps compiling.

The fix always targets the default-channel overload, since that is the only rewrite that
preserves behavior without guessing. Pick a channel by hand afterward with `Log.InfoTo`,
`Log.WarnTo`, `Log.ErrorTo`, or the `*OnceTo` equivalents.

Three call shapes get no fix, and stay squiggled for a human to handle:

- a named argument, like `Log.ErrorOnce(text: msg, key: 1)`
- the `Log.Message(object)` overload, which has no string to move
- anything the fix can't tell apart from those two at the call site

Fix-all only ever touches call sites that already carry the diagnostic. It never removes an
unused `using Verse;`, since the file almost certainly uses Verse for other things too.

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
