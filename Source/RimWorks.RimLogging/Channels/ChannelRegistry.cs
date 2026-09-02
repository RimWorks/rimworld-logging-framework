using System;
using System.Collections.Generic;
using System.Linq;

namespace RimWorks.RimLogging.Channels;

/// <summary>In-memory lookup of <see cref="ChannelDef"/>s loaded from the DefDatabase, with prefix-based channel name resolution.</summary>
public static class ChannelRegistry
{
    private static Dictionary<string, ChannelDef>? _byName;

    // resolving a channel walks its dot-segments and allocates a substring per level, which is
    // fine once and far too expensive per emit. copy-on-write so the read side never locks.
    private static volatile Dictionary<string, ChannelSettings> _settings =
        new Dictionary<string, ChannelSettings>(StringComparer.Ordinal);

    private static readonly object SettingsLock = new object();

    internal static void Boot()
    {
        Dictionary<string, ChannelDef> table = new Dictionary<string, ChannelDef>(StringComparer.Ordinal);
        foreach (ChannelDef d in Verse.DefDatabase<ChannelDef>.AllDefs)
            table[d.defName] = d;
        _byName = table;
        _settings = new Dictionary<string, ChannelSettings>(StringComparer.Ordinal);
        Logging.ChannelSettingsProvider = SettingsFor;
    }

    /// <summary>The overrides for a channel, memoised because this runs on every emit.</summary>
    internal static ChannelSettings SettingsFor(string channelName)
    {
        if (_settings.TryGetValue(channelName, out ChannelSettings hit)) return hit;

        ChannelDef? def = TryResolve(channelName);
        ChannelSettings resolved = def == null
            ? ChannelSettings.Inherit
            : new ChannelSettings(def.defaultLevel, def.captureStackAt, def.destinations, def.format);

        lock (SettingsLock)
        {
            _settings = new Dictionary<string, ChannelSettings>(_settings, StringComparer.Ordinal)
            {
                [channelName] = resolved,
            };
        }
        return resolved;
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
