using RimWorks.RimLogging.Capture;
using Xunit;

namespace RimWorks.RimLogging.Tests.Capture;

public class VerseLevelMappingTests
{
    [Fact]
    public void FromVerseMessageTypeId_0_ReturnsInfo()
    {
        Assert.Equal(LogLevel.Info, VerseLevelMapping.FromVerseMessageTypeId(0));
    }

    [Fact]
    public void FromVerseMessageTypeId_1_ReturnsWarn()
    {
        Assert.Equal(LogLevel.Warn, VerseLevelMapping.FromVerseMessageTypeId(1));
    }

    [Fact]
    public void FromVerseMessageTypeId_2_ReturnsError()
    {
        Assert.Equal(LogLevel.Error, VerseLevelMapping.FromVerseMessageTypeId(2));
    }

    [Fact]
    public void FromVerseMessageTypeId_Unknown_ReturnsInfo()
    {
        Assert.Equal(LogLevel.Info, VerseLevelMapping.FromVerseMessageTypeId(99));
    }
}
