using RimWorks.RimLogging.Viewer;
using Xunit;

namespace RimWorks.RimLogging.Tests.Viewer;

public class RowWindowTests
{
    // 22px rows, a 440px viewport: 20 rows fit
    private const float RowHeight = 22f;
    private const float Viewport = 440f;

    [Fact]
    public void Visible_ScrolledToTop_StartsAtTheFirstRow()
    {
        (int first, int last) = RowWindow.Visible(0f, Viewport, RowHeight, 5000);

        Assert.Equal(0, first);
        Assert.InRange(last, 20, 24);
    }

    [Fact]
    public void Visible_HugeList_DrawsOnlyAScreenful()
    {
        (int first, int last) = RowWindow.Visible(0f, Viewport, RowHeight, 100000);

        Assert.True(last - first < 30, $"expected roughly a screenful, got {last - first}");
    }

    [Fact]
    public void Visible_ScrolledDown_SkipsTheRowsAbove()
    {
        (int first, int last) = RowWindow.Visible(2200f, Viewport, RowHeight, 5000);

        Assert.Equal(100, first);
        Assert.InRange(last, 120, 124);
    }

    [Fact]
    public void Visible_ScrolledToTheEnd_ClampsToTheRowCount()
    {
        (int first, int last) = RowWindow.Visible(2200f, Viewport, RowHeight, 105);

        Assert.Equal(100, first);
        Assert.Equal(105, last);
    }

    [Fact]
    public void Visible_ListShorterThanViewport_DrawsEveryRow()
    {
        (int first, int last) = RowWindow.Visible(0f, Viewport, RowHeight, 3);

        Assert.Equal(0, first);
        Assert.Equal(3, last);
    }

    [Fact]
    public void Visible_EmptyList_DrawsNothing()
    {
        Assert.Equal((0, 0), RowWindow.Visible(0f, Viewport, RowHeight, 0));
    }

    [Fact]
    public void Visible_PartlyScrolledRow_StillIncludesIt()
    {
        // half a row down, so row 0 is still partly on screen
        (int first, _) = RowWindow.Visible(11f, Viewport, RowHeight, 5000);

        Assert.Equal(0, first);
    }
}
