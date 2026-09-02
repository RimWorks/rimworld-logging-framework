using System;

namespace RimWorks.RimLogging.Bundle;

/// <summary>The visibility values Docbin accepts, and which one a given API key allows.</summary>
internal static class DocbinVisibility
{
    /// <summary>Anyone can find the paste.</summary>
    internal const string Public = "public";

    /// <summary>Only someone holding the link can open the paste.</summary>
    internal const string Unlisted = "unlisted";

    /// <summary>Whether an API key is present, which is what makes the choice available.</summary>
    internal static bool CanChoose(string? apiKey) => !string.IsNullOrWhiteSpace(apiKey);

    /// <summary>
    /// Resolves the visibility actually sent. Anonymous pastes are always public because Docbin
    /// has nowhere to file them; a key makes unlisted the default and public opt-in.
    /// </summary>
    internal static string Effective(bool authed, string? chosen)
    {
        if (!authed) return Public;
        return string.Equals(chosen, Public, StringComparison.OrdinalIgnoreCase) ? Public : Unlisted;
    }
}
