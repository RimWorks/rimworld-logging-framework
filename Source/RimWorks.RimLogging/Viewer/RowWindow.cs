namespace RimWorks.RimLogging.Viewer;

/// <summary>Picks the row range a scroll view actually shows. Pure so it tests without Unity.</summary>
internal static class RowWindow
{
    /// <summary>Returns the half-open range of row indices to draw, clamped to the row count.</summary>
    internal static (int First, int Last) Visible(float scrollY, float viewportHeight, float rowHeight, int count)
    {
        if (count <= 0 || rowHeight <= 0f)
        {
            return (0, 0);
        }

        int first = scrollY <= 0f ? 0 : (int)(scrollY / rowHeight);
        if (first > count)
        {
            first = count;
        }

        // one extra row so a partly scrolled row still draws
        int last = (int)((scrollY + viewportHeight) / rowHeight) + 2;
        if (last > count)
        {
            last = count;
        }
        if (last < first)
        {
            last = first;
        }

        return (first, last);
    }
}
