using System.Collections.Generic;
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
        ChannelSettings s = new ChannelSettings(LogLevel.Warn, null, null, null);

        Assert.Equal(LogLevel.Warn, s.MinLevelOr(LogLevel.Debug));
    }

    [Fact]
    public void MinLevelOr_OverrideBelowGlobal_IsStillHonoured()
    {
        // a channel may ask for more than the global, the emit path applies the global gate first
        ChannelSettings s = new ChannelSettings(LogLevel.Trace, null, null, null);

        Assert.Equal(LogLevel.Trace, s.MinLevelOr(LogLevel.Error));
    }

    [Fact]
    public void ShouldCaptureStack_GlobalOff_NeverCaptures()
    {
        ChannelSettings s = new ChannelSettings(null, LogLevel.Trace, null, null);

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
        ChannelSettings s = new ChannelSettings(null, LogLevel.Error, null, null);

        Assert.Equal(expected, s.ShouldCaptureStack(level, globalCapture: true));
    }

    [Fact]
    public void AllowsSink_NoDestinations_AllowsEverySink()
    {
        Assert.True(ChannelSettings.Inherit.AllowsSink("anything"));
        Assert.False(ChannelSettings.Inherit.HasDestinations);
    }

    [Fact]
    public void AllowsSink_EmptyList_CountsAsNoRestriction()
    {
        // an unset <destinations></destinations> must not mute the channel
        ChannelSettings s = new ChannelSettings(null, null, new List<string>(), null);

        Assert.False(s.HasDestinations);
        Assert.True(s.AllowsSink("VerseLog"));
    }

    [Fact]
    public void AllowsSink_OnlyTheNamedSinksPass()
    {
        ChannelSettings s = new ChannelSettings(null, null, new List<string> { "Memory" }, null);

        Assert.True(s.HasDestinations);
        Assert.True(s.AllowsSink("Memory"));
        Assert.False(s.AllowsSink("VerseLog"));
    }

    [Fact]
    public void AllowsSink_IsCaseInsensitive()
    {
        // these are hand-typed in xml
        ChannelSettings s = new ChannelSettings(null, null, new List<string> { "memory" }, null);

        Assert.True(s.AllowsSink("Memory"));
    }

    [Fact]
    public void TemplateOr_NoOverride_UsesTheSinkTemplate()
    {
        Assert.Equal("[{level}] {msg}", ChannelSettings.Inherit.TemplateOr("[{level}] {msg}"));
    }

    [Fact]
    public void TemplateOr_Override_WinsOverTheSinkTemplate()
    {
        ChannelSettings s = new ChannelSettings(null, null, null, "{msg}");

        Assert.Equal("{msg}", s.TemplateOr("[{level}] {msg}"));
    }

    [Fact]
    public void TemplateOr_EmptyOverride_FallsBackToTheSink()
    {
        ChannelSettings s = new ChannelSettings(null, null, null, "");

        Assert.Equal("[{level}] {msg}", s.TemplateOr("[{level}] {msg}"));
    }
}
