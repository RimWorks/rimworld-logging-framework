# Custom sinks

Implement `ILogSink` and register it, either from code or via a `SinkDef`.

```csharp
public sealed class MySink : ILogSink
{
    public string Name => "MySink";
    public LogLevel MinLevel => LogLevel.Info;

    public void Write(LogEntry entry) { /* render or store entry */ }
    public void Flush() { /* flush buffers */ }
    public void Dispose() { /* close handles */ }
}

// Register from a StaticConstructorOnStartup or your mod ctor:
Logging.RegisterSink(new MySink());
```

Or load it from XML so the bootstrap phase instantiates it:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
    <RimWorks.RimLogging.Sinks.SinkDef>
        <defName>MySink</defName>
        <label>My Sink</label>
        <sinkClass>MyMod.MySink, MyMod</sinkClass>
        <minLevel>Info</minLevel>
        <enabledByDefault>true</enabledByDefault>
    </RimWorks.RimLogging.Sinks.SinkDef>
</Defs>
```

`sinkClass` is an assembly-qualified type name. The implementation needs a public parameterless constructor for XML loading. Built-in sinks: `VerseLog`, `RollingText` (enabled by default), `RollingJson` (NDJSON, off by default).

## The NDJSON row

`RollingJson` writes one JSON object per line to `RimLogging-<utc-stamp>-<pid>.ndjson`. Each row carries every `LogEntry` field:

| Key | Type | Notes |
|---|---|---|
| `ts` | string | UTC, `yyyy-MM-ddTHH:mm:ss.fffZ` |
| `level` | string | Uppercase level name |
| `channel` | string | |
| `src` | string or null | `File.cs:42`, null when the call site was not captured |
| `msg` | string | Rendered message, rich-text tags stripped |
| `tmpl` | string | Unrendered template |
| `ctx` | object or null | Structured context; null when empty |
| `stack` | string or null | |
| `exc` | object or null | `type`, `message`, `stack` |
| `mod` | string or null | |
| `tick` | number or null | Null outside a running game |
| `repeats` | number | 1 for a normal entry |
| `patched` | array or null | **Null means attribution never ran; an empty array means nothing patched the method.** Those are different claims, so keep them apart. |

## Reading a log back

`NdjsonLogReader.ReadFile(path)` parses a file into `LogEntry` values, and the viewer's More menu lists the files in your log directory so you can load one. That matters when a mod throws during startup: the viewer never opens, and the file is the only record.

Two things do not survive the trip:

- **Exceptions arrive as context.** An arbitrary exception type cannot be rebuilt from a name and a message, so `exc` is folded into the context under `exception_type`, `exception_message` and `exception_stack`.
- **Whole numbers come back as `long`.** JSON has one number type, so an `int` written to context reads back as a `long`.

A malformed line is skipped rather than thrown on, because a session that crashed mid-write leaves a truncated last line. Rows written before `mod`, `tick`, `repeats` and `patched` existed still parse, taking the default for each missing key.

