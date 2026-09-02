using System;
using System.Collections.Generic;

namespace RimWorks.RimLogging.Filtering;

/// <summary>
/// Saved filter expressions, held as two parallel lists because that is what the settings
/// scribe already persists. Every read guards against the two drifting out of step.
/// </summary>
internal static class FilterPresets
{
    /// <summary>Returns the index of a preset by name, or -1 when it is absent or unusable.</summary>
    internal static int IndexOf(IReadOnlyList<string>? names, IReadOnlyList<string>? expressions, string? name)
    {
        if (names == null || expressions == null || string.IsNullOrEmpty(name)) return -1;

        int usable = Math.Min(names.Count, expressions.Count);
        for (int i = 0; i < usable; i++)
        {
            if (string.Equals(names[i], name, StringComparison.OrdinalIgnoreCase)) return i;
        }
        return -1;
    }

    /// <summary>Returns the expression saved under a name, or <c>null</c> when there is none.</summary>
    internal static string? Expression(IReadOnlyList<string>? names, IReadOnlyList<string>? expressions, string? name)
    {
        int index = IndexOf(names, expressions, name);
        return index < 0 ? null : expressions![index];
    }

    /// <summary>Saves an expression under a name, replacing any preset already using it.</summary>
    internal static void Save(List<string> names, List<string> expressions, string? name, string? expression)
    {
        if (names == null || expressions == null) return;
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(expression)) return;

        string trimmed = name!.Trim();
        int index = IndexOf(names, expressions, trimmed);
        if (index >= 0)
        {
            expressions[index] = expression!;
            return;
        }

        // a drifted pair would pair the new name with someone else's expression
        Realign(names, expressions);
        names.Add(trimmed);
        expressions.Add(expression!);
    }

    /// <summary>Removes a preset by name. Returns <c>true</c> when one was removed.</summary>
    internal static bool Remove(List<string> names, List<string> expressions, string? name)
    {
        int index = IndexOf(names, expressions, name);
        if (index < 0) return false;

        names.RemoveAt(index);
        expressions.RemoveAt(index);
        return true;
    }

    /// <summary>Returns the names that have an expression to go with them.</summary>
    internal static List<string> Usable(IReadOnlyList<string>? names, IReadOnlyList<string>? expressions)
    {
        List<string> usable = new List<string>();
        if (names == null || expressions == null) return usable;

        int count = Math.Min(names.Count, expressions.Count);
        for (int i = 0; i < count; i++) usable.Add(names[i]);
        return usable;
    }

    private static void Realign(List<string> names, List<string> expressions)
    {
        int count = Math.Min(names.Count, expressions.Count);
        if (names.Count > count) names.RemoveRange(count, names.Count - count);
        if (expressions.Count > count) expressions.RemoveRange(count, expressions.Count - count);
    }
}
