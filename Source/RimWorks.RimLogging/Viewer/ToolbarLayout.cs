namespace RimWorks.RimLogging.Viewer;

/// <summary>The row-one width and filter-group width the toolbar drew, and where the filter
/// field landed as a result.</summary>
internal readonly struct ToolbarPlan
{
    public readonly bool TwoRows;
    public readonly bool CompactLevels;
    public readonly float FilterFieldWidth;

    public ToolbarPlan(bool twoRows, bool compactLevels, float filterFieldWidth)
    {
        TwoRows = twoRows;
        CompactLevels = compactLevels;
        FilterFieldWidth = filterFieldWidth;
    }
}

/// <summary>Decides how the toolbar degrades as the window narrows, from real measured widths
/// rather than hardcoded thresholds, so it stays correct under a translation.</summary>
internal static class ToolbarLayout
{
    /// <summary>Below this the filter field is unreadable no matter how much room it is given.</summary>
    internal const float FilterFloor = 260f;

    // rowOneFixedWidth excludes the filter field and Presets; filterGroupWidth is Presets alone.
    public static ToolbarPlan Compute(float availableWidth, float rowOneFixedWidth, float filterGroupWidth)
    {
        float oneRowRequirement = rowOneFixedWidth + FilterFloor + filterGroupWidth;
        if (availableWidth >= oneRowRequirement)
        {
            float filterWidth = availableWidth - rowOneFixedWidth - filterGroupWidth;
            return new ToolbarPlan(false, false, filterWidth);
        }

        bool compact = availableWidth < rowOneFixedWidth;
        float rowTwoFilterWidth = availableWidth - filterGroupWidth;
        if (rowTwoFilterWidth < FilterFloor)
        {
            rowTwoFilterWidth = FilterFloor;
        }
        return new ToolbarPlan(true, compact, rowTwoFilterWidth);
    }
}
