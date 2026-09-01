using System;
using System.Collections.Generic;

namespace RimWorks.RimLogging.Channels;

/// <summary>Pure dotted-name resolution: exact match, then prefix walk, then "default" only if it is itself a registered key.</summary>
public static class ChannelResolution
{
    /// <summary>
    /// Resolves a channel to a registered key: exact match, then each dotted ancestor.
    /// Falls back to <c>"default"</c> only if that is itself registered, else null.
    /// </summary>
    public static string? ResolveOwnerKey(string channelName, IEnumerable<string> registeredKeys)
    {
        if (string.IsNullOrEmpty(channelName)) return null;
        HashSet<string> set = new HashSet<string>(registeredKeys, StringComparer.Ordinal);
        if (set.Contains(channelName)) return channelName;
        string cur = channelName;
        int dot;
        while ((dot = cur.LastIndexOf('.')) > 0)
        {
            cur = cur.Substring(0, dot);
            if (set.Contains(cur)) return cur;
        }
        return set.Contains("default") ? "default" : null;
    }
}
