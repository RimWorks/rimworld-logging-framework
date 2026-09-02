using System;
using System.Collections.Generic;
using RimWorks.RimLogging.Format;

namespace RimWorks.RimLogging.Bundle;

/// <summary>
/// Builds a <see cref="BundlePayload"/> from captured log entries and environment metadata, flattening each
/// <see cref="LogEntry"/> into a serializable <see cref="BundlePayload.EntryDto"/>.
/// </summary>
public static class Bundler
{
    /// <summary>
    /// Builds the bundle payload. Timestamps go out as ISO-8601, messages lose their rich-text
    /// markup, and source is included as <c>file:line</c> only when the caller supplied it.
    /// </summary>
    /// <param name="entries">The log entries to include.</param>
    /// <param name="rimWorldVersion">The RimWorld game version to record.</param>
    /// <param name="frameworkVersion">The RimLogging framework revision to record.</param>
    /// <param name="mods">The loaded mod list to record.</param>
    /// <returns>A populated <see cref="BundlePayload"/>.</returns>
    public static BundlePayload Build(
        IReadOnlyList<LogEntry> entries,
        string rimWorldVersion,
        string frameworkVersion,
        List<BundlePayload.ModInfo> mods)
    {
        BundlePayload p = new BundlePayload
        {
            RimWorldVersion = rimWorldVersion,
            FrameworkVersion = frameworkVersion,
            Mods = mods,
        };
        for (int i = 0; i < entries.Count; i++)
        {
            LogEntry e = entries[i];
            p.Entries.Add(new BundlePayload.EntryDto
            {
                Timestamp = e.Timestamp.ToString("o"),
                Level = SerializeLevel(e.Level),
                Channel = e.Channel,
                Source = e.Source.IsCallerProvided ? $"{e.Source.File}:{e.Source.Line}" : "",
                Message = RichText.Strip(e.RenderedMessage),
                Context = CopyContext(e.Context, e.PatchedBy),
                Stack = e.StackTrace ?? e.Exception?.ToString(),
            });
        }
        return p;
    }

    /// <summary>
    /// Maps a  to the canonical level name accepted by the bundle upload worker. The worker's accepted set is
    /// Trace, Debug, Info, Warning, Error, Critical, so Warn and Fatal must be translated.
    /// </summary>
    private static string SerializeLevel(LogLevel level) => level switch
    {
        LogLevel.Trace => "Trace",
        LogLevel.Debug => "Debug",
        LogLevel.Info  => "Info",
        LogLevel.Warn  => "Warning",
        LogLevel.Error => "Error",
        LogLevel.Fatal => "Critical",
        _              => level.ToString(),
    };

    private static Dictionary<string, object?>? CopyContext(IReadOnlyDictionary<string, object?>? source, IReadOnlyList<string>? patchedBy)
    {
        if (source == null && patchedBy is not { Count: > 0 }) return null;
        Dictionary<string, object?> copy = new Dictionary<string, object?>(source?.Count ?? 1);
        if (source != null)
        {
            foreach (KeyValuePair<string, object?> kv in source)
                copy[kv.Key] = kv.Value;
        }
        if (patchedBy is { Count: > 0 }) copy["PatchedBy"] = string.Join(",", patchedBy);
        return copy;
    }
}
