using RimWorks.RimLogging.Viewer;
using Xunit;

namespace RimWorks.RimLogging.Tests.Viewer;

public class ToolbarLayoutTests
{
    private const float RowOneFixed = 500f;
    private const float FilterGroup = 28f;

    [Fact]
    public void Compute_WideWindow_StaysOneRow()
    {
        ToolbarPlan plan = ToolbarLayout.Compute(900f, RowOneFixed, FilterGroup);

        Assert.False(plan.TwoRows);
        Assert.False(plan.CompactLevels);
        Assert.Equal(372f, plan.FilterFieldWidth);
    }

    [Fact]
    public void Compute_ExactlyAtTheOneRowRequirement_StaysOneRow()
    {
        // requirement is rowOneFixed + FilterFloor + filterGroup = 500 + 260 + 28 = 788
        ToolbarPlan plan = ToolbarLayout.Compute(788f, RowOneFixed, FilterGroup);

        Assert.False(plan.TwoRows);
        Assert.Equal(ToolbarLayout.FilterFloor, plan.FilterFieldWidth);
    }

    [Fact]
    public void Compute_OnePixelBelowTheOneRowRequirement_DropsToTwoRows()
    {
        ToolbarPlan plan = ToolbarLayout.Compute(787f, RowOneFixed, FilterGroup);

        Assert.True(plan.TwoRows);
        Assert.False(plan.CompactLevels);
    }

    [Fact]
    public void Compute_TwoRows_RowOneStaysUncompactedUntilItStopsFittingAlone()
    {
        ToolbarPlan plan = ToolbarLayout.Compute(RowOneFixed, RowOneFixed, FilterGroup);

        Assert.True(plan.TwoRows);
        Assert.False(plan.CompactLevels);
    }

    [Fact]
    public void Compute_NarrowerThanRowOneItself_CompactsTheLevelPills()
    {
        ToolbarPlan plan = ToolbarLayout.Compute(RowOneFixed - 1f, RowOneFixed, FilterGroup);

        Assert.True(plan.TwoRows);
        Assert.True(plan.CompactLevels);
    }

    [Fact]
    public void Compute_TwoRows_FilterFieldFillsRowTwoRatherThanStayingAtTheFloor()
    {
        ToolbarPlan plan = ToolbarLayout.Compute(787f, RowOneFixed, FilterGroup);

        Assert.Equal(787f - FilterGroup, plan.FilterFieldWidth);
    }

    [Fact]
    public void Compute_RowTwoNarrowerThanTheFloor_ClampsToTheFloor()
    {
        ToolbarPlan plan = ToolbarLayout.Compute(200f, RowOneFixed, FilterGroup);

        Assert.True(plan.CompactLevels);
        Assert.Equal(ToolbarLayout.FilterFloor, plan.FilterFieldWidth);
    }
}
