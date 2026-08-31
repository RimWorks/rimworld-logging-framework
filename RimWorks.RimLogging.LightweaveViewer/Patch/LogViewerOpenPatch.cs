using Concord;
using Verse;

namespace RimWorks.RimLogging.LightweaveViewer;

[Patch(typeof(Verse.Log))]
internal static class LogViewerOpenPatch {
    [Inject(At.Head, nameof(Verse.Log.TryOpenLogWindow))]
    private static Control Prefix() {
        LightweaveLogSink? sink = LogViewerBoot.Sink;
        WindowStack? windowStack = Find.WindowStack;
        if (sink == null || windowStack == null) {
            return Control.Continue;
        }
        if (windowStack.WindowOfType<LogViewerWindow>() == null) {
            windowStack.Add(new LogViewerWindow(sink));
        }
        return Control.Cancel;
    }
}
