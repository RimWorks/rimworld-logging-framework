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
            // before Init: a throw in there would otherwise leave the provider null, and
            // IsPrimary then reads true off a bootstrap that never finished
            Logging.IsDegradedProvider = () => DegradedMode.IsPresent;
            Logging.Init();
            Logging.GlobalMinLevel = settings.globalMinLevel;
            Logging.CaptureStackTraces = settings.captureStackTraces;
            if (Hijack.HijackBootstrap.Install())
                Log.InfoTo(Log.SelfChannel, "RimLogging initialized");
            else
                Log.WarnTo(Log.SelfChannel,
                    "Another RimLogging instance already installed; running in degraded mode");
        }
        catch (System.Exception ex)
        {
            PanicLog.Write("[RimLogging] early bootstrap failed: " + ex);
        }
    }
}
