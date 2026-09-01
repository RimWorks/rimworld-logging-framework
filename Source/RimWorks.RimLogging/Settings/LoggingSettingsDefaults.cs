namespace RimWorks.RimLogging.Settings;

/// <summary>
/// Canonical default values for . Referenced from the field initializers, the Scribe_Values.Look defaults
/// in , and the Reset button in LoggingSettingsWindow so the three sites cannot drift apart.
/// </summary>
internal static class LoggingSettingsDefaults
{
    internal const LogLevel GlobalMinLevel = LogLevel.Info;
    internal const int RetentionCount = 5;
    internal const string ProxyUrl = "https://rimlogging-bundle.cryptiklemur.workers.dev/v1/bundle";
    internal const bool CaptureStackTraces = true;
    internal const string GitHubToken = "";
}
