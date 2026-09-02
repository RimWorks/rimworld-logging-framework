using System;
using System.Collections.Generic;
using RimWorks.RimLogging.Viewer;
using Xunit;

namespace RimWorks.RimLogging.Tests.Viewer;

public class LevelCountsTests
{
    private static LogEntry Entry(LogLevel level)
    {
        return new LogEntry
        {
            Timestamp = new DateTime(2026, 1, 1),
            Level = level,
            Channel = "Test",
            MessageTemplate = "x",
            RenderedMessage = "x",
        };
    }

    [Fact]
    public void FromSnapshot_CountsEachLevel()
    {
        List<LogEntry> snapshot = new List<LogEntry>
        {
            Entry(LogLevel.Info),
            Entry(LogLevel.Info),
            Entry(LogLevel.Warn),
        };

        LevelCounts counts = LevelCounts.FromSnapshot(snapshot);

        Assert.Equal(2, counts.For(LogLevel.Info));
        Assert.Equal(1, counts.For(LogLevel.Warn));
    }

    [Fact]
    public void FromSnapshot_EmptySnapshot_EveryLevelIsZero()
    {
        LevelCounts counts = LevelCounts.FromSnapshot(new List<LogEntry>());

        Assert.Equal(0, counts.For(LogLevel.Trace));
        Assert.Equal(0, counts.For(LogLevel.Fatal));
    }

    [Fact]
    public void For_Error_IncludesFatal()
    {
        List<LogEntry> snapshot = new List<LogEntry> { Entry(LogLevel.Error), Entry(LogLevel.Fatal), Entry(LogLevel.Fatal) };

        LevelCounts counts = LevelCounts.FromSnapshot(snapshot);

        Assert.Equal(3, counts.For(LogLevel.Error));
    }

    [Fact]
    public void For_OtherLevels_DoNotIncludeFatal()
    {
        List<LogEntry> snapshot = new List<LogEntry> { Entry(LogLevel.Fatal) };

        LevelCounts counts = LevelCounts.FromSnapshot(snapshot);

        Assert.Equal(0, counts.For(LogLevel.Warn));
    }

    [Fact]
    public void Default_EveryLevelIsZero()
    {
        LevelCounts counts = default;

        Assert.Equal(0, counts.For(LogLevel.Error));
    }
}
