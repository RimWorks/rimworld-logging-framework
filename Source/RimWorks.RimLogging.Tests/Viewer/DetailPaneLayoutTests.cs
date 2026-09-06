using RimWorks.RimLogging.Viewer;
using Xunit;

namespace RimWorks.RimLogging.Tests.Viewer;

/// <summary>
/// Guards the Copy All button. Rows became read-only text areas, and a text area eats the
/// mouse down inside its rect, so any row overlapping the button stops the button working.
/// </summary>
public class DetailPaneLayoutTests
{
    [Theory]
    [InlineData(0f)]
    [InlineData(37.5f)]
    [InlineData(600f)]
    public void BodyStartsBelowTheButton_SoNoRowCanEatTheClick(float innerTop)
    {
        Assert.False(DetailPaneLayout.Overlaps(innerTop, DetailPaneLayout.BodyTop(innerTop)));
    }

    [Fact]
    public void TheOldLayoutOverlapped_WhichIsTheBugThisGuards()
    {
        // the body used to start at the pane's inner top, exactly where the button is drawn
        Assert.True(DetailPaneLayout.Overlaps(100f, 100f));
    }

    [Fact]
    public void AGapSeparatesTheButtonFromTheFirstRow()
    {
        Assert.Equal(DetailPaneLayout.Gap,
            DetailPaneLayout.BodyTop(0f) - DetailPaneLayout.ButtonHeight);
    }

    [Fact]
    public void BodyGivesBackWhatIsLeftOfThePane()
    {
        Assert.Equal(200f - DetailPaneLayout.HeaderHeight, DetailPaneLayout.BodyHeight(200f));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(10f)]
    [InlineData(DetailPaneLayout.HeaderHeight)]
    public void APaneTooShortForTheStrip_DoesNotProduceANegativeHeight(float innerHeight)
    {
        // a negative height rect makes the scroll view misbehave rather than just look wrong
        Assert.Equal(0f, DetailPaneLayout.BodyHeight(innerHeight));
    }
}
