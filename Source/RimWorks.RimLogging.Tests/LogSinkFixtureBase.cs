using System;
using RimWorks.RimLogging;
using RimWorks.RimLogging.Sinks;

namespace RimWorks.RimLogging.Tests;

// Fixture for tests that emit through Log and read back from a MemoryLogSink. Clears sinks,
// drops the global gate to Trace, and restores it on dispose.
public abstract class LogSinkFixtureBase : IDisposable
{
    private readonly LogLevel _savedMin;
    protected readonly MemoryLogSink _sink = new MemoryLogSink();

    protected LogSinkFixtureBase()
    {
        _savedMin = Logging.GlobalMinLevel;
        SinkRegistry.DisposeAll();
        SinkRegistry.Register(_sink);
        Logging.GlobalMinLevel = LogLevel.Trace;
    }

    public virtual void Dispose()
    {
        Logging.GlobalMinLevel = _savedMin;
        SinkRegistry.Remove(_sink);
        _sink.Dispose();
        GC.SuppressFinalize(this);
    }
}
