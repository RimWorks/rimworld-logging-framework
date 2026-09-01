using System;

namespace RimWorks.RimLogging.Viewer;

/// <summary>Scroll math for the tailing log list. Pure so it tests without Unity.</summary>
internal static class TailScroll {
    // a row is 22px, so half a row of slack still counts as parked at the bottom
    private const float Slack = 12f;

    internal static float MaxScroll(float viewportHeight, float contentHeight) {
        return Math.Max(0f, contentHeight - viewportHeight);
    }

    internal static bool IsAtBottom(float scrollY, float viewportHeight, float contentHeight) {
        return scrollY >= MaxScroll(viewportHeight, contentHeight) - Slack;
    }
}
