using Concord;
using LudeonTK;
using Verse;

namespace RimWorks.RimLogging.Viewer;

// UIRoot.CheckOpenLogWindow is the only place vanilla builds an EditWindow_Log, and it runs
// every UI tick, so every auto-open path funnels through here no matter who set wantsToOpen.
[Patch]
internal abstract class LogViewerAutoOpenPatch : UIRoot {
    [Inject(At.Head, "CheckOpenLogWindow")]
    private static Control Prefix() {
        WindowStack? windowStack = Find.WindowStack;
        if (windowStack == null) {
            return Control.Continue;
        }

        ViewerLogSink? sink = LogViewerBoot.Sink;
        bool reclaim = LogViewerBoot.ConsumeReclaim();

        if (AutoOpenDecision.ShouldReclaim(reclaim, sink != null, windowStack.IsOpen(typeof(EditWindow_Log)))) {
            windowStack.TryRemove(typeof(EditWindow_Log));
            Open(windowStack, sink!);
            return Control.Cancel;
        }

        if (!AutoOpenDecision.ShouldTakeOver(EditWindow_Log.wantsToOpen, sink != null, true)) {
            return Control.Continue;
        }

        EditWindow_Log.wantsToOpen = false;
        Open(windowStack, sink!);
        return Control.Cancel;
    }

    private static void Open(WindowStack windowStack, ViewerLogSink sink) {
        if (windowStack.WindowOfType<LogViewerWindow>() == null) {
            windowStack.Add(new LogViewerWindow(sink, selectNewest: true));
        }
    }
}
