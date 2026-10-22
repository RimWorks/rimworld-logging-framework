using System;
using System.Collections.Generic;
using RimWorks.RimLogging;
using RimWorks.RimLogging.Viewer;
using Xunit;

namespace RimWorks.RimLogging.Tests.Viewer;

public class ShareLinkTests
{
    private const string Viewer = "https://rimworks.org/logging?id=3cf768ed-e957-4383-a511-048d13c5885a";

    private static LogEntry Entry(string message)
    {
        return new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = LogLevel.Info,
            Channel = "Vanilla",
            RenderedMessage = message,
        };
    }

    [Fact]
    public void IndexOf_ReturnsThePositionInTheSnapshot()
    {
        LogEntry wanted = Entry("the one being shared");
        List<LogEntry> snapshot = new List<LogEntry> { Entry("before"), wanted, Entry("after") };

        Assert.Equal(1, ShareLink.IndexOf(snapshot, wanted));
    }

    [Fact]
    public void IndexOf_MatchesByReferenceSoTwoIdenticalEntriesStayApart()
    {
        LogEntry first = Entry("same text");
        LogEntry second = Entry("same text");
        List<LogEntry> snapshot = new List<LogEntry> { first, second };

        Assert.Equal(0, ShareLink.IndexOf(snapshot, first));
        Assert.Equal(1, ShareLink.IndexOf(snapshot, second));
    }

    [Fact]
    public void IndexOf_ReturnsMinusOneWhenTheRingNoLongerHoldsTheEntry()
    {
        List<LogEntry> snapshot = new List<LogEntry> { Entry("still here") };

        Assert.Equal(-1, ShareLink.IndexOf(snapshot, Entry("rolled out")));
    }

    [Fact]
    public void IndexOf_ReturnsMinusOneForNoEntry()
    {
        Assert.Equal(-1, ShareLink.IndexOf(new List<LogEntry> { Entry("anything") }, null));
    }

    [Fact]
    public void WithLine_AppendsToAUrlThatAlreadyCarriesAQuery()
    {
        Assert.Equal(Viewer + "&line=40", ShareLink.WithLine(Viewer, 40));
    }

    [Fact]
    public void WithLine_OpensTheQueryOnAUrlWithoutOne()
    {
        Assert.Equal("https://rimworks.org/logging?line=0", ShareLink.WithLine("https://rimworks.org/logging", 0));
    }

    [Fact]
    public void WithLine_LeavesTheUrlAloneWhenTheEntryWasNotFound()
    {
        Assert.Equal(Viewer, ShareLink.WithLine(Viewer, -1));
    }

    [Fact]
    public void WithLine_LeavesAnEmptyUrlAlone()
    {
        Assert.Equal(string.Empty, ShareLink.WithLine(string.Empty, 3));
    }
}
