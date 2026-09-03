using System.Collections.Generic;
using Verse;

namespace RimWorks.RimLogging.Viewer;

/// <summary>Feeds the running mod list into <see cref="ChannelClassifier"/>. Holds the only Verse dependency in the channel-path logic.</summary>
internal static class ChannelClassifierBootstrap
{
    public static void Install()
    {
        ChannelClassifier.UseModTable(LoadRunningMods);
    }

    private static void LoadRunningMods()
    {
        List<ModContentPack> running = LoadedModManager.RunningModsListForReading;
        if (running == null)
        {
            return;
        }

        for (int i = 0; i < running.Count; i++)
        {
            ModContentPack mcp = running[i];
            if (mcp == null)
            {
                continue;
            }
            ChannelClassifier.AddMod(mcp.PackageId, mcp.PackageIdPlayerFacing);
        }
    }
}
