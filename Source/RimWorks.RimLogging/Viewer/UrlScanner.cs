using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RimWorks.RimLogging.Viewer;

/// <summary>Pulls http and https links out of an entry, so they can be copied without dragging.</summary>
public static class UrlScanner
{
    /// <summary>More than this in one entry is noise, and the detail pane has finite room.</summary>
    public const int DefaultMax = 5;

    private static readonly Regex Pattern = new Regex(
        @"\bhttps?://[^\s""'<>]+", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));

    // a url rarely ends in punctuation, but a sentence containing one usually does
    private static readonly char[] TrailingJunk = { '.', ',', ';', ':', '!', '?', ')', ']', '}', '>', '"', '\'' };

    /// <summary>Distinct links in the order they appear, capped so one entry cannot flood the pane.</summary>
    public static IReadOnlyList<string> Find(string? text, int max = DefaultMax)
    {
        if (string.IsNullOrEmpty(text) || max <= 0) return Array.Empty<string>();

        List<string> found = new List<string>();
        foreach (Match match in Pattern.Matches(text))
        {
            string url = Trim(match.Value);
            if (url.Length == 0 || found.Contains(url)) continue;

            found.Add(url);
            if (found.Count == max) break;
        }
        return found;
    }

    /// <summary>Every link across an entry's message and stack trace.</summary>
    public static IReadOnlyList<string> ForEntry(LogEntry entry, string? trace, int max = DefaultMax)
    {
        if (string.IsNullOrEmpty(trace)) return Find(entry.RenderedMessage, max);
        return Find(entry.RenderedMessage + "\n" + trace, max);
    }

    // balanced parens are not tracked, so a url that genuinely ends in ')' loses it
    private static string Trim(string url) => url.TrimEnd(TrailingJunk);
}
