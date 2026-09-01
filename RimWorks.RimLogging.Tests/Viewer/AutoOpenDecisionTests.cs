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

    [Fact]
    public void ShouldReclaim_VanillaWonTheBootRace_TakesTheWindowBack()
    {
        Assert.True(AutoOpenDecision.ShouldReclaim(reclaimPending: true, hasSink: true, vanillaOpen: true));
    }

    [Fact]
    public void ShouldReclaim_UserOpenedVanillaThemselves_LeavesItAlone()
    {
        Assert.False(AutoOpenDecision.ShouldReclaim(reclaimPending: false, hasSink: true, vanillaOpen: true));
    }

    [Fact]
    public void ShouldReclaim_NoVanillaWindowOnScreen_DoesNothing()
    {
        Assert.False(AutoOpenDecision.ShouldReclaim(reclaimPending: true, hasSink: true, vanillaOpen: false));
    }

    [Fact]
    public void ShouldReclaim_SinkNotRegistered_DoesNothing()
    {
        Assert.False(AutoOpenDecision.ShouldReclaim(reclaimPending: true, hasSink: false, vanillaOpen: true));
    }
}
