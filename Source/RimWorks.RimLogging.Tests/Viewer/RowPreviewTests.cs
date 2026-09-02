using RimWorks.RimLogging.Viewer;
using Xunit;

namespace RimWorks.RimLogging.Tests.Viewer;

public class RowPreviewTests
{
    [Fact]
    public void SingleLine_IsReturnedWholeWithNoExtra()
    {
        Assert.Equal(("all fine here", 0), EntryText.SplitRow("all fine here"));
    }

    [Fact]
    public void MultiLine_KeepsTheFirstLineAndCountsTheRest()
    {
        string message = "Initializing new game with mods:\n- concordlib.concord\n- brrainz.harmony";

        Assert.Equal(("Initializing new game with mods:", 2), EntryText.SplitRow(message));
    }

    [Fact]
    public void MultiLine_NeverSurfacesAMiddleLine()
    {
        // the row is one line tall, so a centred multi-line block used to show line 7 of 16
        (string head, _) = EntryText.SplitRow("head\nmiddle one\nmiddle two\ntail");

        Assert.Equal("head", head);
    }

    [Fact]
    public void TrailingNewline_DoesNotCountAsAnotherLine()
    {
        Assert.Equal(("head", 0), EntryText.SplitRow("head\n"));
    }

    [Fact]
    public void CarriageReturns_AreNotLeftOnTheHead()
    {
        Assert.Equal(("head", 1), EntryText.SplitRow("head\r\ntail"));
    }

    [Fact]
    public void Empty_IsEmpty()
    {
        Assert.Equal((string.Empty, 0), EntryText.SplitRow(""));
    }

    [Fact]
    public void ExtraLinesLabel_ReadsAsSingularForOne()
    {
        Assert.Equal("(+1 line)", EntryText.ExtraLinesLabel(1));
    }

    [Fact]
    public void ExtraLinesLabel_ReadsAsPluralBeyondOne()
    {
        Assert.Equal("(+12 lines)", EntryText.ExtraLinesLabel(12));
    }
}
