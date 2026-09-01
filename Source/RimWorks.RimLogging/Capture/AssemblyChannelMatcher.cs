using System.Collections.Generic;
using System.Reflection;

namespace RimWorks.RimLogging.Capture;

/// <summary>
/// Pure assembly-to-channel matcher. Given the running mods (packageId plus their
/// loaded assemblies) it returns <c>"Mod.&lt;sanitized-packageId&gt;"</c> for the mod
/// that owns the target assembly, or <see cref="AssemblyChannelCache.Unknown"/>
/// when no mod claims it. Verse-free so it can be unit-tested directly.
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
