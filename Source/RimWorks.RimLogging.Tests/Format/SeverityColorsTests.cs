using RimWorks.RimLogging;
using RimWorks.RimLogging.Format;
using Xunit;

namespace RimWorks.RimLogging.Tests.Format;

public class SeverityColorsTests
{
    [Theory]
    [InlineData(LogLevel.Trace, "808080")]
    [InlineData(LogLevel.Debug, "8AA9C8")]
    [InlineData(LogLevel.Info,  "A5C2A5")]
    [InlineData(LogLevel.Warn,  "D8C36C")]
    [InlineData(LogLevel.Error, "C97373")]
    [InlineData(LogLevel.Fatal, "9F4FBF")]
    public void GetHex_KnownLevel_ReturnsDocumentedHex(LogLevel level, string expectedHex)
    {
        Assert.Equal(expectedHex, SeverityColors.GetHex(level));
    }

    [Fact]
    public void GetHex_Fatal_DoesNotCollideWithError()
    {
        string fatal = SeverityColors.GetHex(LogLevel.Fatal);
        string error = SeverityColors.GetHex(LogLevel.Error);

        Assert.Equal("9F4FBF", fatal);
        Assert.NotEqual(error, fatal);
    }

    [Fact]
    public void GetHex_UnknownLevel_ReturnsFallback()
    {
        string result = SeverityColors.GetHex((LogLevel)99);

        Assert.Equal("C7C7C7", result);
    }
}
