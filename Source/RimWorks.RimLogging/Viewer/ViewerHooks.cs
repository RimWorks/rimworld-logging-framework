using LudeonTK;
using UnityEngine;
using Verse;

namespace RimWorks.RimLogging.Viewer;

/// <summary>Log-window takeover, stated once so each patch backend only has to call it.</summary>
internal static class ViewerHooks
{
    /// <summary>Head of <c>Verse.Log.TryOpenLogWindow</c>. True runs vanilla's body.</summary>
    internal static bool OnTryOpenLogWindow()
    {
        ViewerLogSink? sink = LogViewerBoot.Sink;
        WindowStack? windowStack = Find.WindowStack;
        if (sink == null || windowStack == null)
        {
            return true;
        }
        Open(windowStack, sink);
        return false;
    }

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
            windowStack.Add(new LogViewerWindow(sink));
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

        if (AutoOpenDecision.ShouldReclaim(reclaim, sink != null, windowStack.IsOpen<EditWindow_Log>()))
        {
            windowStack.TryRemove(typeof(EditWindow_Log));
            Open(windowStack, sink!);
            return false;
        }

        if (!AutoOpenDecision.ShouldTakeOver(EditWindow_Log.wantsToOpen, sink != null, true))
        {
            return true;
        }

        EditWindow_Log.wantsToOpen = false;
        Open(windowStack, sink!);
        return false;
    }

    private static void Open(WindowStack windowStack, ViewerLogSink sink)
    {
        if (windowStack.WindowOfType<LogViewerWindow>() == null)
        {
            windowStack.Add(new LogViewerWindow(sink, selectNewest: true));
        }
    }
}
