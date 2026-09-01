using RimWorks.RimLogging.Settings;

namespace RimWorks.RimLogging.Bootstrap;

/// <summary>
/// Performs early framework bootstrap: starts the logging pipeline, wires the
/// shutdown-flush and degraded-mode hooks, applies the configured global minimum
/// level, and installs the <c>Verse.Log</c>/Unity hijack. Invoked from
/// <see cref="LoggingMod"/> during mod loading (in mod-list order), well before
/// any <see cref="Verse.StaticConstructorOnStartup"/>, so the hijack is live
/// before other mods emit their load-time logs.
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
