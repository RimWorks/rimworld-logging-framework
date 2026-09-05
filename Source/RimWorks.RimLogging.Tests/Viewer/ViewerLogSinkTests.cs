using System.Collections.Generic;
using System.Reflection;
using RimWorks.RimLogging.Viewer;
using Xunit;

namespace RimWorks.RimLogging.Tests.Viewer;

public class ViewerLogSinkTests
{
    private const int RingCapacity = 20000;

    private static LogEntry Entry(
        LogLevel level,
        string message,
        string channel = "Test",
        int? tick = null,
        IReadOnlyList<string>? patchedBy = null)
    {
        return new LogEntry
        {
            Timestamp = new System.DateTime(2026, 1, 1),
            Level = level,
            Channel = channel,
            MessageTemplate = message,
            RenderedMessage = message,
            Tick = tick,
            PatchedBy = patchedBy,
        };
    }

    [Fact]
    public void Write_BelowMinLevel_IsDroppedAndDoesNotBumpRevision()
    {
        ViewerLogSink sink = new ViewerLogSink { MinLevel = LogLevel.Warn };
        int before = sink.Revision;

        sink.Write(Entry(LogLevel.Info, "ignored"));

        Assert.Empty(sink.Snapshot());
        Assert.Equal(before, sink.Revision);
    }

    [Fact]
    public void Write_AtOrAboveMinLevel_IsKeptAndBumpsRevision()
    {
        ViewerLogSink sink = new ViewerLogSink { MinLevel = LogLevel.Warn };
        int before = sink.Revision;

        sink.Write(Entry(LogLevel.Warn, "kept"));

        Assert.Single(sink.Snapshot());
        Assert.Equal(before + 1, sink.Revision);
    }

    [Fact]
    public void Snapshot_ReturnsEntriesOldestFirst()
    {
        ViewerLogSink sink = new ViewerLogSink();
        sink.Write(Entry(LogLevel.Info, "first"));
        sink.Write(Entry(LogLevel.Info, "second"));
        sink.Write(Entry(LogLevel.Info, "third"));

        IReadOnlyList<LogEntry> snapshot = sink.Snapshot();

        Assert.Equal(["first", "second", "third"], [snapshot[0].RenderedMessage, snapshot[1].RenderedMessage, snapshot[2].RenderedMessage]);
    }

    [Fact]
    public void Write_PastRingCapacity_KeepsNewestAndDropsOldest()
    {
        ViewerLogSink sink = new ViewerLogSink();
        for (int i = 0; i < RingCapacity + 10; i++)
        {
            sink.Write(Entry(LogLevel.Info, i.ToString()));
        }

        IReadOnlyList<LogEntry> snapshot = sink.Snapshot();

        Assert.Equal(RingCapacity, snapshot.Count);
        Assert.Equal("10", snapshot[0].RenderedMessage);
        Assert.Equal((RingCapacity + 9).ToString(), snapshot[snapshot.Count - 1].RenderedMessage);
    }

    [Fact]
    public void Clear_EmptiesTheBufferAndBumpsRevision()
    {
        ViewerLogSink sink = new ViewerLogSink();
        sink.Write(Entry(LogLevel.Info, "gone"));
        int before = sink.Revision;

        sink.Clear();

        Assert.Empty(sink.Snapshot());
        Assert.Equal(before + 1, sink.Revision);
    }

    [Fact]
    public void Clear_ThenWrite_StartsCountingFromTheBeginningAgain()
    {
        ViewerLogSink sink = new ViewerLogSink();
        sink.Write(Entry(LogLevel.Info, "old"));
        sink.Clear();

        sink.Write(Entry(LogLevel.Info, "new"));

        Assert.Single(sink.Snapshot());
        Assert.Equal("new", sink.Snapshot()[0].RenderedMessage);
    }

    [Fact]
    public void Write_SameEntryTwiceInARow_CollapsesIntoOneRowWithACount()
    {
        ViewerLogSink sink = new ViewerLogSink();
        for (int i = 0; i < 5; i++)
        {
            sink.Write(Entry(LogLevel.Info, "spam"));
        }

        IReadOnlyList<LogEntry> snapshot = sink.Snapshot();

        Assert.Single(snapshot);
        Assert.Equal(5, snapshot[0].Repeats);
    }

    [Fact]
    public void Write_DifferentMessageBetweenRepeats_StartsANewRow()
    {
        ViewerLogSink sink = new ViewerLogSink();
        sink.Write(Entry(LogLevel.Info, "spam"));
        sink.Write(Entry(LogLevel.Info, "other"));
        sink.Write(Entry(LogLevel.Info, "spam"));

        IReadOnlyList<LogEntry> snapshot = sink.Snapshot();

        Assert.Equal(3, snapshot.Count);
        Assert.All(snapshot, e => Assert.Equal(1, e.Repeats));
    }

    [Fact]
    public void Write_SameTextDifferentLevel_DoesNotCollapse()
    {
        ViewerLogSink sink = new ViewerLogSink();
        sink.Write(Entry(LogLevel.Info, "same"));
        sink.Write(Entry(LogLevel.Error, "same"));

        Assert.Equal(2, sink.Snapshot().Count);
    }

    [Fact]
    public void Write_RepeatsStillBumpRevisionSoTheViewerRefreshes()
    {
        ViewerLogSink sink = new ViewerLogSink();
        sink.Write(Entry(LogLevel.Info, "spam"));
        int afterFirst = sink.Revision;
        sink.Write(Entry(LogLevel.Info, "spam"));

        Assert.NotEqual(afterFirst, sink.Revision);
    }

    [Fact]
    public void Write_CollapsingARepeat_LeavesAnAlreadyReturnedEntryAlone()
    {
        // regression: collapsing used to do ring[last].Repeats++, mutating an entry that other
        // sinks and earlier snapshots still hold while another thread reads it
        ViewerLogSink sink = new ViewerLogSink();
        sink.Write(Entry(LogLevel.Info, "spam"));
        LogEntry handedOut = sink.Snapshot()[0];

        sink.Write(Entry(LogLevel.Info, "spam"));

        Assert.Equal(1, handedOut.Repeats);
        Assert.Equal(2, sink.Snapshot()[0].Repeats);
    }

    [Fact]
    public void Write_CollapsingARepeat_KeepsTheOriginalEntryFields()
    {
        ViewerLogSink sink = new ViewerLogSink();
        sink.Write(Entry(LogLevel.Warn, "same"));
        sink.Write(Entry(LogLevel.Warn, "same"));

        LogEntry collapsed = sink.Snapshot()[0];

        Assert.Equal(LogLevel.Warn, collapsed.Level);
        Assert.Equal("same", collapsed.RenderedMessage);
        Assert.Equal(2, collapsed.Repeats);
    }

    [Fact]
    public void Write_CollapsingARepeat_CopiesEveryPropertyExceptRepeats()
    {
        // regression: the hand-rolled clone dropped whichever property was added last, which is
        // how Tick and PatchedBy went missing. reflection so a new one cannot slip past.
        ViewerLogSink sink = new ViewerLogSink();
        LogEntry original = FullyPopulated();
        sink.Write(original);
        sink.Write(original);

        LogEntry collapsed = sink.Snapshot()[0];

        foreach (PropertyInfo prop in typeof(LogEntry).GetProperties())
        {
            // a value left at its default would match by accident and prove nothing
            Assert.NotNull(prop.GetValue(original));
            if (prop.Name == nameof(LogEntry.Repeats)) continue;
            Assert.Equal(prop.GetValue(original), prop.GetValue(collapsed));
        }

        Assert.Equal(2, collapsed.Repeats);
    }

    private static LogEntry FullyPopulated()
    {
        return new LogEntry
        {
            Timestamp = new System.DateTime(2026, 3, 4, 5, 6, 7, System.DateTimeKind.Utc),
            Level = LogLevel.Error,
            Channel = "Cosmere.Roshar",
            MessageTemplate = "bond formed with {Spren}",
            RenderedMessage = "bond formed with Syl",
            Context = new Dictionary<string, object?> { ["Spren"] = "Syl" },
            Source = new RimWorks.RimLogging.Capture.SourceLocation("Surgebinding.cs", 42, "Bond"),
            StackTrace = "at Surgebinding.Bond()",
            Exception = new System.InvalidOperationException("boom"),
            Mod = "Stormlight",
            Tick = 999,
            PatchedBy = new[] { "some.mod" },
            Repeats = 1,
        };
    }

    [Fact]
    public void Write_CollapsingARepeat_KeepsPatchedByNullWhenAttributionCouldNotRun()
    {
        // null means attribution never ran, empty means nothing patched it. dropping the field
        // defaulted it to empty, turning "we do not know" into "nobody did".
        ViewerLogSink sink = new ViewerLogSink();
        LogEntry entry = Entry(LogLevel.Error, "boom", patchedBy: null);
        sink.Write(entry);
        sink.Write(entry);

        Assert.Null(sink.Snapshot()[0].PatchedBy);
    }

    [Fact]
    public void ChannelTallies_CountRowsPerChannel()
    {
        ViewerLogSink sink = new ViewerLogSink();
        sink.Write(Entry(LogLevel.Info, "a", "Alpha"));
        sink.Write(Entry(LogLevel.Info, "b", "Alpha"));
        sink.Write(Entry(LogLevel.Info, "c", "Beta"));

        var tallies = sink.ChannelTallies();

        Assert.Equal(2, tallies["Alpha"].Count);
        Assert.Equal(1, tallies["Beta"].Count);
    }

    [Fact]
    public void ChannelTallies_ErrorCountTracksErrorsNotRows()
    {
        ViewerLogSink sink = new ViewerLogSink();
        sink.Write(Entry(LogLevel.Info, "fine", "Alpha"));
        sink.Write(Entry(LogLevel.Error, "bad", "Alpha"));

        Assert.Equal(2, sink.ChannelTallies()["Alpha"].Count);
        Assert.Equal(1, sink.ChannelTallies()["Alpha"].ErrorCount);
    }

    [Fact]
    public void ChannelTallies_EvictedEntriesAreSubtracted()
    {
        // the tally has to shrink as the ring wraps, or the tree counts drift up forever
        ViewerLogSink sink = new ViewerLogSink();
        for (int i = 0; i < RingCapacity + 50; i++)
        {
            sink.Write(Entry(LogLevel.Info, i.ToString(), "Alpha"));
        }

        Assert.Equal(RingCapacity, sink.ChannelTallies()["Alpha"].Count);
    }

    [Fact]
    public void ChannelTallies_ChannelDropsOutOnceEveryRowIsEvicted()
    {
        ViewerLogSink sink = new ViewerLogSink();
        sink.Write(Entry(LogLevel.Error, "old", "Gone"));
        for (int i = 0; i < RingCapacity; i++)
        {
            sink.Write(Entry(LogLevel.Info, i.ToString(), "Alpha"));
        }

        Assert.False(sink.ChannelTallies().ContainsKey("Gone"));
        Assert.Equal(0, sink.ChannelTallies()["Alpha"].ErrorCount);
    }

    [Fact]
    public void ChannelTallies_ClearEmptiesThem()
    {
        ViewerLogSink sink = new ViewerLogSink();
        sink.Write(Entry(LogLevel.Info, "x", "Alpha"));
        sink.Clear();

        Assert.Empty(sink.ChannelTallies());
    }

    [Fact]
    public void LevelTallies_CountRowsPerLevel()
    {
        ViewerLogSink sink = new ViewerLogSink();
        sink.Write(Entry(LogLevel.Info, "a"));
        sink.Write(Entry(LogLevel.Info, "b"));
        sink.Write(Entry(LogLevel.Warn, "c"));

        LevelCounts counts = sink.LevelTallies();

        Assert.Equal(2, counts.For(LogLevel.Info));
        Assert.Equal(1, counts.For(LogLevel.Warn));
        Assert.Equal(0, counts.For(LogLevel.Debug));
    }

    [Fact]
    public void LevelTallies_ErrorFoldsInFatal()
    {
        // the Error pill's toggle also flips Fatal, so its count has to include Fatal rows
        ViewerLogSink sink = new ViewerLogSink();
        sink.Write(Entry(LogLevel.Error, "a"));
        sink.Write(Entry(LogLevel.Fatal, "b"));

        Assert.Equal(2, sink.LevelTallies().For(LogLevel.Error));
    }

    [Fact]
    public void LevelTallies_MatchABruteForceScanOfTheSnapshot()
    {
        // risk mitigation from the toolbar spec: the incremental tally must not drift from a rescan
        ViewerLogSink sink = new ViewerLogSink();
        sink.Write(Entry(LogLevel.Trace, "a"));
        sink.Write(Entry(LogLevel.Warn, "b"));
        sink.Write(Entry(LogLevel.Error, "c"));
        sink.Write(Entry(LogLevel.Fatal, "d"));
        sink.Write(Entry(LogLevel.Warn, "e"));

        LevelCounts incremental = sink.LevelTallies();
        LevelCounts rescanned = LevelCounts.FromSnapshot(sink.Snapshot());

        foreach (LogLevel level in new[] { LogLevel.Trace, LogLevel.Debug, LogLevel.Info, LogLevel.Warn, LogLevel.Error, LogLevel.Fatal })
        {
            Assert.Equal(rescanned.For(level), incremental.For(level));
        }
    }

    [Fact]
    public void LevelTallies_EvictedEntriesAreSubtracted()
    {
        ViewerLogSink sink = new ViewerLogSink();
        for (int i = 0; i < RingCapacity + 50; i++)
        {
            sink.Write(Entry(LogLevel.Info, i.ToString()));
        }

        Assert.Equal(RingCapacity, sink.LevelTallies().For(LogLevel.Info));
    }

    [Fact]
    public void LevelTallies_ClearEmptiesThem()
    {
        ViewerLogSink sink = new ViewerLogSink();
        sink.Write(Entry(LogLevel.Error, "x"));
        sink.Clear();

        Assert.Equal(0, sink.LevelTallies().For(LogLevel.Error));
    }
}
