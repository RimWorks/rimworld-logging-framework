namespace RimWorks.RimLogging.Settings;

/// <summary>
/// Canonical default values for <see cref="LoggingSettings"/>. Referenced from the field
/// initializers, the <c>Scribe_Values.Look</c> defaults in <see cref="LoggingSettings.ExposeData"/>,
/// and the Reset button in <c>LoggingSettingsWindow</c> so the three sites cannot drift apart.
/// </summary>
internal static class LoggingSettingsDefaults
{
    internal const LogLevel GlobalMinLevel = LogLevel.Info;
    internal const int RetentionCount = 5;
    internal const string ProxyUrl = "https://rimlogging-bundle.cryptiklemur.workers.dev/v1/bundle";
    internal const bool CaptureStackTraces = true;
    internal const string GitHubToken = "";
}
