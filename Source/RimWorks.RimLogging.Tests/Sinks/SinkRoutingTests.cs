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
        SinkRegistry.Register(a, defName: "Memory");
        SinkRegistry.Register(b, defName: "VerseLog");

        SinkRegistry.DispatchSynchronously(Entry());

        Assert.Equal(1, a.Writes);
        Assert.Equal(1, b.Writes);
    }

    [Fact]
    public void NamedDestination_OnlyThatSinkGetsTheEntry()
    {
        CountingSink a = new CountingSink("Memory");
        CountingSink b = new CountingSink("VerseLog");
        SinkRegistry.Register(a, defName: "Memory");
        SinkRegistry.Register(b, defName: "VerseLog");
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
        SinkRegistry.Register(a, defName: "Memory");
        SinkRegistry.Register(b, defName: "VerseLog");
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
        SinkRegistry.Register(a, defName: "Memory");
        SinkRegistry.Register(b, defName: "VerseLog");
        Route("Memory", "Typo");

        SinkRegistry.DispatchSynchronously(Entry());

        Assert.Equal(1, a.Writes);
        Assert.Equal(0, b.Writes);
    }

    [Fact]
    public void CodeRegisteredSink_IsNotFilteredByDestinations()
    {
        // destinations name SinkDefs, and a sink registered from code never had one
        CountingSink loaded = new CountingSink("VerseLog");
        CountingSink code = new CountingSink("Memory");
        SinkRegistry.Register(loaded, defName: "VerseLog");
        SinkRegistry.Register(code);
        Route("VerseLog");

        SinkRegistry.DispatchSynchronously(Entry());

        Assert.Equal(1, loaded.Writes);
        Assert.Equal(1, code.Writes);
    }

    [Fact]
    public void CodeRegisteredSink_DoesNotSatisfyADestinationList()
    {
        CountingSink code = new CountingSink("Memory");
        CountingSink loaded = new CountingSink("VerseLog");
        SinkRegistry.Register(code);
        SinkRegistry.Register(loaded, defName: "VerseLog");
        Route("Memory");

        SinkRegistry.DispatchSynchronously(Entry());

        Assert.Equal(1, code.Writes);
        Assert.Equal(1, loaded.Writes);
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
