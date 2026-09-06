using System;

namespace RimWorks.RimLogging.Viewer;

/// <summary>
/// Builds the channel text shown in the entry list. A packageId channel says little at a glance,
/// so a mod entry shows the mod's own name instead.
/// </summary>
public static class EntryLabel
{
    private const string ModChannelPrefix = "Mod.";

    private const string ModLabelPrefix = "Mod: ";

    /// <summary>
    /// The channel as the list should show it. Falls back to the raw channel whenever the mod
    /// name is unknown, so a row never loses the only identifier it had.
    /// </summary>
    public static string Channel(string? channel, string? modName)
    {
        if (string.IsNullOrEmpty(channel)) return string.Empty;
        if (string.IsNullOrEmpty(modName)) return channel!;
        if (!channel!.StartsWith(ModChannelPrefix, StringComparison.Ordinal)) return channel;

        return ModLabelPrefix + modName;
    }
}
