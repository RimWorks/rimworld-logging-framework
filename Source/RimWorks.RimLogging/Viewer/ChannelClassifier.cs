using System;
using System.Collections.Generic;
using RimWorks.RimLogging.Capture;

namespace RimWorks.RimLogging.Viewer;

internal static class ChannelClassifier
{
    private const string ModGroup = "Mod";
    private const string VanillaGroup = "Vanilla";
    private const string ModChannelPrefix = "Mod.";

    private static Dictionary<string, string[]>? packageIdToPath;
    private static Dictionary<string, string[]>? fullFacingToPath;
    private static Dictionary<string, string[]>? moduleNameToPath;
    private static Dictionary<string, List<RootEntry>>? rootsByFirstSegment;

    private sealed class RootEntry
    {
        public RootEntry(string[] segments, string[] modPath)
        {
            Segments = segments;
            ModPath = modPath;
        }

        public string[] Segments { get; }
        public string[] ModPath { get; }
    }

    public static string ModGroupId => ModGroup;
    public static string VanillaGroupId => VanillaGroup;

    private static Action? modTableLoader;

    /// <summary>Installs the table filler. <c>ChannelClassifierBootstrap</c> owns the Verse dependency.</summary>
    internal static void UseModTable(Action? loader)
    {
        modTableLoader = loader;
    }

    public static void EnsureBuilt()
    {
        if (packageIdToPath != null)
        {
            return;
        }

        packageIdToPath = new Dictionary<string, string[]>(StringComparer.Ordinal);
        fullFacingToPath = new Dictionary<string, string[]>(StringComparer.Ordinal);
        moduleNameToPath = new Dictionary<string, string[]>(StringComparer.Ordinal);
        rootsByFirstSegment = new Dictionary<string, List<RootEntry>>(StringComparer.OrdinalIgnoreCase);

        modTableLoader?.Invoke();
    }

    /// <summary>Registers one running mod against the channel paths it owns.</summary>
    internal static void AddMod(string packageId, string playerFacingId)
    {
        if (string.IsNullOrEmpty(packageId))
        {
            return;
        }

        string facing = string.IsNullOrEmpty(playerFacingId) ? packageId : playerFacingId;
        string[] modPath = facing.Split('.');

        packageIdToPath![ModChannelPrefix + PackageIdSanitizer.ToChannelSegment(packageId)] = modPath;
        fullFacingToPath![facing.ToLowerInvariant()] = modPath;
        if (modPath.Length > 0)
        {
            moduleNameToPath![modPath[modPath.Length - 1].ToLowerInvariant()] = modPath;
        }
    }

    /// <summary>
    /// Registers one running mod under an explicit tree path. A native channel whose leading dotted segments equal one
    /// of <paramref name="channelRoots"/> resolves under <paramref name="modPath"/>, with the rest of its segments below.
    /// Roots match whole segments and ignore case, and the longest matching root wins.
    /// </summary>
    internal static void AddMod(string packageId, string[] modPath, string[] channelRoots)
    {
        if (string.IsNullOrEmpty(packageId) || modPath == null || modPath.Length == 0)
        {
            return;
        }

        packageIdToPath![ModChannelPrefix + PackageIdSanitizer.ToChannelSegment(packageId)] = modPath;

        if (channelRoots == null)
        {
            return;
        }

        foreach (string root in channelRoots)
        {
            if (string.IsNullOrEmpty(root))
            {
                continue;
            }

            string[] segments = root.Split('.');
            if (Array.IndexOf(segments, string.Empty) >= 0)
            {
                continue;
            }

            if (!rootsByFirstSegment!.TryGetValue(segments[0], out List<RootEntry>? bucket))
            {
                bucket = new List<RootEntry>();
                rootsByFirstSegment[segments[0]] = bucket;
            }

            int existing = bucket.FindIndex(e => e.Segments.Length == segments.Length && SegmentsStartWith(e.Segments, segments, segments.Length));
            RootEntry entry = new RootEntry(segments, modPath);
            if (existing >= 0)
            {
                bucket[existing] = entry;
            }
            else
            {
                bucket.Add(entry);
            }
        }
    }

    /// <summary>Drops the cached tables so the next <see cref="EnsureBuilt"/> rebuilds them.</summary>
    internal static void Reset()
    {
        packageIdToPath = null;
        fullFacingToPath = null;
        moduleNameToPath = null;
        rootsByFirstSegment = null;
    }

    public static string[] PathFor(string? channel)
    {
        EnsureBuilt();

        if (string.IsNullOrEmpty(channel) || channel == "(root)" || channel == VanillaGroup)
        {
            return new[] { VanillaGroup };
        }

        if (channel!.StartsWith(ModChannelPrefix, StringComparison.Ordinal))
        {
            if (packageIdToPath != null && packageIdToPath.TryGetValue(channel, out string[]? mapped))
            {
                return Prepend(ModGroup, mapped);
            }
            return Prepend(ModGroup, channel.Substring(ModChannelPrefix.Length).Split('.'));
        }

        string[] segs = channel.Split('.');
        return ResolveNative(segs);
    }

    private static string[] ResolveNative(string[] segs)
    {
        RootEntry? root = LongestRoot(segs);
        if (root != null)
        {
            return BuildNative(root.ModPath, segs, root.Segments.Length);
        }

        if (fullFacingToPath != null && segs.Length >= 2)
        {
            string twoKey = (segs[0] + "." + segs[1]).ToLowerInvariant();
            if (fullFacingToPath.TryGetValue(twoKey, out string[]? modPath2))
            {
                return BuildNative(modPath2, segs, 2);
            }
        }

        if (moduleNameToPath != null && segs.Length >= 1 && moduleNameToPath.TryGetValue(segs[0].ToLowerInvariant(), out string[]? modPath1))
        {
            return BuildNative(modPath1, segs, 1);
        }

        return Prepend(ModGroup, segs);
    }

    private static RootEntry? LongestRoot(string[] segs)
    {
        if (rootsByFirstSegment == null || segs.Length == 0 || !rootsByFirstSegment.TryGetValue(segs[0], out List<RootEntry>? bucket))
        {
            return null;
        }

        RootEntry? best = null;
        for (int i = 0; i < bucket.Count; i++)
        {
            RootEntry candidate = bucket[i];
            if ((best == null || candidate.Segments.Length > best.Segments.Length)
                && SegmentsStartWith(segs, candidate.Segments, candidate.Segments.Length))
            {
                best = candidate;
            }
        }

        return best;
    }

    private static bool SegmentsStartWith(string[] segs, string[] root, int rootLength)
    {
        if (segs.Length < rootLength)
        {
            return false;
        }

        for (int i = 0; i < rootLength; i++)
        {
            if (!string.Equals(segs[i], root[i], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    public static string JoinPath(string[] path)
    {
        return string.Join("/", path);
    }

    private static string[] BuildNative(string[] modPath, string[] segs, int consumed)
    {
        int remaining = segs.Length - consumed;
        string[] result = new string[1 + modPath.Length + remaining];
        result[0] = ModGroup;
        Array.Copy(modPath, 0, result, 1, modPath.Length);
        Array.Copy(segs, consumed, result, 1 + modPath.Length, remaining);
        return result;
    }

    private static string[] Prepend(string head, string[] tail)
    {
        string[] result = new string[tail.Length + 1];
        result[0] = head;
        Array.Copy(tail, 0, result, 1, tail.Length);
        return result;
    }
}
