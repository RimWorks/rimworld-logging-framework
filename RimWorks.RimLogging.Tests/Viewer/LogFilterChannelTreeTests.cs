using System;
using System.Collections.Generic;
using System.Linq;
using RimWorks.RimLogging.Viewer;
using Xunit;

namespace RimWorks.RimLogging.Tests.Viewer;

public class LogFilterChannelTreeTests
{
    private static readonly ChannelLabels Labels = new ChannelLabels("ALL", "MOD", "VANILLA");

    private static LogViewerState FreshState()
    {
        ChannelClassifier.Reset();
        ChannelClassifier.UseModTable(null);
        ChannelClassifier.EnsureBuilt();

        LogViewerSession.DslSource = "";
        LogViewerSession.DslError = null;
        LogViewerSession.ChannelFilter = "";

        return new LogViewerState();
    }

    private static LogEntry Entry(LogLevel level, string channel)
    {
        return new LogEntry
        {
            Timestamp = new DateTime(2026, 1, 1),
            Level = level,
            Channel = channel,
            MessageTemplate = "m",
            RenderedMessage = "m",
        };
    }

    private static readonly LogEntry[] Sample =
    {
        Entry(LogLevel.Info, "Cosmere.Roshar"),
        Entry(LogLevel.Info, "Cosmere.Roshar"),
        Entry(LogLevel.Error, "Cosmere.Scadrial"),
        Entry(LogLevel.Info, "Vanilla"),
    };

    private static LogChannel Find(List<LogChannel> channels, string id)
    {
        return channels.First(c => c.Id == id);
    }

    [Fact]
    public void BuildChannels_AlwaysLeadsWithTheAllRowCarryingTheTotalCount()
    {
        LogViewerState state = FreshState();

        List<LogChannel> channels = LogFilter.BuildChannels(Sample, state, Labels);

        Assert.Equal(LogViewerState.AllChannels, channels[0].Id);
        Assert.Equal("ALL", channels[0].Name);
        Assert.Equal(Sample.Length, channels[0].Count);
    }

    [Fact]
    public void BuildChannels_RollsDescendantCountsUpIntoAncestors()
    {
        LogViewerState state = FreshState();

        List<LogChannel> channels = LogFilter.BuildChannels(Sample, state, Labels);

        Assert.Equal(3, Find(channels, "Mod").Count);
        Assert.Equal(3, Find(channels, "Mod/Cosmere").Count);
        Assert.Equal(2, Find(channels, "Mod/Cosmere/Roshar").Count);
    }

    [Fact]
    public void BuildChannels_MarksAnAncestorAsErroredWhenAnyDescendantErrored()
    {
        LogViewerState state = FreshState();

        List<LogChannel> channels = LogFilter.BuildChannels(Sample, state, Labels);

        Assert.True(Find(channels, "Mod/Cosmere").HasError);
        Assert.True(Find(channels, "Mod/Cosmere/Scadrial").HasError);
        Assert.False(Find(channels, "Mod/Cosmere/Roshar").HasError);
    }

    [Fact]
    public void BuildChannels_UsesTheSuppliedGroupLabelsInsteadOfRawIds()
    {
        LogViewerState state = FreshState();

        List<LogChannel> channels = LogFilter.BuildChannels(Sample, state, Labels);

        Assert.Equal("MOD", Find(channels, "Mod").Name);
        Assert.Equal("VANILLA", Find(channels, "Vanilla").Name);
    }

    [Fact]
    public void BuildChannels_CollapsedAncestor_HidesItsDescendants()
    {
        LogViewerState state = FreshState();
        state.ExpandedChannels["Mod/Cosmere"] = false;

        List<LogChannel> channels = LogFilter.BuildChannels(Sample, state, Labels);

        Assert.Contains(channels, c => c.Id == "Mod/Cosmere");
        Assert.DoesNotContain(channels, c => c.Id == "Mod/Cosmere/Roshar");
    }

    [Fact]
    public void BuildChannels_ChannelFilter_KeepsMatchesAndTheirAncestors()
    {
        LogViewerState state = FreshState();
        state.ChannelFilter = "roshar";

        List<LogChannel> channels = LogFilter.BuildChannels(Sample, state, Labels);

        Assert.Contains(channels, c => c.Id == "Mod/Cosmere/Roshar");
        Assert.Contains(channels, c => c.Id == "Mod/Cosmere");
        Assert.DoesNotContain(channels, c => c.Id == "Mod/Cosmere/Scadrial");
    }
}
