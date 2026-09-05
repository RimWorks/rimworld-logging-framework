using System;
using System.Collections.Generic;
using RimWorks.RimLogging.Viewer;
using Xunit;

namespace RimWorks.RimLogging.Tests.Viewer;

public class LoadedTalliesTests
{
    private static LogEntry Entry(string channel, LogLevel level) => new LogEntry
    {
        Timestamp = new DateTime(2026, 1, 1),
        Level = level,
        Channel = channel,
        MessageTemplate = "m",
        RenderedMessage = "m",
    };

    [Fact]
    public void TalliesFrom_CountsRowsPerChannel()
    {
        var tallies = LogFilter.TalliesFrom([
            Entry("Alpha", LogLevel.Info),
            Entry("Alpha", LogLevel.Info),
            Entry("Beta", LogLevel.Info),
        ]);

        Assert.Equal(2, tallies[LogFilter.KeyFor("Alpha")].Count);
        Assert.Equal(1, tallies[LogFilter.KeyFor("Beta")].Count);
    }

    [Fact]
    public void TalliesFrom_CountsErrorsSeparately()
    {
        var tallies = LogFilter.TalliesFrom([
            Entry("Alpha", LogLevel.Info),
            Entry("Alpha", LogLevel.Error),
            Entry("Alpha", LogLevel.Fatal),
        ]);

        Assert.Equal(3, tallies[LogFilter.KeyFor("Alpha")].Count);
        Assert.Equal(2, tallies[LogFilter.KeyFor("Alpha")].ErrorCount);
    }

    [Fact]
    public void TalliesFrom_EmptySnapshot_IsEmpty()
    {
        Assert.Empty(LogFilter.TalliesFrom([]));
    }

    [Fact]
    public void TalliesFrom_MatchesWhatTheLiveSinkWouldReport()
    {
        // a loaded file has no sink behind it, so this walk must agree with ViewerLogSink's
        // running tallies or the channel tree would differ between live and loaded mode
        LogEntry[] entries = [
            Entry("Cosmere.Roshar", LogLevel.Info),
            Entry("Cosmere.Roshar", LogLevel.Error),
            Entry("Vanilla", LogLevel.Warn),
        ];

        ViewerLogSink sink = new ViewerLogSink();
        foreach (LogEntry e in entries) sink.Write(e);

        var walked = LogFilter.TalliesFrom(entries);
        var live = sink.ChannelTallies();

        Assert.Equal(live.Count, walked.Count);
        foreach (KeyValuePair<string, ChannelTally> pair in live)
        {
            Assert.Equal(pair.Value.Count, walked[pair.Key].Count);
            Assert.Equal(pair.Value.ErrorCount, walked[pair.Key].ErrorCount);
        }
    }
}
