using System.Collections.Generic;
using System.Reflection;

namespace RimWorks.RimLogging.Capture;

/// <summary>
/// Maps an assembly to <c>"Mod.&lt;sanitized-packageId&gt;"</c> for whichever loaded mod owns it,
/// or <see cref="AssemblyChannelCache.Unknown"/>. Verse-free so it tests directly.
/// </summary>
internal static class AssemblyChannelMatcher
{
    internal static string Match(
        Assembly target,
        IEnumerable<(string PackageId, IReadOnlyList<Assembly> Assemblies)> mods)
    {
        foreach ((string packageId, IReadOnlyList<Assembly> assemblies) in mods)
        {
            for (int i = 0; i < assemblies.Count; i++)
            {
                if (ReferenceEquals(assemblies[i], target))
                    return "Mod." + PackageIdSanitizer.ToChannelSegment(packageId);
            }
        }
        return AssemblyChannelCache.Unknown;
    }
}
