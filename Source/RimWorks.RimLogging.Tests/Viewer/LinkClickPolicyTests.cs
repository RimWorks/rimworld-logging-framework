using RimWorks.RimLogging.Viewer;
using Xunit;

namespace RimWorks.RimLogging.Tests.Viewer;

/// <summary>
/// Guards the link row. The url is a selectable text area, so a copy may only fire on a press
/// and release that stayed in one place; dragging across the url has to select it instead.
/// </summary>
public class LinkClickPolicyTests
{
    [Fact]
    public void PointerNeverMoved_IsAClick()
    {
        Assert.True(LinkClickPolicy.IsClick(0f, 0f));
    }

    [Fact]
    public void ASmallDriftWhilePressing_StillCountsAsAClick()
    {
        // a mouse rocks a pixel or two under a real finger, and that must still copy
        Assert.True(LinkClickPolicy.IsClick(1f, 1f));
        Assert.True(LinkClickPolicy.IsClick(-2f, 0f));
    }

    [Fact]
    public void DraggingAcrossTheUrl_IsNotAClick()
    {
        Assert.False(LinkClickPolicy.IsClick(40f, 0f));
        Assert.False(LinkClickPolicy.IsClick(-40f, 6f));
    }

    [Fact]
    public void TravelIsMeasuredOnBothAxesTogether()
    {
        // MaxTravel is a radius, so a straight drop down counts the same as a drag sideways
        Assert.True(LinkClickPolicy.IsClick(0f, LinkClickPolicy.MaxTravel));
        Assert.True(LinkClickPolicy.IsClick(-LinkClickPolicy.MaxTravel, 0f));
        Assert.False(LinkClickPolicy.IsClick(LinkClickPolicy.MaxTravel, LinkClickPolicy.MaxTravel));
    }

    [Fact]
    public void JustPastTheEdge_IsADrag()
    {
        Assert.False(LinkClickPolicy.IsClick(LinkClickPolicy.MaxTravel + 0.1f, 0f));
    }
}
