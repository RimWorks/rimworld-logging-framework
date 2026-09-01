using RimWorks.RimLogging.Pipeline;
using Xunit;

namespace RimWorks.RimLogging.Tests.Pipeline;

public class SynchronousBypassTests
{
    [Fact]
    public void ShouldBypass_Trace_ReturnsFalse()
        => Assert.False(SynchronousBypass.ShouldBypass(LogLevel.Trace));

    [Fact]
    public void ShouldBypass_Debug_ReturnsFalse()
        => Assert.False(SynchronousBypass.ShouldBypass(LogLevel.Debug));

    [Fact]
    public void ShouldBypass_Info_ReturnsFalse()
        => Assert.False(SynchronousBypass.ShouldBypass(LogLevel.Info));

    [Fact]
    public void ShouldBypass_Warn_ReturnsFalse()
        => Assert.False(SynchronousBypass.ShouldBypass(LogLevel.Warn));

    [Fact]
    public void ShouldBypass_Error_ReturnsTrue()
        => Assert.True(SynchronousBypass.ShouldBypass(LogLevel.Error));

    [Fact]
    public void ShouldBypass_Fatal_ReturnsTrue()
        => Assert.True(SynchronousBypass.ShouldBypass(LogLevel.Fatal));
}
