using System;
using System.Collections.Generic;
using RimWorks.RimLogging;
using RimWorks.RimLogging.Bundle;
using RimWorks.RimLogging.Viewer;
using Xunit;

namespace RimWorks.RimLogging.Tests.Viewer;

public class BundleShareSessionTests
{
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

    private static List<LogEntry> Entries(int count)
    {
        List<LogEntry> list = new List<LogEntry>();
        for (int i = 0; i < count; i++) list.Add(Entry("line " + i));
        return list;
    }

    private static PublishResult Created()
    {
        return new PublishResult { Success = true, Id = "bundle-1", Token = "token-1" };
    }

    [Fact]
    public void BeforeAnyUploadThereIsNothingToAppendTo()
    {
        BundleShareSession session = new BundleShareSession();

        Assert.False(session.CanAppend(Entries(3)));
        Assert.Null(session.Url);
    }

    [Fact]
    public void AfterAnUploadTheSameBufferCanBeAppendedTo()
    {
        List<LogEntry> snapshot = Entries(3);
        BundleShareSession session = new BundleShareSession();
        session.Record(Created(), "https://rimworks.org/logging?id=bundle-1", snapshot);

        snapshot.Add(Entry("new line"));

        Assert.True(session.CanAppend(snapshot));
        Assert.Equal("https://rimworks.org/logging?id=bundle-1", session.Url);
    }

    [Fact]
    public void OnlyTheUnsentTailIsPending()
    {
        List<LogEntry> snapshot = Entries(3);
        BundleShareSession session = new BundleShareSession();
        session.Record(Created(), "u", snapshot);

        LogEntry fresh = Entry("new line");
        snapshot.Add(fresh);

        List<LogEntry> pending = session.Pending(snapshot);
        Assert.Single(pending);
        Assert.Same(fresh, pending[0]);
    }

    [Fact]
    public void AnUploadWithNothingNewHasNothingPending()
    {
        List<LogEntry> snapshot = Entries(3);
        BundleShareSession session = new BundleShareSession();
        session.Record(Created(), "u", snapshot);

        Assert.True(session.CanAppend(snapshot));
        Assert.Empty(session.Pending(snapshot));
    }

    [Fact]
    public void ABufferThatDroppedOldEntriesCannotBeAppendedTo()
    {
        List<LogEntry> snapshot = Entries(3);
        BundleShareSession session = new BundleShareSession();
        session.Record(Created(), "u", snapshot);

        List<LogEntry> rolled = new List<LogEntry> { snapshot[1], snapshot[2], Entry("new line") };

        Assert.False(session.CanAppend(rolled));
    }

    [Fact]
    public void AShorterBufferCannotBeAppendedTo()
    {
        List<LogEntry> snapshot = Entries(3);
        BundleShareSession session = new BundleShareSession();
        session.Record(Created(), "u", snapshot);

        Assert.False(session.CanAppend(Entries(1)));
    }

    [Fact]
    public void ClearingForgetsTheBundle()
    {
        List<LogEntry> snapshot = Entries(3);
        BundleShareSession session = new BundleShareSession();
        session.Record(Created(), "u", snapshot);

        session.Clear();

        Assert.False(session.CanAppend(snapshot));
        Assert.Null(session.Id);
        Assert.Null(session.Token);
        Assert.Null(session.Url);
    }
}
