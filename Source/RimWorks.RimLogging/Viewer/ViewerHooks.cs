using System;
using LudeonTK;
using UnityEngine;
using Verse;

namespace RimWorks.RimLogging.Viewer;

/// <summary>Log-window takeover, stated once so each patch backend only has to call it.</summary>
internal static class ViewerHooks
{
    // whatever escapes Open reaches Root.OnGUI's catch, which logs, which re-arms wantsToOpen:
    // without the latch one failed open becomes another every frame
    private static bool viewerBroken;

    /// <summary>Head of <c>DebugWindowsOpener.ToggleLogWindow</c>. Shift falls through to vanilla.</summary>
    internal static bool OnToggleLogWindow()
    {
        if (Event.current != null && Event.current.shift)
        {
            return true;
        }
        ViewerLogSink? sink = LogViewerBoot.Sink;
        WindowStack? windowStack = Find.WindowStack;
        if (sink == null || windowStack == null)
        {
            return true;
        }
        if (!windowStack.TryRemove(typeof(LogViewerWindow)))
        {
            Open(windowStack, sink, selectNewest: false);
        }
        return false;
    }

    // UIRoot.CheckOpenLogWindow is the only place vanilla builds an EditWindow_Log, and it runs
    // every UI tick, so every auto-open path funnels through here whoever set wantsToOpen
    internal static bool OnCheckOpenLogWindow()
    {
        WindowStack? windowStack = Find.WindowStack;
        if (windowStack == null)
        {
            return true;
        }

        ViewerLogSink? sink = LogViewerBoot.Sink;
        bool reclaim = LogViewerBoot.ConsumeReclaim();

        if (AutoOpenDecision.ShouldReclaim(reclaim, sink != null, windowStack.IsOpen<EditWindow_Log>(), viewerBroken))
        {
            windowStack.TryRemove(typeof(EditWindow_Log));
            Open(windowStack, sink!, selectNewest: true);
            return false;
        }

        if (!AutoOpenDecision.ShouldTakeOver(EditWindow_Log.wantsToOpen, sink != null, true, viewerBroken))
        {
            return true;
        }

        EditWindow_Log.wantsToOpen = false;
        Open(windowStack, sink!, selectNewest: true);
        return false;
    }

    private static void Open(WindowStack windowStack, ViewerLogSink sink, bool selectNewest)
    {
        if (windowStack.WindowOfType<LogViewerWindow>() != null)
        {
            return;
        }
        try
        {
            windowStack.Add(new LogViewerWindow(sink, selectNewest));
        }
        catch (Exception ex)
        {
            // vanilla's Log, not ours: ours feeds the window that just failed to open
            viewerBroken = true;
            Verse.Log.Warning("RimLogging viewer failed to open, vanilla log window takes over: " + ex);
        }
    }
}
