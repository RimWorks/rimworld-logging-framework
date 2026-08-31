using System;
using RimWorks.RimLogging;
using RimWorks.RimLogging.Sinks;

namespace RimWorks.RimLogging.Tests;

// Shared fixture for tests that emit through the Log API and read entries back from a
// MemoryLogSink. Isolates each test: clears any registered sinks, registers a fresh in-memory
// sink, lowers the global gate to Trace, and restores the prior gate on dispose.
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
