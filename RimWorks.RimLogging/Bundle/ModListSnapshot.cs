namespace RimWorks.RimLogging.Bundle;

/// <summary>
/// Captures the currently loaded RimWorld mods into a list of <see cref="BundlePayload.ModInfo"/> for inclusion
/// in a bug-report bundle.
/// </summary>
public static class ModListSnapshot
{
    /// <summary>
    /// Snapshots the running mods, recording each one's name, package id, and manifest version. All captured
    /// mods are marked active since the snapshot reflects the live load order.
    /// </summary>
    /// <returns>The loaded mods as <see cref="BundlePayload.ModInfo"/> entries.</returns>
    public static System.Collections.Generic.List<BundlePayload.ModInfo> Capture()
    {
        System.Collections.Generic.List<BundlePayload.ModInfo> result = new System.Collections.Generic.List<BundlePayload.ModInfo>();
        foreach (Verse.ModContentPack mcp in Verse.LoadedModManager.RunningMods)
        {
            result.Add(new BundlePayload.ModInfo
            {
                Name = mcp.Name ?? "",
                PackageId = mcp.PackageId ?? "",
                Version = ManifestVersionReader.TryGetVersion(mcp.RootDir),
                Active = true,
            });
        }
        return result;
    }
}
