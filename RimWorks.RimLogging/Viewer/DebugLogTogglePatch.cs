using Concord;
using UnityEngine;
using Verse;

namespace RimWorks.RimLogging.Viewer;

[Patch]
internal abstract class DebugLogTogglePatch : DebugWindowsOpener {
    [Inject(At.Head, "ToggleLogWindow")]
    private Control Prefix() {
        if (Event.current != null && Event.current.shift) {
            return Control.Continue;
        }
        ViewerLogSink? sink = LogViewerBoot.Sink;
        WindowStack? windowStack = Find.WindowStack;
        if (sink == null || windowStack == null) {
            return Control.Continue;
        }
        if (!windowStack.TryRemove(typeof(LogViewerWindow))) {
            windowStack.Add(new LogViewerWindow(sink));
        }
        return Control.Cancel;
    }
}
