using System.Collections.Generic;
using System.Reflection;
using RimWorks.RimLogging.Capture;

namespace RimWorks.RimLogging.Hijack;

internal static class AssemblyChannelResolver
{
    /// <summary>
    /// Verse-aware resolver wired into by . Projects into packageId/assembly pairs and delegates the actual
    /// matching to .
    /// </summary>
    internal static string Resolve(Assembly asm)
    {
        return AssemblyChannelMatcher.Match(asm, RunningMods());
    }

    private static IEnumerable<(string, IReadOnlyList<Assembly>)> RunningMods()
    {
        foreach (Verse.ModContentPack mcp in Verse.LoadedModManager.RunningMods)
            yield return (mcp.PackageId, mcp.assemblies.loadedAssemblies);
    }
}
