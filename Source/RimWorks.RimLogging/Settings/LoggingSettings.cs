using System.Collections.Generic;
using RimWorks.RimLogging;
using Verse;

namespace RimWorks.RimLogging.Settings;

/// <summary>Persisted RimWorld <see cref="ModSettings"/> for the logging framework (min level, output directory, retention, bundle proxy, filter presets).</summary>
public sealed class LoggingSettings : ModSettings
{
    /// <summary>Global minimum log level; entries below this are dropped.</summary>
    public LogLevel globalMinLevel = LoggingSettingsDefaults.GlobalMinLevel;

    /// <summary>Directory where log files are written; empty until normalized to the default.</summary>
    public string logDirectory = "";

    /// <summary>Number of rotated log files to retain.</summary>
    public int retentionCount = LoggingSettingsDefaults.RetentionCount;

    /// <summary>Endpoint URL used when uploading bug-report bundles.</summary>
    public string proxyUrl = LoggingSettingsDefaults.ProxyUrl;

    /// <summary>Display names of saved filter presets, parallel to <see cref="filterPresetExpressions"/>.</summary>
    public List<string> filterPresetNames = new();

    /// <summary>Filter DSL expressions for saved presets, parallel to <see cref="filterPresetNames"/>.</summary>
    public List<string> filterPresetExpressions = new();

    /// <summary>When <c>true</c>, every emitted entry captures and stores a formatted stack trace. Defaults to <c>true</c>.</summary>
    public bool captureStackTraces = LoggingSettingsDefaults.CaptureStackTraces;

    /// <summary>Optional user-supplied GitHub PAT; when set, it is relayed to the bundle proxy so gists are created under the user's account. Empty means the proxy uses its own token.</summary>
    public string githubToken = LoggingSettingsDefaults.GitHubToken;

    /// <summary>When <c>true</c>, the log viewer detail pane combines message and stack trace into one scroll area instead of separate panes.</summary>
    public bool logViewerCombinedDetail;

    /// <summary>Where the log viewer draws the detail pane. Cycled from the viewer's own toolbar.</summary>
    internal Viewer.DetailPlacement logViewerDetailPlacement = Viewer.DetailPlacement.Bottom;

    /// <summary>Serializes and deserializes the settings via RimWorld's Scribe system, applying defaults on post-load.</summary>
    public override void ExposeData()
    {
        Scribe_Values.Look(ref globalMinLevel, "globalMinLevel", LoggingSettingsDefaults.GlobalMinLevel);
        Scribe_Values.Look(ref logDirectory, "logDirectory", "");
        Scribe_Values.Look(ref retentionCount, "retentionCount", LoggingSettingsDefaults.RetentionCount);
        Scribe_Values.Look(ref proxyUrl, "proxyUrl", LoggingSettingsDefaults.ProxyUrl);
        Scribe_Values.Look(ref captureStackTraces, "captureStackTraces", LoggingSettingsDefaults.CaptureStackTraces);
        Scribe_Values.Look(ref githubToken, "githubToken", LoggingSettingsDefaults.GitHubToken);
        Scribe_Values.Look(ref logViewerCombinedDetail, "logViewerCombinedDetail", false);
        Scribe_Values.Look(ref logViewerDetailPlacement, "logViewerDetailPlacement", Viewer.DetailPlacement.Bottom);
        Scribe_Collections.Look(ref filterPresetNames, "filterPresetNames", LookMode.Value);
        Scribe_Collections.Look(ref filterPresetExpressions, "filterPresetExpressions", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            filterPresetNames ??= new List<string>();
            filterPresetExpressions ??= new List<string>();
            if (string.IsNullOrEmpty(logDirectory)) logDirectory = LogDirectory.Default;
        }
    }
}
