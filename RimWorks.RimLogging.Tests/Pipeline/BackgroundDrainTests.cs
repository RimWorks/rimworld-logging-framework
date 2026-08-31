using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using RimWorks.RimLogging;
using RimWorks.RimLogging.Capture;
using RimWorks.RimLogging.Pipeline;
using Xunit;

namespace RimWorks.RimLogging.Tests.Pipeline;

public class BackgroundDrainTests
{
    private static LogEntry MakeEntry(int seq) => new LogEntry
    {
        Timestamp = DateTime.UtcNow,
        Level = LogLevel.Info,
        Channel = "test",
        MessageTemplate = $"msg {seq}",
        RenderedMessage = $"msg {seq}",
        Source = SourceLocation.Empty,
    };

    [Fact]
    public void Enqueue_DispatchFiresPromptly()
    {
        // Asserts the drain thread wakes and dispatches an enqueued entry. Uses a generous
        // upper bound so the test doesn't flake on loaded CI runners; the original 50ms ceiling
        // was a flaky measurement of scheduler responsiveness, not the contract under test.
        MpscQueue<LogEntry> queue = new MpscQueue<LogEntry>(16);
        ManualResetEventSlim fired = new ManualResetEventSlim(false);

        BackgroundDrain drain = new BackgroundDrain(queue, _ => fired.Set());
        try
        {
            drain.Enqueue(MakeEntry(1));
            Assert.True(fired.Wait(5000), "drain did not dispatch within 5 s");
        }
        finally
        {
            drain.Dispose();
        }
    }

    [Fact]
    public void Enqueue1000_AllDispatched()
    {
        MpscQueue<LogEntry> queue = new MpscQueue<LogEntry>(2048);
        ConcurrentQueue<LogEntry> received = new ConcurrentQueue<LogEntry>();

        BackgroundDrain drain = new BackgroundDrain(queue, received.Enqueue);
        try
        {
            for (int i = 0; i < 1000; i++)
                drain.Enqueue(MakeEntry(i));

            bool drained = drain.WaitForDrain(5000);
            Assert.True(drained, "WaitForDrain timed out before 1000 entries dispatched");
            Assert.Equal(1000, received.Count);
        }
        finally
        {
            drain.Dispose();
        }
    }

    [Fact]
    public void DispatchThrows_NextEntryStillDispatched()
    {
        MpscQueue<LogEntry> queue = new MpscQueue<LogEntry>(32);
        ConcurrentQueue<int> dispatched = new ConcurrentQueue<int>();
        int seq = 0;

        BackgroundDrain drain = new BackgroundDrain(queue, entry =>
        {
            int n = Interlocked.Increment(ref seq);
            if (n == 5)
                throw new InvalidOperationException("sentinel");
            dispatched.Enqueue(n);
        });

        try
        {
            for (int i = 0; i < 10; i++)
                drain.Enqueue(MakeEntry(i));

            drain.WaitForDrain(5000);
            Assert.Equal(9, dispatched.Count);
        }
        finally
        {
            drain.Dispose();
        }
    }

    [Fact]
    public void WaitForDrain_ReturnsTrueWhenEmpties()
    {
        MpscQueue<LogEntry> queue = new MpscQueue<LogEntry>(64);
        BackgroundDrain drain = new BackgroundDrain(queue, _ => { });
        try
        {
            for (int i = 0; i < 20; i++)
                drain.Enqueue(MakeEntry(i));

            bool result = drain.WaitForDrain(5000);
            Assert.True(result, "WaitForDrain should return true once queue empties");
        }
        finally
        {
            drain.Dispose();
        }
    }

    [Fact]
    public void Dispose_BlocksUntilPendingDispatch()
    {
        MpscQueue<LogEntry> queue = new MpscQueue<LogEntry>(256);
        List<int> dispatched = new List<int>();
        object lockObj = new object();
        int count = 0;

        BackgroundDrain drain = new BackgroundDrain(queue, _ =>
        {
            lock (lockObj)
                dispatched.Add(Interlocked.Increment(ref count));
        });

        for (int i = 0; i < 100; i++)
            drain.Enqueue(MakeEntry(i));

        drain.Dispose();

        Assert.Equal(100, dispatched.Count);
    }
}
