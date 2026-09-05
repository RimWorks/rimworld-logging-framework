using RimWorks.RimLogging.Viewer;
using Xunit;

namespace RimWorks.RimLogging.Tests.Viewer;

/// <summary>
/// Guards the slow-drag fix. Unity stops emitting MouseDrag once the pointer creeps, so the
/// detail pane re-aims the selection itself on repaint; these pin down when it may do that.
/// </summary>
public class DragSelectPolicyTests
{
    private const int Field = 75;

    [Fact]
    public void PointerMovedWhileTheFieldOwnsTheDrag_Extends()
    {
        Assert.True(DragSelectPolicy.ShouldExtend(true, Field, Field, ownsRect: true, dx: 3f, dy: 0f));
    }

    [Fact]
    public void OnlyOnRepaint_SoItCannotFightUnitysOwnDragHandling()
    {
        Assert.False(DragSelectPolicy.ShouldExtend(false, Field, Field, ownsRect: true, dx: 3f, dy: 0f));
    }

    [Fact]
    public void NoButtonHeld_DoesNotExtend()
    {
        Assert.False(DragSelectPolicy.ShouldExtend(true, 0, 0, ownsRect: true, dx: 3f, dy: 0f));
    }

    [Fact]
    public void ButtonOrScrollbarHoldsTheMouseWithoutFocus_DoesNotExtend()
    {
        // a Widgets.ButtonText takes hotControl but never keyboardControl
        Assert.False(DragSelectPolicy.ShouldExtend(true, Field, 0, ownsRect: true, dx: 3f, dy: 0f));
        Assert.False(DragSelectPolicy.ShouldExtend(true, Field, 91, ownsRect: true, dx: 3f, dy: 0f));
    }

    [Fact]
    public void AnotherFieldOwnsTheDrag_DoesNotExtend()
    {
        // the pane draws two text areas, so only the one holding the mouse may move its cursor
        Assert.False(DragSelectPolicy.ShouldExtend(true, Field, Field, ownsRect: false, dx: 3f, dy: 0f));
    }

    [Fact]
    public void PointerStillOrBarelyMoved_DoesNotRemeasureTheText()
    {
        Assert.False(DragSelectPolicy.ShouldExtend(true, Field, Field, ownsRect: true, dx: 0f, dy: 0f));
        Assert.False(DragSelectPolicy.ShouldExtend(true, Field, Field, ownsRect: true, dx: 0.2f, dy: 0.2f));
    }

    [Fact]
    public void MovementIsMeasuredOnBothAxesTogether()
    {
        // MinMove is a radius, so a purely vertical creep counts the same as a horizontal one
        Assert.True(DragSelectPolicy.ShouldExtend(true, Field, Field, ownsRect: true,
            dx: 0f, dy: DragSelectPolicy.MinMove));
        Assert.True(DragSelectPolicy.ShouldExtend(true, Field, Field, ownsRect: true,
            dx: -DragSelectPolicy.MinMove, dy: 0f));
    }

    [Fact]
    public void ACreepTooSlowForUnityToReportStillAccumulatesIntoAnExtension()
    {
        // the reported bug: 218px over 13s at 233fps is 0.07px per frame, which Unity drops
        float travelled = 0f;
        int extensions = 0;
        for (int frame = 0; frame < 3038; frame++)
        {
            travelled += 218f / 3038f;
            if (!DragSelectPolicy.ShouldExtend(true, Field, Field, ownsRect: true, travelled, 0f)) continue;

            extensions++;
            travelled = 0f;
        }

        // 218px at a 0.5px step is 436 ideally; resetting the accumulator drops the leftover
        // fraction each time, so a couple go missing. Freezing at zero is the failure to catch.
        Assert.InRange(extensions, 430, 436);
    }
}
