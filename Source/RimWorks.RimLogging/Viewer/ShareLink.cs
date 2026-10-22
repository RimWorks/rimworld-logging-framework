using System.Collections.Generic;

namespace RimWorks.RimLogging.Viewer;

/// <summary>
/// Pins a share URL to one entry. The viewer uploads the snapshot it is showing and the bundle
/// keeps that order, so an entry's index in the snapshot is the line the website opens on.
/// </summary>
public static class ShareLink
{
    /// <summary>The entry's index in the snapshot, or <c>-1</c> when the snapshot does not hold it.</summary>
    /// <param name="entries">The snapshot the bundle was built from.</param>
    /// <param name="entry">The entry to find, matched by reference.</param>
    public static int IndexOf(IReadOnlyList<LogEntry> entries, LogEntry? entry)
    {
        if (entry == null) return -1;
        for (int i = 0; i < entries.Count; i++)
        {
            if (ReferenceEquals(entries[i], entry)) return i;
        }
        return -1;
    }

    /// <summary>Appends the line parameter, picking <c>?</c> or <c>&amp;</c> from the URL's own query.</summary>
    /// <param name="url">The viewer URL the upload answered with.</param>
    /// <param name="index">The entry's index in the uploaded snapshot, or <c>-1</c> to leave the URL alone.</param>
    public static string WithLine(string url, int index)
    {
        if (index < 0 || string.IsNullOrEmpty(url)) return url;
        char separator = url.IndexOf('?') >= 0 ? '&' : '?';
        return url + separator + "line=" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
