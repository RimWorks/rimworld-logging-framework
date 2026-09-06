using System;

namespace RimWorks.RimLogging.Capture;

/// <summary>
/// Reads a bracketed tag off the head of a captured message. A library that logs through
/// Verse.Log gets attributed to whatever frame the walk lands on, which is usually not itself.
/// </summary>
public static class TaggedMessageChannel
{
    /// <summary>Tag root Concord writes, as in <c>[Concord.Coex] hook-installed</c>.</summary>
    public const string ConcordRoot = "Concord";

    /// <summary>Simple name of Concord's assembly, which is how its packageId is looked up.</summary>
    public const string ConcordAssembly = "Concord";

    /// <summary>
    /// The channel a leading <c>[Root.Sub]</c> tag implies, or null when the message carries no
    /// such tag. The sub-tag is kept, so Concord.Coex nests under Concord in the channel tree.
    /// </summary>
    public static string? Read(string? message, string? root)
    {
        if (string.IsNullOrEmpty(message) || string.IsNullOrEmpty(root)) return null;
        if (message![0] != '[') return null;

        int close = message.IndexOf(']');
        if (close < 2) return null;

        string tag = message.Substring(1, close - 1);
        if (!IsChannelSegment(tag)) return null;

        return tag.Equals(root, StringComparison.Ordinal)
            || tag.StartsWith(root + ".", StringComparison.Ordinal)
            ? tag
            : null;
    }

    // a tag with a space or punctuation is prose, not a channel, so it is left alone
    private static bool IsChannelSegment(string tag)
    {
        if (tag.Length == 0 || tag[0] == '.' || tag[tag.Length - 1] == '.') return false;

        foreach (char c in tag)
        {
            if (c != '.' && c != '_' && !char.IsLetterOrDigit(c)) return false;
        }
        return true;
    }
}
