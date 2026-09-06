using System;
using System.Collections.Generic;
using System.Globalization;

namespace RimWorks.RimLogging.Viewer;

/// <summary>
/// Distinct context keys and their values, kept as entries land so the filter box can complete
/// <c>ctx.</c> without rescanning the buffer.
/// </summary>
public sealed class ContextIndex
{
    /// <summary>Keys tracked before new ones are ignored.</summary>
    public const int MaxKeys = 64;

    /// <summary>Values kept per key. A key holding a tick or an id would grow without one.</summary>
    public const int MaxValuesPerKey = 64;

    private static readonly string[] NoValues = Array.Empty<string>();

    private readonly Dictionary<string, HashSet<string>> byKey =
        new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Records every key and value in one entry's context. Null contexts are ignored.</summary>
    public void Add(IReadOnlyDictionary<string, object?>? context)
    {
        if (context == null) return;

        foreach (KeyValuePair<string, object?> pair in context)
        {
            if (string.IsNullOrEmpty(pair.Key)) continue;

            if (!byKey.TryGetValue(pair.Key, out HashSet<string>? values))
            {
                if (byKey.Count >= MaxKeys) continue;
                values = new HashSet<string>(StringComparer.Ordinal);
                byKey[pair.Key] = values;
            }

            if (values.Count >= MaxValuesPerKey) continue;

            // matches how the compiler stringifies a value, so a suggestion always filters
            values.Add(Convert.ToString(pair.Value, CultureInfo.InvariantCulture) ?? string.Empty);
        }
    }

    /// <summary>Every key seen, sorted, ready to be offered after <c>ctx.</c>.</summary>
    public IReadOnlyList<string> Keys()
    {
        List<string> keys = new List<string>(byKey.Count);
        foreach (KeyValuePair<string, HashSet<string>> pair in byKey) keys.Add(pair.Key);
        keys.Sort(StringComparer.OrdinalIgnoreCase);
        return keys;
    }

    /// <summary>Values seen for one key, sorted. Empty when the key has never been seen.</summary>
    public IReadOnlyList<string> ValuesFor(string? key)
    {
        if (string.IsNullOrEmpty(key) || !byKey.TryGetValue(key!, out HashSet<string>? values)) return NoValues;

        List<string> sorted = new List<string>(values);
        sorted.Sort(StringComparer.OrdinalIgnoreCase);
        return sorted;
    }

    /// <summary>Drops everything, for when the viewer buffer is cleared.</summary>
    public void Clear() => byKey.Clear();

    /// <summary>Walks a whole snapshot, for a log loaded from a file rather than streamed.</summary>
    public static ContextIndex FromSnapshot(IReadOnlyList<LogEntry> entries)
    {
        ContextIndex index = new ContextIndex();
        for (int i = 0; i < entries.Count; i++) index.Add(entries[i].Context);
        return index;
    }
}
