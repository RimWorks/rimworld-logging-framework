using System;
using System.Linq;
using RimWorks.RimLogging.Pipeline;
using RimWorks.RimLogging.Sinks;
using Xunit;

namespace RimWorks.RimLogging.Tests.Pipeline;

/// <summary>
/// Drives the whole pipeline from Log.X to sink.Write, checking that rendering, context
/// capture, async dispatch and the Error-level sync bypass all work together.
/// </summary>
public class PipelineEndToEndTests : IDisposable
{
    private readonly LogLevel _savedMin;

    public PipelineEndToEndTests()
    {
        _savedMin = Logging.GlobalMinLevel;
        Logging.ResetShutdownHookForTests();
        SinkRegistry.DisposeAll();
        Logging.GlobalMinLevel = LogLevel.Trace;
    }

    public void Dispose()
    {
        Logging.Shutdown();
        SinkRegistry.DisposeAll();
        Logging.GlobalMinLevel = _savedMin;
        Logging.ResetShutdownHookForTests();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Info_TemplateAndArgs_AsyncDrainDeliversRenderedEntryWithContext()
    {
        Logging.Init();
        MemoryLogSink memory = new MemoryLogSink();
        SinkRegistry.Register(memory);

        ManualResetEventSlim signal = new ManualResetEventSlim(false);
        ThreadCaptureSink signalSink = new ThreadCaptureSink(_ => signal.Set());
        SinkRegistry.Register(signalSink);

        Log.InfoTo("e2e", "user {Name} logged in from {Host}", new object?[] { "alice", "ws-1" });

        Assert.True(signal.Wait(5000), "async drain did not dispatch within 5 s");

        LogEntry entry = Assert.Single(memory.Entries);
        Assert.Equal(LogLevel.Info, entry.Level);
        Assert.Equal("e2e", entry.Channel);
        Assert.Equal("user alice logged in from ws-1", entry.RenderedMessage);
        Assert.NotNull(entry.Context);
        Assert.Equal("alice", entry.Context!["Name"]);
        Assert.Equal("ws-1", entry.Context!["Host"]);
    }

    [Fact]
    public void Info_StructuredContextObject_CapturesPropertiesIntoContextDictionary()
    {
        Logging.Init();
        MemoryLogSink memory = new MemoryLogSink();
        SinkRegistry.Register(memory);

        ManualResetEventSlim signal = new ManualResetEventSlim(false);
        ThreadCaptureSink signalSink = new ThreadCaptureSink(_ => signal.Set());
        SinkRegistry.Register(signalSink);

        Log.InfoTo("e2e", "user action", new { UserId = 42, Role = "admin" });

        Assert.True(signal.Wait(5000), "async drain did not dispatch within 5 s");

        LogEntry entry = Assert.Single(memory.Entries);
        Assert.Equal("user action", entry.RenderedMessage);
        Assert.NotNull(entry.Context);
        Assert.Equal(42, entry.Context!["UserId"]);
        Assert.Equal("admin", entry.Context!["Role"]);
    }

    [Fact]
    public void Error_BypassesAsyncDrain_DispatchesOnCallingThread()
    {
        Logging.Init();

        int callingThreadId = Environment.CurrentManagedThreadId;
        int dispatchedThreadId = -1;
        ThreadCaptureSink captureSink = new ThreadCaptureSink(id => dispatchedThreadId = id);
        SinkRegistry.Register(captureSink);

        MemoryLogSink memory = new MemoryLogSink();
        SinkRegistry.Register(memory);

        Log.ErrorTo("e2e", "boom {Code}", new object?[] { 500 });

        Assert.Equal(callingThreadId, dispatchedThreadId);

        LogEntry entry = Assert.Single(memory.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal("boom 500", entry.RenderedMessage);
    }

    [Fact]
    public void GlobalMinLevel_Filters_EntriesBelowThresholdNeverReachSinks()
    {
        Logging.Init();
        Logging.GlobalMinLevel = LogLevel.Warn;

        MemoryLogSink memory = new MemoryLogSink();
        SinkRegistry.Register(memory);

        ManualResetEventSlim signal = new ManualResetEventSlim(false);
        ThreadCaptureSink signalSink = new ThreadCaptureSink(_ => signal.Set());
        SinkRegistry.Register(signalSink);

        Log.InfoTo("e2e", "below threshold");
        Log.WarnTo("e2e", "at threshold");

        Assert.True(signal.Wait(5000), "async drain did not dispatch within 5 s");

        Assert.Single(memory.Entries);
        Assert.Equal(LogLevel.Warn, memory.Entries[0].Level);
        Assert.Equal("at threshold", memory.Entries[0].RenderedMessage);
    }

    [Fact]
    public void MultipleSinks_AllReceiveEntry()
    {
        Logging.Init();
        MemoryLogSink memoryA = new MemoryLogSink();
        MemoryLogSink memoryB = new MemoryLogSink();
        SinkRegistry.Register(memoryA);
        SinkRegistry.Register(memoryB);

        ManualResetEventSlim signal = new ManualResetEventSlim(false);
        ThreadCaptureSink signalSink = new ThreadCaptureSink(_ => signal.Set());
        SinkRegistry.Register(signalSink);

        Log.InfoTo("e2e", "fanout");

        Assert.True(signal.Wait(5000), "async drain did not dispatch within 5 s");

        Assert.Equal("fanout", memoryA.Entries.Single().RenderedMessage);
        Assert.Equal("fanout", memoryB.Entries.Single().RenderedMessage);
    }
}
