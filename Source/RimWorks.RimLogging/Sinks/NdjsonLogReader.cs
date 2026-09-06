using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using RimWorks.RimLogging.Capture;

namespace RimWorks.RimLogging.Sinks;

/// <summary>Reads back the NDJSON that <see cref="RollingJsonFileSink"/> writes, skipping malformed lines.</summary>
public static class NdjsonLogReader
{
    /// <summary>Matches the viewer's ring, so reading past this just throws rows away.</summary>
    public const int DefaultMax = 20000;

    /// <summary>Parses a log file, keeping at most the newest entries, oldest first.</summary>
    public static IReadOnlyList<LogEntry> ReadFile(string path, int max = DefaultMax)
        => ReadLines(File.ReadLines(path), max);

    /// <summary>Parses ndjson lines, keeping at most the newest entries, oldest first.</summary>
    public static IReadOnlyList<LogEntry> ReadLines(IEnumerable<string> lines, int max = DefaultMax)
    {
        List<LogEntry> entries = new List<LogEntry>();
        foreach (string line in lines)
        {
            LogEntry? entry = TryParse(line);
            if (entry != null) entries.Add(entry);
        }

        if (max > 0 && entries.Count > max) entries.RemoveRange(0, entries.Count - max);
        return entries;
    }

    private static LogEntry? TryParse(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(line);
            return doc.RootElement.ValueKind == JsonValueKind.Object ? Build(doc.RootElement) : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static LogEntry Build(JsonElement row)
    {
        Dictionary<string, object?>? context = ReadContext(row);
        FoldException(row, ref context);

        return new LogEntry
        {
            Timestamp = ReadTimestamp(row),
            Level = ReadLevel(row),
            Channel = Text(row, "channel") ?? Log.DefaultChannel,
            MessageTemplate = Text(row, "tmpl") ?? string.Empty,
            RenderedMessage = Text(row, "msg") ?? string.Empty,
            Context = context,
            Source = ReadSource(row),
            StackTrace = Text(row, "stack"),
            Mod = Text(row, "mod"),
            Tick = Int(row, "tick"),
            Repeats = Int(row, "repeats") ?? 1,
            PatchedBy = ReadPatchedBy(row),
        };
    }

    private static DateTime ReadTimestamp(JsonElement row)
    {
        return DateTime.TryParse(Text(row, "ts"), CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime parsed)
            ? parsed
            : default;
    }

    private static LogLevel ReadLevel(JsonElement row)
        => Enum.TryParse(Text(row, "level"), ignoreCase: true, out LogLevel level) ? level : LogLevel.Info;

    private static SourceLocation ReadSource(JsonElement row)
    {
        string? src = Text(row, "src");
        if (string.IsNullOrEmpty(src)) return SourceLocation.Empty;

        int split = src!.LastIndexOf(':');
        if (split <= 0 || !int.TryParse(src.Substring(split + 1), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out int line))
        {
            return SourceLocation.Empty;
        }
        return new SourceLocation(src.Substring(0, split), line, null);
    }

    private static Dictionary<string, object?>? ReadContext(JsonElement row)
    {
        if (!row.TryGetProperty("ctx", out JsonElement ctx) || ctx.ValueKind != JsonValueKind.Object) return null;

        Dictionary<string, object?> result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (JsonProperty pair in ctx.EnumerateObject()) result[pair.Name] = ValueOf(pair.Value);
        return result.Count == 0 ? null : result;
    }

    // an Exception cannot be rebuilt from a name and a message, so its parts ride in the context
    private static void FoldException(JsonElement row, ref Dictionary<string, object?>? context)
    {
        if (!row.TryGetProperty("exc", out JsonElement exc) || exc.ValueKind != JsonValueKind.Object) return;

        context ??= new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (JsonProperty pair in exc.EnumerateObject()) context["exception_" + pair.Name] = ValueOf(pair.Value);
    }

    private static List<string>? ReadPatchedBy(JsonElement row)
    {
        // null means attribution never ran, which is a different claim from an empty list
        if (!row.TryGetProperty("patched", out JsonElement patched) || patched.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        List<string> owners = new List<string>(patched.GetArrayLength());
        foreach (JsonElement owner in patched.EnumerateArray())
        {
            if (owner.ValueKind == JsonValueKind.String) owners.Add(owner.GetString()!);
        }
        return owners;
    }

    private static object? ValueOf(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        // the cast matters: without it the ternary unifies to double and whole numbers lose their type
        JsonValueKind.Number => value.TryGetInt64(out long whole) ? (object)whole : value.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => value.GetRawText(),
    };

    private static string? Text(JsonElement row, string name)
        => row.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? Int(JsonElement row, string name)
        => row.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : null;
}
