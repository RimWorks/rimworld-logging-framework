using System;
using RimWorks.RimLogging;
using Xunit;

namespace RimWorks.RimLogging.Tests;

public class LogTraceTests : LogSinkFixtureBase
{
    [Fact]
    public void Trace_DefaultChannelTemplate_RoutesAtCorrectLevel()
    {
        Log.Trace("trace-level-test-sentinel");

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal(LogLevel.Trace, entry!.Level);
        Assert.Equal("default", entry.Channel);
    }

    [Fact]
    public void Trace_Exception_DefaultChannel_PopulatesEntryException()
    {
        Exception ex = new InvalidOperationException("trace-ex-test");

        Log.Trace(ex, "trace-exception-message");

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal(LogLevel.Trace, entry!.Level);
        Assert.Same(ex, entry.Exception);
    }

    [Fact]
    public void Trace_Exception_ExplicitChannel_PopulatesEntryException()
    {
        Exception ex = new InvalidOperationException("trace-ex-channel-test");

        Log.Trace("trace-chan", ex, "trace-exception-channel-message");

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal(LogLevel.Trace, entry!.Level);
        Assert.Equal("trace-chan", entry.Channel);
        Assert.Same(ex, entry.Exception);
    }

    [Fact]
    public void Trace_BelowGlobalMinLevel_IsDropped()
    {
        Logging.GlobalMinLevel = LogLevel.Debug;
        int countBefore = _sink.Entries.Count;

        Log.Trace("dropped-trace-sentinel");

        Assert.Equal(countBefore, _sink.Entries.Count);
    }

    [Fact]
    public void Trace_ExplicitChannel_RoutesChannelUnchanged()
    {
        Log.Trace("trace-audit", "explicit-channel-trace-sentinel");

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal("trace-audit", entry!.Channel);
        Assert.Equal(LogLevel.Trace, entry.Level);
    }
}
