using System;
using LudeonTK;
using Verse;

namespace RimWorks.RimLogging.Viewer;

internal static class LogViewerBoot {
    public static ViewerLogSink? Sink { get; private set; }

    public static void Init() {
        if (Sink != null) {
            return;
        }
        try {
            ChannelClassifierBootstrap.Install();
            ViewerLogSink sink = new ViewerLogSink();
            Logging.RegisterSink(sink);
            Sink = sink;
            Log.Info("Log viewer sink registered");
        }
        catch (Exception ex) {
            Log.Error("Failed to register log viewer sink: " + ex);
        }
    }

    public static void OpenVanilla() {
        WindowStack? windowStack = Find.WindowStack;
        if (windowStack == null) {
            return;
        }
        if (windowStack.WindowOfType<EditWindow_Log>() == null) {
            windowStack.Add(new EditWindow_Log());
        }
    }
}
