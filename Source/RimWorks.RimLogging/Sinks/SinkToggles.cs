using System;
using System.Collections.Generic;

namespace RimWorks.RimLogging.Sinks;

/// <summary>Resolves whether a sink runs, from the def's default plus the user's explicit overrides.</summary>
public static class SinkToggles
{
    /// <summary>The def's own default, unless the user set this sink explicitly.</summary>
    public static bool IsEnabled(
        string defName,
        bool enabledByDefault,
        IReadOnlyList<string>? overrideNames,
        IReadOnlyList<bool>? overrideStates)
    {
        int index = IndexOf(defName, overrideNames, overrideStates);
        return index < 0 ? enabledByDefault : overrideStates![index];
    }

    /// <summary>Records an explicit choice, replacing any earlier one for the same sink.</summary>
    public static void Set(string defName, bool enabled, List<string> overrideNames, List<bool> overrideStates)
    {
        if (string.IsNullOrEmpty(defName)) return;

        int index = IndexOf(defName, overrideNames, overrideStates);
        if (index >= 0)
        {
            overrideStates[index] = enabled;
            return;
        }
        overrideNames.Add(defName);
        overrideStates.Add(enabled);
    }

    /// <summary>Drops an explicit choice so the sink falls back to its def default.</summary>
    public static void Clear(string defName, List<string> overrideNames, List<bool> overrideStates)
    {
        int index = IndexOf(defName, overrideNames, overrideStates);
        if (index < 0) return;
        overrideNames.RemoveAt(index);
        overrideStates.RemoveAt(index);
    }

    // the lists are written separately by Scribe, so a torn save can leave them different lengths
    private static int IndexOf(string defName, IReadOnlyList<string>? names, IReadOnlyList<bool>? states)
    {
        if (names == null || states == null || string.IsNullOrEmpty(defName)) return -1;

        int usable = Math.Min(names.Count, states.Count);
        for (int i = 0; i < usable; i++)
        {
            if (string.Equals(names[i], defName, StringComparison.Ordinal)) return i;
        }
        return -1;
    }
}
