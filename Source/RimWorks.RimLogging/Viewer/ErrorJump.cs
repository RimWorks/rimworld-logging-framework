using System.Collections.Generic;

namespace RimWorks.RimLogging.Viewer;

/// <summary>Finds the next problem row and where to scroll for it. Pure so it tests without Unity.</summary>
internal static class ErrorJump
{
    /// <summary>
    /// Returns the next index after <paramref name="fromIndex"/> whose level is at or above
    /// <paramref name="minLevel"/>, wrapping to the top so repeated presses cycle. Returns -1
    /// when the list holds nothing that severe.
    /// </summary>
    internal static int Next(IReadOnlyList<LogEntry> entries, int fromIndex, LogLevel minLevel)
    {
        if (entries == null || entries.Count == 0) return -1;

        for (int i = fromIndex + 1; i < entries.Count; i++)
        {
            if (entries[i].Level >= minLevel) return i;
        }
        // wrap, including fromIndex itself so a lone error is still reachable
        for (int i = 0; i <= fromIndex && i < entries.Count; i++)
        {
            if (entries[i].Level >= minLevel) return i;
        }
        return -1;
    }

    /// <summary>Returns the scroll offset that puts a row in view, roughly a third down the pane.</summary>
    internal static float ScrollTo(int index, float rowHeight, float viewportHeight, int count)
    {
        if (index < 0 || rowHeight <= 0f) return 0f;

        float target = index * rowHeight - viewportHeight / 3f;
        float max = count * rowHeight - viewportHeight;
        if (max < 0f) max = 0f;
        if (target < 0f) target = 0f;
        if (target > max) target = max;
        return target;
    }
}
