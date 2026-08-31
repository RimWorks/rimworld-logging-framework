using System.Collections.Generic;
using RimWorks.RimLogging.Viewer;
using Xunit;

namespace RimWorks.RimLogging.Tests.Viewer;

public class ViewerLogSinkTests
{
    private const int RingCapacity = 4096;

    private static LogEntry Entry(LogLevel level, string message)
    {
        return new LogEntry
        {
            Timestamp = new System.DateTime(2026, 1, 1),
            Level = level,
            Channel = "Test",
            MessageTemplate = message,
            RenderedMessage = message,
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

        Assert.Equal(new[] { "first", "second", "third" }, new[] { snapshot[0].RenderedMessage, snapshot[1].RenderedMessage, snapshot[2].RenderedMessage });
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
}
