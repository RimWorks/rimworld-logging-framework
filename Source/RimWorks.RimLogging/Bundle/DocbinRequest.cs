using System;

namespace RimWorks.RimLogging.Bundle;

/// <summary>Builds the Docbin paste URL and byte budget. Pure so it tests without a network call.</summary>
internal static class DocbinRequest
{
    /// <summary>Docbin's anonymous paste ceiling, minus room for headers.</summary>
    internal const int AnonMaxBytes = 1048576 - 4096;

    /// <summary>Docbin's authenticated paste ceiling, minus room for headers.</summary>
    internal const int AuthedMaxBytes = 5242880 - 4096;

    /// <summary>Returns the byte budget Docbin accepts for the given auth state.</summary>
    internal static int MaxBytes(bool authed) => authed ? AuthedMaxBytes : AnonMaxBytes;

    /// <summary>
    /// Returns the paste endpoint for the given auth state. Authenticated pastes carry a
    /// visibility and a name; anonymous ones take neither.
    /// </summary>
    internal static string Url(string baseUrl, bool authed, string? visibility, string? name)
    {
        string root = (baseUrl ?? string.Empty).TrimEnd('/');
        if (!authed) return root + "/api/docs/paste/anon?type=text&language=rimworld";

        string vis = string.IsNullOrWhiteSpace(visibility) ? "unlisted" : visibility!;
        return root + "/api/docs/paste?type=text&language=rimworld"
             + "&visibility=" + Uri.EscapeDataString(vis)
             + "&name=" + Uri.EscapeDataString(name ?? string.Empty);
    }

    /// <summary>Returns the default paste name for a bundle captured at the given moment.</summary>
    internal static string NameFor(DateTime utcNow) => "rimlogging-" + utcNow.ToString("yyyyMMdd-HHmmss");
}
