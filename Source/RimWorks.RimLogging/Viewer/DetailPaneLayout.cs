using System;

namespace RimWorks.RimLogging.Viewer;

/// <summary>
/// Splits the detail pane into the strip holding the copy button and the scrolling body under
/// it. The two must never overlap. Every row in the body is a read-only text area, and a text
/// area takes the mouse down and marks it used, so a row drawn under the button would swallow
/// the click and the button would never fire.
/// </summary>
public static class DetailPaneLayout
{
    /// <summary>Height of the copy button itself.</summary>
    public const float ButtonHeight = 22f;

    /// <summary>Breathing room between the button and the first row.</summary>
    public const float Gap = 4f;

    /// <summary>Vertical space the button strip takes out of the pane.</summary>
    public const float HeaderHeight = ButtonHeight + Gap;

    /// <summary>Top edge of the scrolling body, given the pane's inner top edge.</summary>
    public static float BodyTop(float innerTop)
    {
        return innerTop + HeaderHeight;
    }

    /// <summary>Height left for the scrolling body, never negative on a very short pane.</summary>
    public static float BodyHeight(float innerHeight)
    {
        return Math.Max(0f, innerHeight - HeaderHeight);
    }

    /// <summary>
    /// True when the button strip and the body would overlap. The body is what scrolls, so an
    /// overlap puts some row under the button no matter where the user has scrolled to.
    /// </summary>
    public static bool Overlaps(float buttonTop, float bodyTop)
    {
        return bodyTop < buttonTop + ButtonHeight;
    }
}
