using System;
using System.Collections.Generic;
using RimWorks.RimLogging;
using RimWorks.RimLogging.Channels;
using RimWorks.RimLogging.Sinks;
using Xunit;

namespace RimWorks.RimLogging.Tests.Sinks;

public class SinkRoutingTests : IDisposable
{
    public SinkRoutingTests() => SinkRegistry.DisposeAll();

    public void Dispose()
    {
        Logging.ChannelSettingsProvider = null;
        SinkRegistry.DisposeAll();
        GC.SuppressFinalize(this);
    }

    private static void Route(params string[] destinations)
        => Logging.ChannelSettingsProvider =
            _ => new ChannelSettings(null, null, new List<string>(destinations), null);

    private static LogEntry Entry() => new LogEntry
    {
        Timestamp = DateTime.UtcNow,
        Level = LogLevel.Info,
        Channel = "test",
        MessageTemplate = "m",
        RenderedMessage = "m",
        Context = null,
        Source = default,
        StackTrace = null,
        Exception = null,
    };

    [Fact]
    public void NoDestinations_EverySinkGetsTheEntry()
    {
        CountingSink a = new CountingSink("Memory");
        CountingSink b = new CountingSink("VerseLog");
        SinkRegistry.Register(a);
        SinkRegistry.Register(b);

        SinkRegistry.DispatchSynchronously(Entry());

        Assert.Equal(1, a.Writes);
        Assert.Equal(1, b.Writes);
    }

    [Fact]
    public void NamedDestination_OnlyThatSinkGetsTheEntry()
    {
        CountingSink a = new CountingSink("Memory");
        CountingSink b = new CountingSink("VerseLog");
        SinkRegistry.Register(a);
        SinkRegistry.Register(b);
        Route("Memory");

        SinkRegistry.DispatchSynchronously(Entry());

        Assert.Equal(1, a.Writes);
        Assert.Equal(0, b.Writes);
    }

    [Fact]
    public void UnknownDestination_FailsOpenRatherThanGoingDark()
    {
        // a typo in xml must not silently mute the channel everywhere
        CountingSink a = new CountingSink("Memory");
        CountingSink b = new CountingSink("VerseLog");
        SinkRegistry.Register(a);
        SinkRegistry.Register(b);
        Route("Memry");

        SinkRegistry.DispatchSynchronously(Entry());

        Assert.Equal(1, a.Writes);
        Assert.Equal(1, b.Writes);
    }

    [Fact]
    public void PartlyUnknownDestination_StillRestrictsToTheValidOne()
    {
        CountingSink a = new CountingSink("Memory");
        CountingSink b = new CountingSink("VerseLog");
        SinkRegistry.Register(a);
        SinkRegistry.Register(b);
        Route("Memory", "Typo");

        SinkRegistry.DispatchSynchronously(Entry());

        Assert.Equal(1, a.Writes);
        Assert.Equal(0, b.Writes);
    }

    private sealed class CountingSink : ILogSink
    {
        public CountingSink(string name) => Name = name;

        public string Name { get; }
        public LogLevel MinLevel => LogLevel.Trace;
        public int Writes { get; private set; }

        public void Write(LogEntry entry) => Writes++;
        public void Flush() { }
        public void Dispose() { }
    }
}
