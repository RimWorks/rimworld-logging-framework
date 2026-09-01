using System;
using System.Reflection;
using LudeonTK;
using Verse;

namespace RimWorks.RimLogging.Viewer;

internal static class LogViewerBoot {
    public static ViewerLogSink? Sink { get; private set; }

    private static bool reclaimPending;

    /// <summary>Returns true once after the sink registers, so the patch can check for a stolen window.</summary>
    public static bool ConsumeReclaim() {
        bool pending = reclaimPending;
        reclaimPending = false;
        return pending;
    }

    public static void Init() {
        if (Sink != null) {
            return;
        }
        try {
            ChannelClassifierBootstrap.Install();
            ViewerLogSink sink = new ViewerLogSink();
            Logging.RegisterSink(sink);
            Sink = sink;
            reclaimPending = true;
            Log.Info("Log viewer sink registered");
        }
        catch (Exception ex) {
            Log.Error("Failed to register log viewer sink: " + ex);
        }
    }

    // EditWindow_Log.canAutoOpen is private, and it is the same flag vanilla's own button flips
    private static readonly FieldInfo? CanAutoOpenField =
        typeof(EditWindow_Log).GetField("canAutoOpen", BindingFlags.NonPublic | BindingFlags.Static);

    public static bool AutoOpen {
        get => CanAutoOpenField?.GetValue(null) as bool? ?? false;
        set => CanAutoOpenField?.SetValue(null, value);
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
