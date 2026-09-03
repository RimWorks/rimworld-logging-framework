using System;
using System.Collections.Generic;
using RimWorks.RimLogging.Capture;
using RimWorks.RimLogging.Viewer;
using Xunit;

namespace RimWorks.RimLogging.Tests.Viewer;

public class ErrorJumpTests
{
    private static LogEntry At(LogLevel level) => new LogEntry
    {
        Timestamp = DateTime.UtcNow,
        Level = level,
        Channel = "c",
        MessageTemplate = "m",
        RenderedMessage = "m",
        Context = null,
        Source = SourceLocation.Empty,
        StackTrace = null,
        Exception = null,
    };

    private static List<LogEntry> List(params LogLevel[] levels)
    {
        List<LogEntry> entries = new List<LogEntry>();
        foreach (LogLevel l in levels) entries.Add(At(l));
        return entries;
    }

    [Fact]
    public void Next_FindsTheFollowingError()
    {
        List<LogEntry> entries = List(LogLevel.Info, LogLevel.Error, LogLevel.Info, LogLevel.Error);

        Assert.Equal(1, ErrorJump.Next(entries, -1, LogLevel.Error));
        Assert.Equal(3, ErrorJump.Next(entries, 1, LogLevel.Error));
    }

    [Fact]
    public void Next_WrapsSoRepeatedPressesCycle()
    {
        List<LogEntry> entries = List(LogLevel.Error, LogLevel.Info, LogLevel.Error);

        Assert.Equal(0, ErrorJump.Next(entries, 2, LogLevel.Error));
    }

    [Fact]
    public void Next_SingleError_StaysReachable()
    {
        List<LogEntry> entries = List(LogLevel.Info, LogLevel.Error, LogLevel.Info);

        Assert.Equal(1, ErrorJump.Next(entries, 1, LogLevel.Error));
    }

    [Fact]
    public void Next_FatalCountsAsAtOrAboveError()
    {
        List<LogEntry> entries = List(LogLevel.Info, LogLevel.Fatal);

        Assert.Equal(1, ErrorJump.Next(entries, -1, LogLevel.Error));
    }

    [Fact]
    public void Next_NothingSevereEnough_ReturnsMinusOne()
    {
        Assert.Equal(-1, ErrorJump.Next(List(LogLevel.Info, LogLevel.Warn), -1, LogLevel.Error));
    }

    [Fact]
    public void Next_EmptyList_ReturnsMinusOne()
    {
        Assert.Equal(-1, ErrorJump.Next(new List<LogEntry>(), -1, LogLevel.Error));
    }

    [Fact]
    public void ScrollTo_PutsTheRowInView()
    {
        // row 100 at 22px, 440px viewport, a third down means about 2054
        float y = ErrorJump.ScrollTo(100, 22f, 440f, 500);

        Assert.InRange(y, 2000f, 2100f);
    }

    [Fact]
    public void ScrollTo_NearTheTop_ClampsToZero()
    {
        Assert.Equal(0f, ErrorJump.ScrollTo(1, 22f, 440f, 500));
    }

    [Fact]
    public void ScrollTo_ShorterThanTheViewport_ClampsToZero()
    {
        Assert.Equal(0f, ErrorJump.ScrollTo(2, 22f, 440f, 3));
    }

    [Fact]
    public void ScrollTo_NearTheEnd_ClampsToTheBottom()
    {
        float y = ErrorJump.ScrollTo(499, 22f, 440f, 500);

        Assert.Equal(500 * 22f - 440f, y);
    }
}
