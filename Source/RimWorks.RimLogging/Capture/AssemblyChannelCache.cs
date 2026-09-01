using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace RimWorks.RimLogging.Capture;

internal static class AssemblyChannelCache
{
    private static readonly ConcurrentDictionary<Assembly, string> _cache = new ConcurrentDictionary<Assembly, string>();

    internal const string Vanilla = "Vanilla";
    internal const string Unknown = "Mod.Unknown";

    /// <summary>
    /// Resolver hook. Bootstrap installs the Verse-aware implementation;
    /// tests may set this directly to assert behavior. When null, the cache
    /// returns <see cref="Unknown"/> for non-vanilla assemblies.
    /// </summary>
    internal static Func<Assembly, string>? ResolverHook { get; set; }

    /// <summary>
    /// Diagnostic hook invoked when <see cref="ResolverHook"/> throws. Bootstrap wires this to
    /// Verse.Log.Warning so a broken resolver surfaces a message instead of silently degrading to
    /// <see cref="Unknown"/>. Verse-free so the cache stays unit-testable.
    /// </summary>
    internal static Action<Assembly, Exception>? OnResolverError { get; set; }

    /// <summary>
    /// Returns the channel name for the given assembly, caching only real resolutions.
    /// The <see cref="Unknown"/> fallback returned before the <see cref="ResolverHook"/> is
    /// wired (or when it throws) is not cached, so an assembly first seen during early boot is
    /// re-resolved once the hook is installed instead of being pinned to <see cref="Unknown"/>.
    /// </summary>
    internal static string Resolve(Assembly asm)
    {
        if (_cache.TryGetValue(asm, out string? cached)) return cached;

        string asmName = asm.GetName().Name ?? "Unknown";
        if (IsVanillaAssembly(asmName)) return _cache.GetOrAdd(asm, Vanilla);
        if (ResolverHook == null) return Unknown;

        try { return _cache.GetOrAdd(asm, ResolverHook(asm)); }
        catch (Exception ex)
        {
            OnResolverError?.Invoke(asm, ex);
            return Unknown;
        }
    }

    internal static bool IsVanillaAssembly(string name)
    {
        switch (name)
        {
            case "Assembly-CSharp":
            case "Assembly-CSharp-firstpass":
            case "UnityEngine":
            case "UnityEngine.CoreModule":
            case "Verse":
                return true;
            default:
                return false;
        }
    }

    internal static int CacheSize => _cache.Count;

    internal static void ClearForTests() => _cache.Clear();
}
