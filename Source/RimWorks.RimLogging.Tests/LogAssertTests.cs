using RimWorks.RimLogging;
using Xunit;

namespace RimWorks.RimLogging.Tests;

public class LogAssertTests : LogSinkFixtureBase
{
    [Fact]
    public void Assert_TrueCondition_WritesNoEntry()
    {
        int countBefore = _sink.Entries.Count;

        Log.Assert(true, "assert-true-sentinel");

        Assert.Equal(countBefore, _sink.Entries.Count);
    }

    [Fact]
    public void Assert_FalseCondition_WritesOneErrorWithMessage()
    {
        int countBefore = _sink.Entries.Count;

        Log.Assert(false, "pawn has no map", line: 77, file: "/proj/Foo.cs");

        Assert.Equal(countBefore + 1, _sink.Entries.Count);
        LogEntry entry = _sink.Entries[_sink.Entries.Count - 1];
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal("default", entry.Channel);
        Assert.Equal("Assertion failed: pawn has no map", entry.RenderedMessage);
        Assert.Equal(77, entry.Source.Line);
        Assert.Equal("proj/Foo", entry.Source.File);
    }

    [Fact]
    public void Assert_NoMessage_UsesConditionText()
    {
        int x = 3;

        Log.Assert(x > 5);

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal("Assertion failed: x > 5", entry.RenderedMessage);
    }

    [Fact]
    public void AssertTo_FalseCondition_WritesOnChannel()
    {
        Log.AssertTo("assert-chan", false, "assert-channel-sentinel");

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal("assert-chan", entry.Channel);
        Assert.Equal("Assertion failed: assert-channel-sentinel", entry.RenderedMessage);
    }

    [Fact]
    public void Assert_FalseCondition_CallerKeepsRunning()
    {
        bool reached = false;

        Log.Assert(false, "assert-continue-sentinel");
        reached = true;

        Assert.True(reached);
    }
}
