using System;
using RimWorks.RimLogging;
using Xunit;

namespace RimWorks.RimLogging.Tests;

public class LogErrorTests : LogSinkFixtureBase
{
    [Fact]
    public void Error_DefaultChannelTemplate_RoutesAtCorrectLevel()
    {
        Log.Error("error-level-test-sentinel");

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal(LogLevel.Error, entry!.Level);
        Assert.Equal("default", entry.Channel);
    }

    [Fact]
    public void Error_Exception_DefaultChannel_PopulatesEntryException()
    {
        Exception ex = new InvalidOperationException("error-ex-test");

        Log.Error(ex, "error-exception-message");

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal(LogLevel.Error, entry!.Level);
        Assert.Same(ex, entry.Exception);
    }

    [Fact]
    public void Error_Exception_ExplicitChannel_PopulatesEntryException()
    {
        Exception ex = new InvalidOperationException("error-ex-channel-test");

        Log.ErrorTo("error-chan", ex, "error-exception-channel-message");

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal(LogLevel.Error, entry!.Level);
        Assert.Equal("error-chan", entry.Channel);
        Assert.Same(ex, entry.Exception);
    }

    [Fact]
    public void Error_Exception_RenderedMessageIsMessageArg()
    {
        InvalidOperationException ex = new InvalidOperationException("boom");

        Log.Error(ex, "save failed");

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal("save failed", entry!.RenderedMessage);
        Assert.Same(ex, entry.Exception);
        Assert.IsType<InvalidOperationException>(entry.Exception);
        Assert.Equal("boom", entry.Exception.Message);
    }

    [Fact]
    public void Error_BelowGlobalMinLevel_IsDropped()
    {
        Logging.GlobalMinLevel = LogLevel.Fatal;
        int countBefore = _sink.Entries.Count;

        Log.Error("dropped-error-sentinel");

        Assert.Equal(countBefore, _sink.Entries.Count);
    }

    [Fact]
    public void Error_ExplicitChannel_RoutesChannelUnchanged()
    {
        Log.ErrorTo("error-audit", "explicit-channel-error-sentinel");

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal("error-audit", entry!.Channel);
        Assert.Equal(LogLevel.Error, entry.Level);
    }

    [Fact]
    public void ErrorOnceTo_ExplicitChannel_KeepsChannel()
    {
        Log.ErrorOnceTo("error-once-chan", "error-once-channel-key", "error-once-channel-sentinel");

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal("error-once-chan", entry!.Channel);
        Assert.Equal(LogLevel.Error, entry.Level);
    }

    [Fact]
    public void ErrorOnceTo_SameKeyTwice_EmitsOnlyOnce()
    {
        Log.ErrorOnceTo("error-once-repeat-chan", "error-once-repeat-key", "first");
        int countAfterFirst = _sink.Entries.Count;

        Log.ErrorOnceTo("error-once-repeat-chan", "error-once-repeat-key", "second");

        Assert.Equal(countAfterFirst, _sink.Entries.Count);
    }

    [Fact]
    public void ErrorOnce_WithoutChannel_StillLandsOnDefault()
    {
        Log.ErrorOnce("error-once-default-key", "error-once-default-sentinel");

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal("default", entry!.Channel);
        Assert.Equal(LogLevel.Error, entry.Level);
    }
}
