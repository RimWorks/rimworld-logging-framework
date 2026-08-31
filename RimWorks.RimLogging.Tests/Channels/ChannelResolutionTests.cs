using RimWorks.RimLogging.Channels;
using Xunit;

namespace RimWorks.RimLogging.Tests.Channels;

public class ChannelResolutionTests
{
    [Fact]
    public void ResolveOwnerKey_ExactMatch_ReturnsExactKey()
    {
        string[] keys = ["Cosmere.Roshar", "default"];

        string? result = ChannelResolution.ResolveOwnerKey("Cosmere.Roshar", keys);

        Assert.Equal("Cosmere.Roshar", result);
    }

    [Fact]
    public void ResolveOwnerKey_SingleLevelPrefixWalk_ReturnsAncestorKey()
    {
        string[] keys = ["Cosmere.Roshar", "default"];

        string? result = ChannelResolution.ResolveOwnerKey("Cosmere.Roshar.Surgebinding", keys);

        Assert.Equal("Cosmere.Roshar", result);
    }

    [Fact]
    public void ResolveOwnerKey_MultiLevelPrefixWalk_ReturnsAncestorKey()
    {
        string[] keys = ["Cosmere", "default"];

        string? result = ChannelResolution.ResolveOwnerKey("Cosmere.Roshar.Surgebinding", keys);

        Assert.Equal("Cosmere", result);
    }

    [Fact]
    public void ResolveOwnerKey_NoMatchWithDefault_ReturnsDefault()
    {
        string[] keys = ["Cosmere.Scadrial", "default"];

        string? result = ChannelResolution.ResolveOwnerKey("Cosmere.Roshar.Surgebinding", keys);

        Assert.Equal("default", result);
    }

    [Fact]
    public void ResolveOwnerKey_NoMatchAndNoDefault_ReturnsNull()
    {
        string[] keys = ["Cosmere.Scadrial"];

        string? result = ChannelResolution.ResolveOwnerKey("Cosmere.Roshar.Surgebinding", keys);

        Assert.Null(result);
    }

    [Fact]
    public void ResolveOwnerKey_EmptyChannelName_ReturnsNull()
    {
        string[] keys = ["default", "Cosmere"];

        string? result = ChannelResolution.ResolveOwnerKey("", keys);

        Assert.Null(result);
    }

    [Fact]
    public void ResolveOwnerKey_SingleSegmentUnknownChannel_FallsBackToDefault()
    {
        string[] keys = ["default", "Cosmere"];

        string? result = ChannelResolution.ResolveOwnerKey("Stormlight", keys);

        Assert.Equal("default", result);
    }
}
