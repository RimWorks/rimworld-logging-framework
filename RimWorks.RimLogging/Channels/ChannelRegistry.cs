using System;
using System.Collections.Generic;
using System.Linq;

namespace RimWorks.RimLogging.Channels;

/// <summary>In-memory lookup of <see cref="ChannelDef"/>s loaded from the DefDatabase, with prefix-based channel name resolution.</summary>
public static class ChannelRegistry
{
    private static Dictionary<string, ChannelDef>? _byName;

    internal static void Boot()
    {
        Dictionary<string, ChannelDef> table = new Dictionary<string, ChannelDef>(StringComparer.Ordinal);
        foreach (ChannelDef d in Verse.DefDatabase<ChannelDef>.AllDefs)
            table[d.defName] = d;
        _byName = table;
    }

    /// <summary>Resolves the owning <see cref="ChannelDef"/> for a channel name by prefix match, or <c>null</c> if none matches or the registry is not booted.</summary>
    /// <param name="channelName">The channel name to resolve.</param>
    /// <returns>The matching <see cref="ChannelDef"/>, or <c>null</c>.</returns>
    public static ChannelDef? TryResolve(string channelName)
    {
        if (_byName == null) return null;
        string? key = ChannelResolution.ResolveOwnerKey(channelName, _byName.Keys);
        return key != null && _byName.TryGetValue(key, out ChannelDef? def) ? def : null;
    }

    /// <summary>All currently registered <see cref="ChannelDef"/>s, or an empty list if the registry is not booted.</summary>
    public static IReadOnlyList<ChannelDef> GetAllRegisteredDefs() =>
        _byName == null ? [] : _byName.Values.ToList();
}
