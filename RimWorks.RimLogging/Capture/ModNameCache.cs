using System;
using System.Collections.Generic;
using System.Reflection;

namespace RimWorks.RimLogging.Capture;

/// <summary>
/// Caches the assembly-simple-name to mod-name (About.xml <c>&lt;name&gt;</c>) map. Bootstrap
/// installs the Verse-aware <see cref="Provider"/>; the map is built once on first non-empty
/// result. The emit pipeline uses it to attribute log entries to their originating mod without
/// a per-call stack walk.
/// </summary>
internal static class ModNameCache
{
    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();

    /// <summary>
    /// Provider hook for the asm-name to mod-display-name map. Bootstrap sets the Verse-aware
    /// implementation; tests set it directly. When <c>null</c>, the cache yields an empty map.
    /// </summary>
    internal static Func<IReadOnlyDictionary<string, string>>? Provider { get; set; }

    /// <summary>
    /// Provider hook for the asm-name to mod-folder-name map (the directory name under <c>/Mods/</c>).
    /// Folder name is preferred over the mod display name when rendering normalised source paths
    /// because it is stable and matches what shows up in the file system.
    /// </summary>
    internal static Func<IReadOnlyDictionary<string, string>>? FolderProvider { get; set; }

    /// <summary>
    /// Diagnostic hook invoked when <see cref="Provider"/> or <see cref="FolderProvider"/> throws.
    /// Bootstrap wires this to Verse.Log.Warning so a broken provider surfaces a message instead
    /// of silently degrading to an empty map. Verse-free so the cache stays unit-testable. Mirrors
    /// <see cref="AssemblyChannelCache.OnResolverError"/> so both static resolver caches surface
    /// failures the same way.
    /// </summary>
    internal static Action<Exception>? OnProviderError { get; set; }

    private static IReadOnlyDictionary<string, string>? _cached;
    private static IReadOnlyDictionary<string, string>? _cachedFolders;

    /// <summary>
    /// Returns the assembly-name to mod-name map, building it via <see cref="Provider"/> and
    /// caching the first non-empty result. Empty results are not cached, so a provider invoked
    /// before mods finish loading is retried on the next call.
    /// </summary>
    internal static IReadOnlyDictionary<string, string> Map()
    {
        if (_cached != null) return _cached;
        if (Provider == null) return Empty;
        IReadOnlyDictionary<string, string> map;
        try { map = Provider() ?? Empty; }
        catch (Exception ex)
        {
            OnProviderError?.Invoke(ex);
            return Empty;
        }
        if (map.Count > 0) _cached = map;
        return map;
    }

    /// <summary>
    /// Returns the assembly-name to mod-folder-name map. Same caching semantics as
    /// <see cref="Map"/>: the first non-empty result is cached; empty results are retried.
    /// </summary>
    internal static IReadOnlyDictionary<string, string> FolderMap()
    {
        if (_cachedFolders != null) return _cachedFolders;
        if (FolderProvider == null) return Empty;
        IReadOnlyDictionary<string, string> map;
        try { map = FolderProvider() ?? Empty; }
        catch (Exception ex)
        {
            OnProviderError?.Invoke(ex);
            return Empty;
        }
        if (map.Count > 0) _cachedFolders = map;
        return map;
    }

    /// <summary>Returns the mod name for the given assembly, or <c>null</c> when unknown.</summary>
    internal static string? ForAssembly(Assembly asm)
    {
        string? name = asm.GetName().Name;
        if (name == null) return null;
        return Map().TryGetValue(name, out string? mod) ? mod : null;
    }

    /// <summary>
    /// Returns the mod folder name (directory under <c>/Mods/</c>) for the given assembly,
    /// or <c>null</c> when unknown.
    /// </summary>
    internal static string? FolderForAssembly(Assembly asm)
    {
        string? name = asm.GetName().Name;
        if (name == null) return null;
        return FolderMap().TryGetValue(name, out string? folder) ? folder : null;
    }

    internal static void ClearForTests()
    {
        _cached = null;
        _cachedFolders = null;
        Provider = null;
        FolderProvider = null;
        OnProviderError = null;
    }
}
