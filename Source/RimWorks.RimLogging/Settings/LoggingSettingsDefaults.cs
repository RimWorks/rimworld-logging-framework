namespace RimWorks.RimLogging.Settings;

/// <summary>
/// Canonical default values for <see cref="LoggingSettings"/>. Referenced from the field initializers,
/// the Scribe_Values.Look defaults, and the Reset button so the three sites cannot drift apart.
/// </summary>
internal static class LoggingSettingsDefaults
{
    internal const LogLevel GlobalMinLevel = LogLevel.Info;
    internal const int RetentionCount = 5;
    internal const bool CaptureStackTraces = true;
    internal const string RimWorksUrl = "https://rimworks.org/api/bundle";
}
