using System;
using RimWorks.RimLogging;
using Xunit;

namespace RimWorks.RimLogging.Tests;

public class LogDebugTests : LogSinkFixtureBase
{
    [Fact]
    public void Debug_DefaultChannelTemplate_RoutesAtCorrectLevel()
    {
        Log.Debug("debug-level-test-sentinel");

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal(LogLevel.Debug, entry!.Level);
        Assert.Equal("default", entry.Channel);
    }

    [Fact]
    public void Debug_Exception_DefaultChannel_PopulatesEntryException()
    {
        Exception ex = new InvalidOperationException("debug-ex-test");

        Log.Debug(ex, "debug-exception-message");

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal(LogLevel.Debug, entry!.Level);
        Assert.Same(ex, entry.Exception);
    }

    [Fact]
    public void Debug_Exception_ExplicitChannel_PopulatesEntryException()
    {
        Exception ex = new InvalidOperationException("debug-ex-channel-test");

        Log.DebugTo("debug-chan", ex, "debug-exception-channel-message");

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal(LogLevel.Debug, entry!.Level);
        Assert.Equal("debug-chan", entry.Channel);
        Assert.Same(ex, entry.Exception);
    }

    [Fact]
    public void Debug_BelowGlobalMinLevel_IsDropped()
    {
        Logging.GlobalMinLevel = LogLevel.Info;
        int countBefore = _sink.Entries.Count;

        Log.Debug("dropped-debug-sentinel");

        Assert.Equal(countBefore, _sink.Entries.Count);
    }

    [Fact]
    public void Debug_ExplicitChannel_RoutesChannelUnchanged()
    {
        Log.DebugTo("debug-audit", "explicit-channel-debug-sentinel");

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal("debug-audit", entry!.Channel);
        Assert.Equal(LogLevel.Debug, entry.Level);
    }
}
