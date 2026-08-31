using System;
using System.Collections.Generic;
using RimWorks.RimLogging.Viewer;
using Xunit;

namespace RimWorks.RimLogging.Tests.Viewer;

public class LogFilterApplyTests
{
    private static LogViewerState FreshState()
    {
        ChannelClassifier.Reset();
        ChannelClassifier.ModTableLoader = null;
        ChannelClassifier.EnsureBuilt();

        LogViewerSession.DslSource = "";
        LogViewerSession.DslError = null;
        LogViewerSession.ChannelFilter = "";
        bool[] levels = LogViewerSession.Levels;
        for (int i = 0; i < levels.Length; i++) levels[i] = true;

        return new LogViewerState();
    }

    private static LogEntry Entry(LogLevel level, string channel, string message = "m")
    {
        return new LogEntry
        {
            Timestamp = new DateTime(2026, 1, 1),
            Level = level,
            Channel = channel,
            MessageTemplate = message,
            RenderedMessage = message,
        };
    }

    private static readonly LogEntry[] Sample =
    {
        Entry(LogLevel.Trace, "Cosmere.Roshar", "trace"),
        Entry(LogLevel.Info, "Cosmere.Roshar", "info"),
        Entry(LogLevel.Warn, "Cosmere.Scadrial", "warn"),
        Entry(LogLevel.Error, "Vanilla", "error"),
    };

    [Fact]
    public void Apply_AllLevelsOnAndNoFilters_ReturnsEverything()
    {
        LogViewerState state = FreshState();

        Assert.Equal(Sample.Length, LogFilter.Apply(Sample, state).Count);
    }

    [Fact]
    public void Apply_LevelToggledOff_DropsEntriesAtThatLevel()
    {
        LogViewerState state = FreshState();
        state.Levels[(int)LogLevel.Trace] = false;

        List<LogEntry> result = LogFilter.Apply(Sample, state);

        Assert.DoesNotContain(result, e => e.Level == LogLevel.Trace);
        Assert.Equal(Sample.Length - 1, result.Count);
    }

    [Fact]
    public void Apply_ActiveChannelSetToAParent_KeepsDescendantsOnly()
    {
        LogViewerState state = FreshState();
        state.ActiveChannel = ChannelClassifier.JoinPath(ChannelClassifier.PathFor("Cosmere.Roshar"));

        List<LogEntry> result = LogFilter.Apply(Sample, state);

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal("Cosmere.Roshar", e.Channel));
    }

    [Fact]
    public void Apply_ValidDslExpression_NarrowsToMatchingEntries()
    {
        LogViewerState state = FreshState();
        state.DslSource = "level >= Warn";

        List<LogEntry> result = LogFilter.Apply(Sample, state);

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.True(e.Level >= LogLevel.Warn));
    }

    [Fact]
    public void Apply_DslWithAParseError_IsIgnoredRatherThanThrowing()
    {
        LogViewerState state = FreshState();
        state.DslSource = "level >= ";
        state.DslError = "unexpected end of expression";

        List<LogEntry> result = LogFilter.Apply(Sample, state);

        Assert.Equal(Sample.Length, result.Count);
    }

    [Fact]
    public void Apply_LevelToggleAndDsl_BothApply()
    {
        LogViewerState state = FreshState();
        state.DslSource = "level >= Warn";
        state.Levels[(int)LogLevel.Error] = false;

        List<LogEntry> result = LogFilter.Apply(Sample, state);

        Assert.Single(result);
        Assert.Equal(LogLevel.Warn, result[0].Level);
    }
}
