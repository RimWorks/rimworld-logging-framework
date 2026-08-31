using RimWorks.RimLogging.Pipeline;
using Xunit;

namespace RimWorks.RimLogging.Tests.Pipeline;

public class ReentryGuardTests
{
    [Fact]
    public void IsInsideSink_OutsideAnyScope_ReturnsFalse()
        => Assert.False(ReentryGuard.IsInsideSink);

    [Fact]
    public void Enter_FlipsInsideThenDisposeFlipsBack()
    {
        using (ReentryGuard.Scope scope = ReentryGuard.Enter())
        {
            Assert.True(ReentryGuard.IsInsideSink);
        }

        Assert.False(ReentryGuard.IsInsideSink);
    }

    [Fact]
    public void NestedEnter_InnerDispose_LeavesFlagTrue()
    {
        using (ReentryGuard.Scope outer = ReentryGuard.Enter())
        {
            using (ReentryGuard.Scope inner = ReentryGuard.Enter())
            {
                Assert.True(ReentryGuard.IsInsideSink);
            }

            Assert.True(ReentryGuard.IsInsideSink);
        }

        Assert.False(ReentryGuard.IsInsideSink);
    }
}
