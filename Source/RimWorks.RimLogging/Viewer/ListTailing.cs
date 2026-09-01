using UnityEngine;
using Verse;

namespace RimWorks.RimLogging.Viewer;

/// <summary>
/// Keeps the log list pinned to the newest entry until the user scrolls away, and stays out of
/// the way while they drag. Owns the state that decides when the list may move on its own.
/// </summary>
internal sealed class ListTailing {
    private bool following = true;
    private bool dragging;
    private float lastContentHeight = -1f;

    /// <summary>Whether the list is currently following new entries.</summary>
    internal bool Following => following;

    /// <summary>Reads this frame's input and returns the scroll position to draw with.</summary>
    internal float BeforeScrollView(Rect rect, float scrollY, float contentHeight) {
        if (Event.current.rawType == EventType.MouseUp) {
            dragging = false;
        }
        else if (Event.current.type == EventType.MouseDown && Mouse.IsOver(rect)) {
            dragging = true;
        }

        if (dragging || (Event.current.type == EventType.ScrollWheel && Mouse.IsOver(rect))) {
            following = false;
        }

        // only jump when rows actually arrived. moving every frame fought the drag instead.
        if (following && contentHeight != lastContentHeight) {
            scrollY = TailScroll.MaxScroll(rect.height, contentHeight);
        }
        lastContentHeight = contentHeight;
        return scrollY;
    }

    /// <summary>Decides whether following resumes, once the scroll view has run.</summary>
    internal void AfterScrollView(Rect rect, float scrollY, float contentHeight) {
        // holding the bar must never re-pin the list mid-drag
        if (!dragging) {
            following = TailScroll.IsAtBottom(scrollY, rect.height, contentHeight);
        }
    }
}
