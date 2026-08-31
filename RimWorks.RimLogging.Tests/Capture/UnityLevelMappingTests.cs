using RimWorks.RimLogging.Capture;
using Xunit;

namespace RimWorks.RimLogging.Tests.Capture;

public class UnityLevelMappingTests
{
    [Fact]
    public void FromUnityLogTypeId_0_ReturnsError()
    {
        Assert.Equal(LogLevel.Error, UnityLevelMapping.FromUnityLogTypeId(0));
    }

    [Fact]
    public void FromUnityLogTypeId_1_ReturnsError()
    {
        Assert.Equal(LogLevel.Error, UnityLevelMapping.FromUnityLogTypeId(1));
    }

    [Fact]
    public void FromUnityLogTypeId_2_ReturnsWarn()
    {
        Assert.Equal(LogLevel.Warn, UnityLevelMapping.FromUnityLogTypeId(2));
    }

    [Fact]
    public void FromUnityLogTypeId_3_ReturnsInfo()
    {
        Assert.Equal(LogLevel.Info, UnityLevelMapping.FromUnityLogTypeId(3));
    }

    [Fact]
    public void FromUnityLogTypeId_4_ReturnsFatal()
    {
        Assert.Equal(LogLevel.Fatal, UnityLevelMapping.FromUnityLogTypeId(4));
    }

    [Fact]
    public void FromUnityLogTypeId_Unknown_ReturnsInfo()
    {
        Assert.Equal(LogLevel.Info, UnityLevelMapping.FromUnityLogTypeId(99));
    }
}
