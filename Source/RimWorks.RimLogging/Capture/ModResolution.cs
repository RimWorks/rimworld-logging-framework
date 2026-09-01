using System;
using System.Collections.Generic;

namespace RimWorks.RimLogging.Capture;

/// <summary>
/// Pure resolver that shortens a raw source-file path and attributes it to its mod. The path
/// is trimmed to everything after the LAST segment whose name matches a known assembly simple
/// name; that segment's mod name (the About.xml <c>&lt;name&gt;</c>) becomes the mod label. The
/// mod folder itself is dropped because the channel already identifies the mod. When no segment
/// matches a known assembly, falls back to <see cref="StackWalker.NormalizePath"/> with no mod.
/// </summary>
internal static class ModResolution
{
    private static readonly char[] Separators = { '/', '\\' };

    /// <summary>
    /// Resolves a compact source path and originating mod from a raw file path.
    /// </summary>
    /// <param name="file">Raw file path (e.g. from <c>[CallerFilePath]</c>).</param>
    /// <param name="assemblyToMod">Map of assembly simple name to mod name.</param>
    /// <returns>The mod-relative path (without <c>.cs</c>) and the mod name, or the normalized
    /// path and <c>null</c> when no path segment matches a known assembly.</returns>
    internal static (string Path, string? Mod) ResolveFromPath(
        string file, IReadOnlyDictionary<string, string> assemblyToMod)
    {
        if (string.IsNullOrEmpty(file) || assemblyToMod == null || assemblyToMod.Count == 0)
            return (StackWalker.NormalizePath(file ?? string.Empty), null);

        string[] segments = file.Split(Separators);
        int match = -1;
        for (int i = 0; i < segments.Length; i++)
        {
            if (assemblyToMod.ContainsKey(segments[i])) match = i;
        }
        if (match < 0)
            return (StackWalker.NormalizePath(file), null);

        string tail = string.Join("/", segments, match + 1, segments.Length - match - 1);
        if (tail.EndsWith(".cs", StringComparison.Ordinal))
            tail = tail.Substring(0, tail.Length - 3);
        return (tail, assemblyToMod[segments[match]]);
    }
}
