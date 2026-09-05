namespace RimWorks.RimLogging.Viewer;

/// <summary>
/// Decides whether a read-only text area should extend its own selection on a repaint.
/// Unity grows a selection only inside <c>EventType.MouseDrag</c>, and it stops emitting those
/// once the pointer creeps below roughly a pixel per frame, which strands a slow drag.
/// </summary>
public static class DragSelectPolicy
{
    /// <summary>Pointer travel below this is not worth re-measuring the text for.</summary>
    public const float MinMove = 0.5f;

    /// <summary>
    /// True when this frame should re-aim the selection at the pointer. Callers pass the ids
    /// rather than reading them so the decision can be exercised without a live GUI.
    /// </summary>
    /// <param name="isRepaint">Whether the current pass is a repaint.</param>
    /// <param name="hotControl">The control holding the mouse, or 0 when no button is down.</param>
    /// <param name="keyboardControl">The focused control, which a text field sets alongside hot.</param>
    /// <param name="ownsRect">Whether the hot control is the field being drawn right now.</param>
    /// <param name="dx">Pointer movement on x since the last extension.</param>
    /// <param name="dy">Pointer movement on y since the last extension.</param>
    public static bool ShouldExtend(bool isRepaint, int hotControl, int keyboardControl, bool ownsRect,
        float dx, float dy)
    {
        // the real drag events already did this, and running mid-event fights Unity's own handling
        if (!isRepaint) return false;

        if (hotControl == 0) return false;

        // a button or a scrollbar takes hot without taking focus, so this rules them out
        if (hotControl != keyboardControl) return false;

        if (!ownsRect) return false;

        return (dx * dx) + (dy * dy) >= MinMove * MinMove;
    }
}
