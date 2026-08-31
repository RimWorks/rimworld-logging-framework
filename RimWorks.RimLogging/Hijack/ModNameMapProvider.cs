using System.Collections.Generic;
using System.Reflection;

namespace RimWorks.RimLogging.Hijack;

/// <summary>
/// Verse-aware provider that projects <see cref="Verse.LoadedModManager.RunningMods"/> into an
/// assembly-simple-name to mod-name (About.xml <c>&lt;name&gt;</c>) map. Wired into
/// <see cref="Capture.ModNameCache.Provider"/> by <see cref="HijackBootstrap.Install"/>.
/// </summary>
internal static class ModNameMapProvider
{
    private static readonly char[] PathSeparators = { '/', '\\' };

    internal static IReadOnlyDictionary<string, string> Build()
    {
        Dictionary<string, string> map = new Dictionary<string, string>();
        foreach (Verse.ModContentPack mcp in Verse.LoadedModManager.RunningMods)
        {
            foreach (Assembly asm in mcp.assemblies.loadedAssemblies)
            {
                string? name = asm.GetName().Name;
                if (name != null) map[name] = mcp.Name;
            }
        }
        return map;
    }

    /// <summary>
    /// Builds the asm-name to mod-folder-name map. The folder name is parsed from
    /// <see cref="Verse.ModContentPack.RootDir"/>, which is stable across loads and matches
    /// the directory the user actually sees under <c>/Mods/</c> -- preferable to the mod's
    /// human-display <c>Name</c> when normalising source-file paths.
    /// </summary>
    internal static IReadOnlyDictionary<string, string> BuildFolders()
    {
        Dictionary<string, string> map = new Dictionary<string, string>();
        foreach (Verse.ModContentPack mcp in Verse.LoadedModManager.RunningMods)
        {
            string? folder = ParseFolder(mcp.RootDir);
            if (folder == null) continue;
            foreach (Assembly asm in mcp.assemblies.loadedAssemblies)
            {
                string? name = asm.GetName().Name;
                if (name != null) map[name] = folder;
            }
        }
        return map;
    }

    private static string? ParseFolder(string? rootDir)
    {
        if (string.IsNullOrEmpty(rootDir)) return null;
        string trimmed = rootDir!.TrimEnd('/', '\\');
        if (trimmed.Length == 0) return null;
        int lastSep = trimmed.LastIndexOfAny(PathSeparators);
        return lastSep < 0 ? trimmed : trimmed.Substring(lastSep + 1);
    }
}
