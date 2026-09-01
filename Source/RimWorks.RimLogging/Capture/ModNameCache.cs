using System;
using System.Collections.Generic;
using System.Reflection;

namespace RimWorks.RimLogging.Capture;

/// <summary>
/// Caches assembly name to mod name, built once from <see cref="Provider"/>. Lets the emit
/// pipeline attribute entries to a mod without walking the stack each call.
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
    /// Supplies assembly name to mod folder. The folder beats the display name for paths,
    /// since it is stable and matches the file system.
    /// </summary>
    internal static Func<IReadOnlyDictionary<string, string>>? FolderProvider { get; set; }

    /// <summary>
    /// Called when a provider throws, so a broken one warns instead of silently emptying the map.
    /// Mirrors <see cref="AssemblyChannelCache.OnResolverError"/>.
    /// </summary>
    internal static Action<Exception>? OnProviderError { get; set; }

    private static IReadOnlyDictionary<string, string>? _cached;
    private static IReadOnlyDictionary<string, string>? _cachedFolders;

    /// <summary>
    /// Builds the map via <see cref="Provider"/>, caching the first non-empty result. An empty
    /// one is not cached, so a call made before mods finish loading is retried.
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
