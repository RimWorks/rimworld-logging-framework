using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using RimWorks.RimLogging.Format;

namespace RimWorks.RimLogging.Sinks;

/// <summary>
/// Writes log entries as NDJSON (one JSON object per line) to a per-session file,
/// and deletes the oldest files beyond the configured retention count.
/// </summary>
public sealed class RollingJsonFileSink : RollingFileSink
{
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <inheritdoc/>
    public override string Name => "RollingJson";

    /// <summary>
    /// Initializes a new <see cref="RollingJsonFileSink"/>.
    /// </summary>
    /// <param name="logDirectory">Directory in which log files are created; created if absent.</param>
    /// <param name="retainCount">Maximum number of log files to keep; oldest beyond this count are deleted.</param>
    /// <param name="minLevel">Entries below this level are silently dropped.</param>
    public RollingJsonFileSink(string logDirectory, int retainCount = 5, LogLevel minLevel = LogLevel.Trace)
        : base(logDirectory, retainCount, minLevel, "ndjson")
    {
    }

    /// <inheritdoc/>
    protected override string FormatLine(LogEntry entry) => BuildJson(entry);

    private static string BuildJson(LogEntry entry)
    {
        string? src = entry.Source.IsCallerProvided
            ? $"{entry.Source.File}:{entry.Source.Line}"
            : null;

        Dictionary<string, object?> row = new Dictionary<string, object?>
        {
            ["ts"] = entry.Timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
            ["level"] = entry.Level.ToString().ToUpperInvariant(),
            ["channel"] = entry.Channel,
            ["src"] = src,
            ["msg"] = RichText.Strip(entry.RenderedMessage),
            ["tmpl"] = entry.MessageTemplate,
            ["ctx"] = BuildCtx(entry.Context),
            ["stack"] = entry.StackTrace,
            ["exc"] = BuildExc(entry.Exception),
            ["mod"] = entry.Mod,
            ["tick"] = entry.Tick,
            ["repeats"] = entry.Repeats,
            // null and empty are different claims: null means attribution never ran
            ["patched"] = entry.PatchedBy,
        };

        return JsonSerializer.Serialize(row, _jsonOptions);
    }

    private static Dictionary<string, object?>? BuildCtx(IReadOnlyDictionary<string, object?>? context)
    {
        if (context == null || context.Count == 0) return null;
        Dictionary<string, object?> result = new Dictionary<string, object?>();
        foreach (KeyValuePair<string, object?> pair in context)
            result[pair.Key] = pair.Value;
        return result;
    }

    private static Dictionary<string, object?>? BuildExc(Exception? ex)
    {
        if (ex == null) return null;

        return new Dictionary<string, object?>
        {
            ["type"] = ex.GetType().FullName,
            ["message"] = ex.Message,
            ["stack"] = ex.StackTrace,
        };
    }
}
