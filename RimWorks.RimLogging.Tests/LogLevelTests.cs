using RimWorks.RimLogging;
using Xunit;

namespace RimWorks.RimLogging.Tests;

public class LogLevelTests
{
    [Fact]
    public void Ordering_IsAscendingBySeverity()
    {
        Assert.True(LogLevel.Trace < LogLevel.Debug);
        Assert.True(LogLevel.Debug < LogLevel.Info);
        Assert.True(LogLevel.Info < LogLevel.Warn);
        Assert.True(LogLevel.Warn < LogLevel.Error);
        Assert.True(LogLevel.Error < LogLevel.Fatal);
    }

    [Theory]
    [InlineData(LogLevel.Trace, "TRACE")]
    [InlineData(LogLevel.Debug, "DEBUG")]
    [InlineData(LogLevel.Info, "INFO")]
    [InlineData(LogLevel.Warn, "WARN")]
    [InlineData(LogLevel.Error, "ERROR")]
    [InlineData(LogLevel.Fatal, "FATAL")]
    public void ToString_ReturnsUppercaseName(LogLevel level, string expected)
    {
        Assert.Equal(expected, level.ToString().ToUpperInvariant());
    }
}
