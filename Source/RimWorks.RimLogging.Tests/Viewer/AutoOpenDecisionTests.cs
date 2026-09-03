using RimWorks.RimLogging.Viewer;
using Xunit;

namespace RimWorks.RimLogging.Tests.Viewer;

public class AutoOpenDecisionTests
{
    [Fact]
    public void ShouldTakeOver_VanillaWantsToOpenAndViewerIsReady_TakesOver()
    {
        Assert.True(AutoOpenDecision.ShouldTakeOver(
            wantsToOpen: true, hasSink: true, hasWindowStack: true, viewerBroken: false));
    }

    [Fact]
    public void ShouldTakeOver_VanillaIsNotAskingToOpen_LeavesVanillaAlone()
    {
        Assert.False(AutoOpenDecision.ShouldTakeOver(
            wantsToOpen: false, hasSink: true, hasWindowStack: true, viewerBroken: false));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void ShouldTakeOver_ViewerNotBootedYet_FallsBackToVanilla(bool hasSink, bool hasWindowStack)
    {
        Assert.False(AutoOpenDecision.ShouldTakeOver(
            wantsToOpen: true, hasSink, hasWindowStack, viewerBroken: false));
    }

    // a failed open is logged, and that log re-arms wantsToOpen, so an unlatched retry runs
    // every frame forever
    [Fact]
    public void ShouldTakeOver_OpenAlreadyThrewOnce_StopsRetryingForever()
    {
        Assert.False(AutoOpenDecision.ShouldTakeOver(
            wantsToOpen: true, hasSink: true, hasWindowStack: true, viewerBroken: true));
    }

    [Fact]
    public void ShouldReclaim_VanillaWonTheBootRace_TakesTheWindowBack()
    {
        Assert.True(AutoOpenDecision.ShouldReclaim(
            reclaimPending: true, hasSink: true, vanillaOpen: true, viewerBroken: false));
    }

    [Fact]
    public void ShouldReclaim_UserOpenedVanillaThemselves_LeavesItAlone()
    {
        Assert.False(AutoOpenDecision.ShouldReclaim(
            reclaimPending: false, hasSink: true, vanillaOpen: true, viewerBroken: false));
    }

    [Fact]
    public void ShouldReclaim_NoVanillaWindowOnScreen_DoesNothing()
    {
        Assert.False(AutoOpenDecision.ShouldReclaim(
            reclaimPending: true, hasSink: true, vanillaOpen: false, viewerBroken: false));
    }

    [Fact]
    public void ShouldReclaim_SinkNotRegistered_DoesNothing()
    {
        Assert.False(AutoOpenDecision.ShouldReclaim(
            reclaimPending: true, hasSink: false, vanillaOpen: true, viewerBroken: false));
    }

    [Fact]
    public void ShouldReclaim_OpenAlreadyThrewOnce_LeavesVanillasWindowUp()
    {
        Assert.False(AutoOpenDecision.ShouldReclaim(
            reclaimPending: true, hasSink: true, vanillaOpen: true, viewerBroken: true));
    }
}
