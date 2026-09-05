using System.Collections.Generic;
using RimWorks.RimLogging.Sinks;
using Xunit;

namespace RimWorks.RimLogging.Tests.Sinks;

public class SinkTogglesTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsEnabled_NoOverride_UsesTheDefDefault(bool byDefault)
    {
        Assert.Equal(byDefault, SinkToggles.IsEnabled("RollingJson", byDefault, new List<string>(), new List<bool>()));
    }

    [Fact]
    public void IsEnabled_OverrideOn_BeatsADefaultOfOff()
    {
        List<string> names = new List<string>();
        List<bool> states = new List<bool>();
        SinkToggles.Set("RollingJson", true, names, states);

        Assert.True(SinkToggles.IsEnabled("RollingJson", enabledByDefault: false, names, states));
    }

    [Fact]
    public void IsEnabled_OverrideOff_BeatsADefaultOfOn()
    {
        List<string> names = new List<string>();
        List<bool> states = new List<bool>();
        SinkToggles.Set("RollingText", false, names, states);

        Assert.False(SinkToggles.IsEnabled("RollingText", enabledByDefault: true, names, states));
    }

    [Fact]
    public void IsEnabled_ASinkNobodyTouched_IsUnaffectedByOtherOverrides()
    {
        // a sink added by a later version, or by another mod, must keep its own default
        List<string> names = new List<string>();
        List<bool> states = new List<bool>();
        SinkToggles.Set("RollingJson", true, names, states);

        Assert.True(SinkToggles.IsEnabled("SomeNewSink", enabledByDefault: true, names, states));
        Assert.False(SinkToggles.IsEnabled("SomeOtherSink", enabledByDefault: false, names, states));
    }

    [Fact]
    public void Set_Twice_ReplacesRatherThanAppending()
    {
        List<string> names = new List<string>();
        List<bool> states = new List<bool>();
        SinkToggles.Set("RollingJson", true, names, states);
        SinkToggles.Set("RollingJson", false, names, states);

        Assert.Single(names);
        Assert.False(SinkToggles.IsEnabled("RollingJson", enabledByDefault: true, names, states));
    }

    [Fact]
    public void Clear_FallsBackToTheDefDefault()
    {
        List<string> names = new List<string>();
        List<bool> states = new List<bool>();
        SinkToggles.Set("RollingJson", true, names, states);

        SinkToggles.Clear("RollingJson", names, states);

        Assert.Empty(names);
        Assert.Empty(states);
        Assert.False(SinkToggles.IsEnabled("RollingJson", enabledByDefault: false, names, states));
    }

    [Fact]
    public void IsEnabled_NullLists_UseTheDefault()
    {
        Assert.True(SinkToggles.IsEnabled("RollingJson", enabledByDefault: true, null, null));
    }

    [Fact]
    public void IsEnabled_ListsOfDifferentLengths_IgnoreTheUnpairedTail()
    {
        // Scribe writes the two lists separately, so a torn save can leave them mismatched
        List<string> names = new List<string> { "RollingJson", "RollingText" };
        List<bool> states = new List<bool> { true };

        Assert.True(SinkToggles.IsEnabled("RollingJson", enabledByDefault: false, names, states));
        Assert.True(SinkToggles.IsEnabled("RollingText", enabledByDefault: true, names, states));
    }
}
