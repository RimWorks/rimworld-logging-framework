using System.Collections.Generic;
using RimWorks.RimLogging;
using RimWorks.RimLogging.Bundle;
using Verse;

namespace RimWorks.RimLogging.Settings;

/// <summary>Persisted RimWorld <see cref="ModSettings"/> for the logging framework (min level, output directory, retention, bundle upload endpoint, filter presets).</summary>
public sealed class LoggingSettings : ModSettings
{
    /// <summary>Global minimum log level; entries below this are dropped.</summary>
    public LogLevel globalMinLevel = LoggingSettingsDefaults.GlobalMinLevel;

    /// <summary>Directory where log files are written; empty until normalized to the default.</summary>
    public string logDirectory = string.Empty;

    /// <summary>Number of rotated log files to retain.</summary>
    public int retentionCount = LoggingSettingsDefaults.RetentionCount;

    /// <summary>Display names of saved filter presets, parallel to <see cref="filterPresetExpressions"/>.</summary>
    public List<string> filterPresetNames = new();

    /// <summary>Filter DSL expressions for saved presets, parallel to <see cref="filterPresetNames"/>.</summary>
    public List<string> filterPresetExpressions = new();

    /// <summary>Sink def names the user set explicitly; anything absent uses the def's own default.</summary>
    public List<string> sinkOverrideNames = new();

    /// <summary>On/off states parallel to <see cref="sinkOverrideNames"/>.</summary>
    public List<bool> sinkOverrideStates = new();

    /// <summary>When <c>true</c>, every emitted entry captures and stores a formatted stack trace. Defaults to <c>true</c>.</summary>
    public bool captureStackTraces = LoggingSettingsDefaults.CaptureStackTraces;

    /// <summary>The rimworks.org endpoint bug-report bundles are uploaded to.</summary>
    public string rimworksUrl = LoggingSettingsDefaults.RimWorksUrl;

    /// <summary>Where the log viewer draws the detail pane. Cycled from the viewer's own toolbar.</summary>
    internal Viewer.DetailPlacement logViewerDetailPlacement = Viewer.DetailPlacement.Bottom;

    /// <summary>Serializes and deserializes the settings via RimWorld's Scribe system, applying defaults on post-load.</summary>
    public override void ExposeData()
    {
        Scribe_Values.Look(ref globalMinLevel, "globalMinLevel", LoggingSettingsDefaults.GlobalMinLevel);
        Scribe_Values.Look(ref logDirectory, "logDirectory", string.Empty);
        Scribe_Values.Look(ref retentionCount, "retentionCount", LoggingSettingsDefaults.RetentionCount);
        Scribe_Values.Look(ref captureStackTraces, "captureStackTraces", LoggingSettingsDefaults.CaptureStackTraces);
        Scribe_Values.Look(ref rimworksUrl, "rimworksUrl", LoggingSettingsDefaults.RimWorksUrl);
        Scribe_Values.Look(ref logViewerDetailPlacement, "logViewerDetailPlacement", Viewer.DetailPlacement.Bottom);
        Scribe_Collections.Look(ref filterPresetNames, "filterPresetNames", LookMode.Value);
        Scribe_Collections.Look(ref filterPresetExpressions, "filterPresetExpressions", LookMode.Value);
        Scribe_Collections.Look(ref sinkOverrideNames, "sinkOverrideNames", LookMode.Value);
        Scribe_Collections.Look(ref sinkOverrideStates, "sinkOverrideStates", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            filterPresetNames ??= new List<string>();
            filterPresetExpressions ??= new List<string>();
            sinkOverrideNames ??= new List<string>();
            sinkOverrideStates ??= new List<bool>();
            if (string.IsNullOrEmpty(logDirectory)) logDirectory = LogDirectory.Default;
        }
    }

    internal PublishOptions ToPublishOptions() => new PublishOptions
    {
        RimWorksUrl = rimworksUrl,
    };
}
