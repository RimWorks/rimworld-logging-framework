using RimWorks.RimLogging.Capture;
using RimWorks.RimLogging.Pipeline;
using UnityEngine;

namespace RimWorks.RimLogging.Hijack;

/// <summary>
/// Subscribes to Unity's threaded log callback so messages emitted via
/// <c>UnityEngine.Debug.Log</c> route into the framework. The framework
/// catches what slips past the Verse.Log Harmony patches.
/// </summary>
internal static class UnityLogBridge
{
    private static volatile bool _installed;

    /// <summary>Subscribes to <see cref="Application.logMessageReceivedThreaded"/>.</summary>
    internal static void Install()
    {
        if (_installed) return;
        _installed = true;
        Application.logMessageReceivedThreaded += OnUnityLog;
    }

    /// <summary>Unsubscribes from <see cref="Application.logMessageReceivedThreaded"/>.</summary>
    internal static void Uninstall()
    {
        if (!_installed) return;
        _installed = false;
        Application.logMessageReceivedThreaded -= OnUnityLog;
    }

    private static void OnUnityLog(string condition, string stackTrace, LogType type)
    {
        if (ReentryGuard.IsInsideSink) return;
        LogLevel level = UnityLevelMapping.FromUnityLogTypeId((int)type);
        Log.EmitCaptured(level, "Unity", condition, stackTrace);
    }
}
