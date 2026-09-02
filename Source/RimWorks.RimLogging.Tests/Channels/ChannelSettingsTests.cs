using RimWorks.RimLogging;
using RimWorks.RimLogging.Channels;
using Xunit;

namespace RimWorks.RimLogging.Tests.Channels;

public class ChannelSettingsTests
{
    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Error)]
    public void MinLevelOr_NoOverride_FollowsTheGlobal(LogLevel global)
    {
        Assert.Equal(global, ChannelSettings.Inherit.MinLevelOr(global));
    }

    [Fact]
    public void MinLevelOr_Override_WinsOverTheGlobal()
    {
        ChannelSettings s = new ChannelSettings(LogLevel.Warn, null);

        Assert.Equal(LogLevel.Warn, s.MinLevelOr(LogLevel.Debug));
    }

    [Fact]
    public void MinLevelOr_OverrideBelowGlobal_IsStillHonoured()
    {
        // a channel may ask for more than the global, the emit path applies the global gate first
        ChannelSettings s = new ChannelSettings(LogLevel.Trace, null);

        Assert.Equal(LogLevel.Trace, s.MinLevelOr(LogLevel.Error));
    }

    [Fact]
    public void ShouldCaptureStack_GlobalOff_NeverCaptures()
    {
        ChannelSettings s = new ChannelSettings(null, LogLevel.Trace);

        Assert.False(s.ShouldCaptureStack(LogLevel.Fatal, globalCapture: false));
    }

    [Fact]
    public void ShouldCaptureStack_NoOverride_CapturesEverything()
    {
        // today's behaviour has to survive untouched for channels that set nothing
        Assert.True(ChannelSettings.Inherit.ShouldCaptureStack(LogLevel.Trace, globalCapture: true));
    }

    [Theory]
    [InlineData(LogLevel.Info, false)]
    [InlineData(LogLevel.Warn, false)]
    [InlineData(LogLevel.Error, true)]
    [InlineData(LogLevel.Fatal, true)]
    public void ShouldCaptureStack_Threshold_CapturesAtOrAbove(LogLevel level, bool expected)
    {
        ChannelSettings s = new ChannelSettings(null, LogLevel.Error);

        Assert.Equal(expected, s.ShouldCaptureStack(level, globalCapture: true));
    }
}
