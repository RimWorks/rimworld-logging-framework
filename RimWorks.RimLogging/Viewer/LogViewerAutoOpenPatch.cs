using Concord;
using LudeonTK;
using Verse;

namespace RimWorks.RimLogging.Viewer;

// UIRoot.CheckOpenLogWindow is the only place vanilla builds an EditWindow_Log, so every
// auto-open path funnels through here no matter who set wantsToOpen.
[Patch]
internal abstract class LogViewerAutoOpenPatch : UIRoot {
    [Inject(At.Head, "CheckOpenLogWindow")]
    private static Control Prefix() {
        ViewerLogSink? sink = LogViewerBoot.Sink;
        WindowStack? windowStack = Find.WindowStack;
        if (!AutoOpenDecision.ShouldTakeOver(EditWindow_Log.wantsToOpen, sink != null, windowStack != null)) {
            return Control.Continue;
        }

        EditWindow_Log.wantsToOpen = false;
        if (windowStack!.WindowOfType<LogViewerWindow>() == null) {
            windowStack.Add(new LogViewerWindow(sink!, selectNewest: true));
        }
        return Control.Cancel;
    }
}
