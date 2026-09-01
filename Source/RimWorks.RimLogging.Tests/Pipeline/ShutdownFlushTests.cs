using System.Collections.Generic;
using RimWorks.RimLogging;
using RimWorks.RimLogging.Capture;
using RimWorks.RimLogging.Pipeline;
using Xunit;

namespace RimWorks.RimLogging.Tests.Pipeline;

public sealed class ShutdownFlushTests : System.IDisposable
{
    private readonly bool _priorInstalled;

    public ShutdownFlushTests()
    {
        _priorInstalled = ShutdownFlushLogic.Installed;
        ShutdownFlushLogic.ResetInstalledForTests();
    }

    public void Dispose()
    {
        if (_priorInstalled)
            ShutdownFlushLogic.Installed = true;
        else
            ShutdownFlushLogic.ResetInstalledForTests();
    }

    private static LogEntry MakeEntry(int seq) => new LogEntry
    {
        Timestamp = System.DateTime.UtcNow,
        Level = LogLevel.Info,
        Channel = "test",
        MessageTemplate = $"msg {seq}",
        RenderedMessage = $"msg {seq}",
        Source = SourceLocation.Empty,
    };

    [Fact]
    public void Flush_WaitsForDrainThenFlushesAllSinks()
    {
        MpscQueue<LogEntry> queue = new MpscQueue<LogEntry>(64);
        List<LogEntry> dispatched = new List<LogEntry>();
        CountingSink sink = new CountingSink();

        BackgroundDrain drain = new BackgroundDrain(queue, entry =>
        {
            lock (dispatched)
                dispatched.Add(entry);
        });

        RimWorks.RimLogging.Sinks.SinkRegistry.Register(sink);
        try
        {
            for (int i = 0; i < 5; i++)
                drain.Enqueue(MakeEntry(i));

            ShutdownFlushLogic.Flush(drain);

            Assert.Equal(5, dispatched.Count);
            Assert.Equal(1, sink.FlushCount);
        }
        finally
        {
            RimWorks.RimLogging.Sinks.SinkRegistry.Remove(sink);
            drain.Dispose();
        }
    }

    [Fact]
    public void Installed_IsIdempotentGuard()
    {
        ShutdownFlushLogic.ResetInstalledForTests();
        Assert.False(ShutdownFlushLogic.Installed);

        ShutdownFlushLogic.Installed = true;
        Assert.True(ShutdownFlushLogic.Installed);

        ShutdownFlushLogic.ResetInstalledForTests();
        Assert.False(ShutdownFlushLogic.Installed);
    }

    [Fact]
    public void DrainTimeoutMs_Is500()
    {
        Assert.Equal(500, ShutdownFlushLogic.DrainTimeoutMs);
    }
}

file sealed class CountingSink : ILogSink
{
    public int FlushCount { get; private set; }
    public string Name => "Counting";
    public LogLevel MinLevel => LogLevel.Trace;
    public void Write(LogEntry entry) { }
    public void Flush() => FlushCount++;
    public void Dispose() { }
}
