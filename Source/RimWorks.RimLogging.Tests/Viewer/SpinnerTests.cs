using RimWorks.RimLogging.Viewer;
using Xunit;

namespace RimWorks.RimLogging.Tests.Viewer;

public class SpinnerTests
{
    [Fact]
    public void Frame_AdvancesTenTimesASecond()
    {
        Assert.NotEqual(Spinner.Frame(1.00f), Spinner.Frame(1.10f));
    }

    [Fact]
    public void Frame_CyclesBackAfterFourSteps()
    {
        Assert.Equal(Spinner.Frame(1.0f), Spinner.Frame(1.4f));
    }

    [Fact]
    public void Frame_HoldsStillWithinOneStep()
    {
        Assert.Equal(Spinner.Frame(2.00f), Spinner.Frame(2.09f));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-5f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void Frame_DegenerateTimes_ReturnTheFirstGlyph(float seconds)
    {
        Assert.Equal('|', Spinner.Frame(seconds));
    }

    [Fact]
    public void Frame_HugeUptime_StaysInRange()
    {
        // a long session must not overflow the cast and index past the glyph array
        Assert.Contains(Spinner.Frame(9_000_000f), "|/-\\");
    }
}
