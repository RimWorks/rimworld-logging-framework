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
