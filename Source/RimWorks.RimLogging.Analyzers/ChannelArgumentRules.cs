namespace RimWorks.RimLogging.Analyzers;

/// <summary>
/// Shared rules for spotting a channel passed as a message. Linked into the code fix project so
/// both read one table.
/// </summary>
internal static class ChannelArgumentRules
{
    /// <summary>Id of the channel-as-message diagnostic, read by the analyzer and the fix alike.</summary>
    internal const string ChannelAsMessageRuleId = "RIMLOG003";

    /// <summary>Suffix marking the sibling overload that takes a channel first.</summary>
    internal const string ChannelSuffix = "To";

    /// <summary>True for a level that has a channel-taking sibling, like Info next to InfoTo.</summary>
    internal static bool HasChannelSibling(string methodName)
    {
        return methodName switch
        {
            "Trace" or "Debug" or "Info" or "Warn" or "Error" or "Fatal" => true,
            _ => false,
        };
    }

    /// <summary>
    /// True when a literal reads as a channel rather than a message: dotted, unspaced, and with
    /// no placeholder. A real template almost always has a space or a brace.
    /// </summary>
    internal static bool LooksLikeChannel(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        if (value[0] == '.' || value[value.Length - 1] == '.') return false;

        bool dotted = false;
        foreach (char c in value)
        {
            if (c == '.')
            {
                dotted = true;
                continue;
            }
            if (!char.IsLetterOrDigit(c) && c != '_') return false;
        }
        return dotted;
    }
}
