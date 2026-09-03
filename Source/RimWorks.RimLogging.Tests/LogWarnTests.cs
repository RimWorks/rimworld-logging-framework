using System;
using RimWorks.RimLogging;
using Xunit;

namespace RimWorks.RimLogging.Tests;

public class LogWarnTests : LogSinkFixtureBase
{
    [Fact]
    public void Warn_DefaultChannelTemplate_RoutesAtCorrectLevel()
    {
        Log.Warn("warn-level-test-sentinel");

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal(LogLevel.Warn, entry!.Level);
        Assert.Equal("default", entry.Channel);
    }

    [Fact]
    public void Warn_Exception_DefaultChannel_PopulatesEntryException()
    {
        Exception ex = new InvalidOperationException("warn-ex-test");

        Log.Warn(ex, "warn-exception-message");

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal(LogLevel.Warn, entry!.Level);
        Assert.Same(ex, entry.Exception);
    }

    [Fact]
    public void Warn_Exception_ExplicitChannel_PopulatesEntryException()
    {
        Exception ex = new InvalidOperationException("warn-ex-channel-test");

        Log.WarnTo("warn-chan", ex, "warn-exception-channel-message");

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal(LogLevel.Warn, entry!.Level);
        Assert.Equal("warn-chan", entry.Channel);
        Assert.Same(ex, entry.Exception);
    }

    [Fact]
    public void Warn_BelowGlobalMinLevel_IsDropped()
    {
        Logging.GlobalMinLevel = LogLevel.Error;
        int countBefore = _sink.Entries.Count;

        Log.Warn("dropped-warn-sentinel");

        Assert.Equal(countBefore, _sink.Entries.Count);
    }

    [Fact]
    public void Warn_ExplicitChannel_RoutesChannelUnchanged()
    {
        Log.WarnTo("warn-audit", "explicit-channel-warn-sentinel");

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal("warn-audit", entry!.Channel);
        Assert.Equal(LogLevel.Warn, entry.Level);
    }

    [Fact]
    public void WarnOnceTo_ExplicitChannel_KeepsChannel()
    {
        Log.WarnOnceTo("warn-once-chan", "warn-once-channel-key", "warn-once-channel-sentinel");

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal("warn-once-chan", entry!.Channel);
        Assert.Equal(LogLevel.Warn, entry.Level);
    }

    [Fact]
    public void WarnOnceTo_SameKeyTwice_EmitsOnlyOnce()
    {
        Log.WarnOnceTo("warn-once-repeat-chan", "warn-once-repeat-key", "first");
        int countAfterFirst = _sink.Entries.Count;

        Log.WarnOnceTo("warn-once-repeat-chan", "warn-once-repeat-key", "second");

        Assert.Equal(countAfterFirst, _sink.Entries.Count);
    }

    [Fact]
    public void WarnOnce_WithoutChannel_StillLandsOnDefault()
    {
        Log.WarnOnce("warn-once-default-key", "warn-once-default-sentinel");

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal("default", entry!.Channel);
        Assert.Equal(LogLevel.Warn, entry.Level);
    }
}
