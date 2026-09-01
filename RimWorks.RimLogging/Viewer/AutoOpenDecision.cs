namespace RimWorks.RimLogging.Viewer;

/// <summary>Decides whether the viewer takes over vanilla's auto-open. Split from the patch so it tests without Verse.</summary>
internal static class AutoOpenDecision {
    internal static bool ShouldTakeOver(bool wantsToOpen, bool hasSink, bool hasWindowStack) {
        return wantsToOpen && hasSink && hasWindowStack;
    }
}
