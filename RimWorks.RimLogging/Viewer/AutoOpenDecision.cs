namespace RimWorks.RimLogging.Viewer;

/// <summary>Decides whether the viewer takes over vanilla's auto-open. Pure so it tests without Verse.</summary>
internal static class AutoOpenDecision {
    internal static bool ShouldTakeOver(bool wantsToOpen, bool hasSink, bool hasWindowStack) {
        return wantsToOpen && hasSink && hasWindowStack;
    }

    // vanilla can win the race when a mod errors before our sink registers, which leaves its
    // window on screen with wantsToOpen already cleared
    internal static bool ShouldReclaim(bool reclaimPending, bool hasSink, bool vanillaOpen) {
        return reclaimPending && hasSink && vanillaOpen;
    }
}
