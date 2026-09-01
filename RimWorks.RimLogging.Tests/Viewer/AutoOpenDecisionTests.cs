using RimWorks.RimLogging.Viewer;
using Xunit;

namespace RimWorks.RimLogging.Tests.Viewer;

public class AutoOpenDecisionTests
{
    [Fact]
    public void ShouldTakeOver_VanillaWantsToOpenAndViewerIsReady_TakesOver()
    {
        Assert.True(AutoOpenDecision.ShouldTakeOver(wantsToOpen: true, hasSink: true, hasWindowStack: true));
    }

    [Fact]
    public void ShouldTakeOver_VanillaIsNotAskingToOpen_LeavesVanillaAlone()
    {
        Assert.False(AutoOpenDecision.ShouldTakeOver(wantsToOpen: false, hasSink: true, hasWindowStack: true));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void ShouldTakeOver_ViewerNotBootedYet_FallsBackToVanilla(bool hasSink, bool hasWindowStack)
    {
        Assert.False(AutoOpenDecision.ShouldTakeOver(wantsToOpen: true, hasSink, hasWindowStack));
    }
}
