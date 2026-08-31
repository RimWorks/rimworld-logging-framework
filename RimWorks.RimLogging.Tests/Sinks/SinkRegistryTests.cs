using System;
using System.Collections.Generic;
using RimWorks.RimLogging;
using RimWorks.RimLogging.Sinks;
using Xunit;

namespace RimWorks.RimLogging.Tests.Sinks;

public class SinkRegistryTests : IDisposable
{
    private readonly int _savedCap;
    private static readonly string[] ExpectedH1H2H3 = ["h1", "h2", "h3"];
    private static readonly string[] ExpectedError = ["error"];
    private static readonly string[] ExpectedE1ToE5 = ["e1", "e2", "e3", "e4", "e5"];
    private static readonly string[] ExpectedE3ToE5 = ["e3", "e4", "e5"];
    private static readonly string[] ExpectedE1ToE4 = ["e1", "e2", "e3", "e4"];

    public SinkRegistryTests()
    {
        SinkRegistry.DisposeAll();
        _savedCap = SinkRegistry.PostReplayCap;
    }

    public void Dispose()
    {
        SinkRegistry.PostReplayCap = _savedCap;
        SinkRegistry.DisposeAll();
        GC.SuppressFinalize(this);
    }

    private static LogEntry MakeEntry(string message = "test", LogLevel level = LogLevel.Info)
    {
        return new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Channel = "test",
            MessageTemplate = message,
            RenderedMessage = message,
            Context = null,
            Source = default,
            StackTrace = null,
            Exception = null,
        };
    }

    [Fact]
    public void Register_AddsSinkToSnapshot()
    {
        FakeSink sink = new FakeSink("alpha");

        SinkRegistry.Register(sink);

        IReadOnlyList<ILogSink> snap = SinkRegistry.Snapshot();
        Assert.Contains(sink, snap);
    }

    [Fact]
    public void Remove_ExistingSink_ReturnsTrueAndRemovesFromSnapshot()
    {
        FakeSink sink = new FakeSink("beta");
        SinkRegistry.Register(sink);

        bool removed = SinkRegistry.Remove(sink);

        Assert.True(removed);
        Assert.DoesNotContain(sink, SinkRegistry.Snapshot());
    }

    [Fact]
    public void Remove_AbsentSink_ReturnsFalse()
    {
        FakeSink sink = new FakeSink("gamma");

        bool removed = SinkRegistry.Remove(sink);

        Assert.False(removed);
    }

    [Fact]
    public void Snapshot_ReturnsStableCopy_NotAffectedByLaterMutation()
    {
        FakeSink first = new FakeSink("delta-1");
        SinkRegistry.Register(first);

        IReadOnlyList<ILogSink> snap = SinkRegistry.Snapshot();

        FakeSink second = new FakeSink("delta-2");
        SinkRegistry.Register(second);

        Assert.DoesNotContain(second, snap);
    }

    [Fact]
    public void DispatchSynchronously_WritesToAllRegisteredSinks()
    {
        FakeSink a = new FakeSink("epsilon-a");
        FakeSink b = new FakeSink("epsilon-b");
        SinkRegistry.Register(a);
        SinkRegistry.Register(b);
        LogEntry entry = MakeEntry("dispatch-test");

        SinkRegistry.DispatchSynchronously(entry);

        Assert.Equal(1, a.WriteCount);
        Assert.Equal(1, b.WriteCount);
        Assert.Same(entry, a.LastWritten);
        Assert.Same(entry, b.LastWritten);
    }

    [Fact]
    public void DispatchSynchronously_SuppressesReentry_RecursiveCallIsIgnored()
    {
        LogEntry entry = MakeEntry("reentry-test");
        ReentrantSink reentrant = new ReentrantSink("zeta", entry);
        SinkRegistry.Register(reentrant);

        SinkRegistry.DispatchSynchronously(entry);

        // The sink was called once; its internal recursive call was suppressed.
        Assert.Equal(1, reentrant.WriteCount);
    }

    [Fact]
    public void DispatchSynchronously_ThrowingSink_DoesNotPreventOtherSinksFromReceivingEntry()
    {
        ThrowingSink bad = new ThrowingSink("eta-bad");
        FakeSink good = new FakeSink("eta-good");
        SinkRegistry.Register(bad);
        SinkRegistry.Register(good);
        LogEntry entry = MakeEntry("throw-test");

        SinkRegistry.DispatchSynchronously(entry);

        Assert.Equal(1, good.WriteCount);
    }

    [Fact]
    public void FlushAll_CallsFlushOnEveryRegisteredSink()
    {
        FakeSink a = new FakeSink("theta-a");
        FakeSink b = new FakeSink("theta-b");
        SinkRegistry.Register(a);
        SinkRegistry.Register(b);

        SinkRegistry.FlushAll();

        Assert.Equal(1, a.FlushCount);
        Assert.Equal(1, b.FlushCount);
    }

    [Fact]
    public void FlushAll_ThrowingSink_DoesNotPreventOtherSinksFromBeingFlushed()
    {
        ThrowingSink bad = new ThrowingSink("iota-bad");
        FakeSink good = new FakeSink("iota-good");
        SinkRegistry.Register(bad);
        SinkRegistry.Register(good);

        SinkRegistry.FlushAll();

        Assert.Equal(1, good.FlushCount);
    }

    [Fact]
    public void DisposeAll_EmptiesRegistryAndDisposesEachSink()
    {
        FakeSink a = new FakeSink("kappa-a");
        FakeSink b = new FakeSink("kappa-b");
        SinkRegistry.Register(a);
        SinkRegistry.Register(b);

        SinkRegistry.DisposeAll();

        Assert.Empty(SinkRegistry.Snapshot());
        Assert.True(a.Disposed);
        Assert.True(b.Disposed);
    }

    [Fact]
    public void DisposeAll_ThrowingSink_DoesNotPreventOtherSinksFromBeingDisposed()
    {
        ThrowingSink bad = new ThrowingSink("lambda-bad");
        FakeSink good = new FakeSink("lambda-good");
        SinkRegistry.Register(bad);
        SinkRegistry.Register(good);

        SinkRegistry.DisposeAll();

        Assert.True(good.Disposed);
    }

    [Fact]
    public void Register_AfterEntriesDispatched_ReplaysPriorEntriesInOrder()
    {
        SinkRegistry.DispatchSynchronously(MakeEntry("h1"));
        SinkRegistry.DispatchSynchronously(MakeEntry("h2"));
        SinkRegistry.DispatchSynchronously(MakeEntry("h3"));
        RecordingSink late = new RecordingSink("late");

        SinkRegistry.Register(late);

        Assert.Equal(ExpectedH1H2H3, late.Messages());
    }

    [Fact]
    public void Register_Replay_RespectsSinkMinLevel()
    {
        SinkRegistry.DispatchSynchronously(MakeEntry("trace", LogLevel.Trace));
        SinkRegistry.DispatchSynchronously(MakeEntry("info", LogLevel.Info));
        SinkRegistry.DispatchSynchronously(MakeEntry("error", LogLevel.Error));
        RecordingSink late = new RecordingSink("warn-and-up", LogLevel.Warn);

        SinkRegistry.Register(late);

        Assert.Equal(ExpectedError, late.Messages());
    }

    [Fact]
    public void Register_UnboundedUntilFirstReplay_RetainsMoreThanCap()
    {
        SinkRegistry.PostReplayCap = 3;
        for (int i = 1; i <= 5; i++) SinkRegistry.DispatchSynchronously(MakeEntry("e" + i));
        RecordingSink late = new RecordingSink("late");

        SinkRegistry.Register(late);

        // No sink had triggered a replay, so all 5 are retained despite the cap of 3.
        Assert.Equal(ExpectedE1ToE5, late.Messages());
    }

    [Fact]
    public void Register_AfterCapEngaged_SecondSinkOnlyGetsCappedTail()
    {
        SinkRegistry.PostReplayCap = 3;
        for (int i = 1; i <= 5; i++) SinkRegistry.DispatchSynchronously(MakeEntry("e" + i));
        SinkRegistry.Register(new RecordingSink("first")); // triggers replay, engages cap, trims to last 3
        RecordingSink second = new RecordingSink("second");

        SinkRegistry.Register(second);

        Assert.Equal(ExpectedE3ToE5, second.Messages());
    }

    [Fact]
    public void DispatchAfterReplay_DeliversLiveEntriesOnceWithoutDuplicatingHistory()
    {
        SinkRegistry.PostReplayCap = 3;
        SinkRegistry.DispatchSynchronously(MakeEntry("e1"));
        SinkRegistry.DispatchSynchronously(MakeEntry("e2"));
        RecordingSink sink = new RecordingSink("live");
        SinkRegistry.Register(sink); // replays e1, e2

        SinkRegistry.DispatchSynchronously(MakeEntry("e3"));
        SinkRegistry.DispatchSynchronously(MakeEntry("e4"));

        // e1,e2 via replay then e3,e4 live: each exactly once, in order, no duplication.
        Assert.Equal(ExpectedE1ToE4, sink.Messages());
    }

    [Fact]
    public void DisposeAll_ClearsHistory_NewSinkGetsNoReplay()
    {
        SinkRegistry.DispatchSynchronously(MakeEntry("before-dispose"));

        SinkRegistry.DisposeAll();
        RecordingSink fresh = new RecordingSink("fresh");
        SinkRegistry.Register(fresh);

        Assert.Empty(fresh.Messages());
    }

    private sealed class RecordingSink : ILogSink
    {
        private readonly List<LogEntry> _entries = new List<LogEntry>();

        public RecordingSink(string name, LogLevel minLevel = LogLevel.Trace)
        {
            Name = name;
            MinLevel = minLevel;
        }

        public string Name { get; }
        public LogLevel MinLevel { get; }

        public void Write(LogEntry entry)
        {
            if (entry.Level < MinLevel) return;
            _entries.Add(entry);
        }

        public string[] Messages()
        {
            string[] result = new string[_entries.Count];
            for (int i = 0; i < _entries.Count; i++) result[i] = _entries[i].RenderedMessage;
            return result;
        }

        public void Flush() { }
        public void Dispose() { }
    }

    private sealed class FakeSink : ILogSink
    {
        public FakeSink(string name) { Name = name; }

        public string Name { get; }
        public LogLevel MinLevel => LogLevel.Trace;
        public int WriteCount { get; private set; }
        public int FlushCount { get; private set; }
        public bool Disposed { get; private set; }
        public LogEntry? LastWritten { get; private set; }

        public void Write(LogEntry entry)
        {
            WriteCount++;
            LastWritten = entry;
        }

        public void Flush() { FlushCount++; }
        public void Dispose() { Disposed = true; }
    }

    /// <summary>Sink that calls DispatchSynchronously recursively during Write.</summary>
    private sealed class ReentrantSink : ILogSink
    {
        private readonly LogEntry _entryToRedispatch;

        public ReentrantSink(string name, LogEntry entryToRedispatch)
        {
            Name = name;
            _entryToRedispatch = entryToRedispatch;
        }

        public string Name { get; }
        public LogLevel MinLevel => LogLevel.Trace;
        public int WriteCount { get; private set; }

        public void Write(LogEntry entry)
        {
            WriteCount++;
            SinkRegistry.DispatchSynchronously(_entryToRedispatch);
        }

        public void Flush() { }
        public void Dispose() { }
    }

    /// <summary>Sink that throws on every operation.</summary>
    private sealed class ThrowingSink : ILogSink
    {
        public ThrowingSink(string name) { Name = name; }

        public string Name { get; }
        public LogLevel MinLevel => LogLevel.Trace;

        public void Write(LogEntry entry) { throw new InvalidOperationException("sink write failure"); }
        public void Flush() { throw new InvalidOperationException("sink flush failure"); }
        public void Dispose() { throw new InvalidOperationException("sink dispose failure"); }
    }
}
