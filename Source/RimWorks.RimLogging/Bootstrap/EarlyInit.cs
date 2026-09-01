using RimWorks.RimLogging.Settings;

namespace RimWorks.RimLogging.Bootstrap;

/// <summary>
/// Starts the pipeline and installs the Verse.Log hijack. Runs from <see cref="LoggingMod"/>'s
/// constructor, before any static constructor, so the hijack catches other mods' load-time logs.
/// </summary>
internal static class EarlyInit
{
    /// <summary>
    /// Runs early bootstrap using the supplied settings. Failures are caught and
    /// reported via <c>Verse.Log</c> so a bootstrap error never aborts mod loading.
    /// </summary>
    /// <param name="settings">The loaded logging settings to apply.</param>
    internal static void Run(LoggingSettings settings)
    {
        try
        {
            Logging.InstallShutdownHook = Pipeline.ShutdownFlush.Install;
            Logging.Init();
            Logging.IsDegradedProvider = () => DegradedMode.IsPresent;
            Logging.GlobalMinLevel = settings.globalMinLevel;
            Logging.CaptureStackTraces = settings.captureStackTraces;
            if (Hijack.HijackBootstrap.Install())
                Log.Info("RimWorks.RimLogging", "RimLogging initialized");
            else
                Log.Warn("RimWorks.RimLogging",
                    "Another RimLogging instance already installed; running in degraded mode");
        }
        catch (System.Exception ex)
        {
            PanicLog.Write("[RimLogging] early bootstrap failed: " + ex);
        }
    }
}
