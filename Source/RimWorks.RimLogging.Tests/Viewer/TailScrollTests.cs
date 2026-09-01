using RimWorks.RimLogging.Viewer;
using Xunit;

namespace RimWorks.RimLogging.Tests.Viewer;

public class TailScrollTests
{
    [Fact]
    public void MaxScroll_ContentShorterThanViewport_DoesNotGoNegative()
    {
        Assert.Equal(0f, TailScroll.MaxScroll(viewportHeight: 400f, contentHeight: 120f));
    }

    [Fact]
    public void MaxScroll_ContentTallerThanViewport_IsTheOverflow()
    {
        Assert.Equal(600f, TailScroll.MaxScroll(viewportHeight: 400f, contentHeight: 1000f));
    }

    [Fact]
    public void IsAtBottom_ParkedAtTheEnd_StaysTailing()
    {
        Assert.True(TailScroll.IsAtBottom(scrollY: 600f, viewportHeight: 400f, contentHeight: 1000f));
    }

    [Fact]
    public void IsAtBottom_WithinASingleRowOfTheEnd_StillCountsAsTailing()
    {
        Assert.True(TailScroll.IsAtBottom(scrollY: 592f, viewportHeight: 400f, contentHeight: 1000f));
    }

    [Fact]
    public void IsAtBottom_UserScrolledUp_StopsTailing()
    {
        Assert.False(TailScroll.IsAtBottom(scrollY: 200f, viewportHeight: 400f, contentHeight: 1000f));
    }

    [Fact]
    public void IsAtBottom_ListShorterThanViewport_IsAlwaysTailing()
    {
        Assert.True(TailScroll.IsAtBottom(scrollY: 0f, viewportHeight: 400f, contentHeight: 80f));
    }
}
