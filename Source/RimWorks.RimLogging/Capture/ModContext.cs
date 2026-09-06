using System;
using System.Collections.Generic;

namespace RimWorks.RimLogging.Capture;

/// <summary>
/// Adds the owning mod's packageId to an entry's structured context, so it can be filtered with
/// <c>ctx.mod_id</c> and survives into the file sinks.
/// </summary>
public static class ModContext
{
    /// <summary>Context key holding the owning mod's packageId.</summary>
    public const string ModIdKey = "mod_id";

    /// <summary>
    /// Returns the context with the packageId added. A key the caller set themselves wins, since
    /// their value is the one they can predict when they write a filter.
    /// </summary>
    public static IReadOnlyDictionary<string, object?>? WithModId(
        IReadOnlyDictionary<string, object?>? context,
        string? modId)
    {
        if (string.IsNullOrEmpty(modId)) return context;

        if (context == null)
        {
            return new Dictionary<string, object?>(1) { [ModIdKey] = modId };
        }

        if (HasModId(context)) return context;

        Dictionary<string, object?> merged = new Dictionary<string, object?>(context.Count + 1);
        foreach (KeyValuePair<string, object?> pair in context) merged[pair.Key] = pair.Value;
        merged[ModIdKey] = modId;
        return merged;
    }

    /// <summary>
    /// The packageId to fall back on when caller resolution found none. Only RimLogging's own
    /// channel gets one, because the caller walk skips our frames and never reaches us.
    /// </summary>
    public static string? Fallback(string? channel, string? selfChannel, string? selfPackageId)
    {
        if (string.IsNullOrEmpty(selfChannel)) return null;

        return string.Equals(channel, selfChannel, StringComparison.Ordinal) ? selfPackageId : null;
    }

    // the filter language looks context keys up case-insensitively, so a differently cased
    // key from the caller would still shadow ours
    private static bool HasModId(IReadOnlyDictionary<string, object?> context)
    {
        foreach (KeyValuePair<string, object?> pair in context)
        {
            if (string.Equals(pair.Key, ModIdKey, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
