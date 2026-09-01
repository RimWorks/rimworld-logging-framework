using System;
using RimWorks.RimLogging;
using RimWorks.RimLogging.Pipeline;
using RimWorks.RimLogging.Sinks;
using Xunit;

namespace RimWorks.RimLogging.Tests.Pipeline;

public class LoggingLifecycleTests : IDisposable
{
    private readonly LogLevel _savedMin;

    public LoggingLifecycleTests()
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
    public void Init_IsIdempotent()
    {
        Logging.Init();

        bool dispatched = false;
        ManualResetEventSlim ready = new ManualResetEventSlim(false);
        MemoryLogSink sink = new MemoryLogSink();
        SinkRegistry.Register(sink);

        Logging.Init();

        // A second Init must not replace the drain -- queue/drain same instance means drain thread is alive.
        // Verify the drain thread is still running by posting and receiving via the sink.
        ThreadCaptureSink captureSink = new ThreadCaptureSink(_ => { dispatched = true; ready.Set(); });
        SinkRegistry.Register(captureSink);

        Log.Info("idempotency-check");

        Assert.True(ready.Wait(5000), "Drain thread did not dispatch within 5 s");
        Assert.True(dispatched);
    }

    [Fact]
    public void Init_InvokesInstalledShutdownHookWithDrain()
    {
        BackgroundDrain? captured = null;
        Logging.SetShutdownHookForTests(drain => captured = drain);

        Logging.Init();

        Assert.NotNull(captured);
    }

    [Fact]
    public void Shutdown_NullsOutQueueAndDrain()
    {
        Logging.Init();
        Logging.Shutdown();

        // After Shutdown, Emit should route synchronously (drain == null).
        int callingThreadId = Environment.CurrentManagedThreadId;
        int dispatchedThreadId = -1;
        ThreadCaptureSink captureSink = new ThreadCaptureSink(id => dispatchedThreadId = id);
        SinkRegistry.Register(captureSink);

        Log.Info("shutdown-test");

        Assert.Equal(callingThreadId, dispatchedThreadId);
    }

    [Fact]
    public void EmitAfterShutdown_DispatchesSynchronously()
    {
        Logging.Init();
        Logging.Shutdown();

        int callingThreadId = Environment.CurrentManagedThreadId;
        int dispatchedThreadId = -1;
        ThreadCaptureSink captureSink = new ThreadCaptureSink(id => dispatchedThreadId = id);
        SinkRegistry.Register(captureSink);

        Log.Info("post-shutdown-emit");

        Assert.Equal(callingThreadId, dispatchedThreadId);
    }

    [Fact]
    public void RegisterSink_NullThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Logging.RegisterSink(null!));
    }

    [Fact]
    public void RemoveSink_NullThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Logging.RemoveSink(null!));
    }

    [Fact]
    public void EnsureStartedAndInit_AreCompatible()
    {
        // EnsureStarted followed by Init must not double-allocate.
        Logging.EnsureStarted();

        bool dispatched = false;
        ManualResetEventSlim ready = new ManualResetEventSlim(false);
        ThreadCaptureSink captureSink = new ThreadCaptureSink(_ => { dispatched = true; ready.Set(); });
        SinkRegistry.Register(captureSink);

        Logging.Init(); // must be a no-op for queue/drain

        Log.Info("ensure-then-init");

        Assert.True(ready.Wait(5000), "Drain thread did not dispatch within 5 s");
        Assert.True(dispatched);
    }
}

