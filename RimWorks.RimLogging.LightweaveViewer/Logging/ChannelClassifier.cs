using System;
using System.Collections.Generic;
using RimWorks.RimLogging.Capture;
using Verse;

namespace RimWorks.RimLogging.LightweaveViewer;

internal static class ChannelClassifier {
    private const string ModGroup = "Mod";
    private const string VanillaGroup = "Vanilla";
    private const string ModChannelPrefix = "Mod.";

    private static Dictionary<string, string[]>? packageIdToPath;
    private static Dictionary<string, string[]>? fullFacingToPath;
    private static Dictionary<string, string[]>? moduleNameToPath;

    public static string ModGroupId => ModGroup;
    public static string VanillaGroupId => VanillaGroup;

    public static void EnsureBuilt() {
        if (packageIdToPath != null) {
            return;
        }

        packageIdToPath = new Dictionary<string, string[]>(StringComparer.Ordinal);
        fullFacingToPath = new Dictionary<string, string[]>(StringComparer.Ordinal);
        moduleNameToPath = new Dictionary<string, string[]>(StringComparer.Ordinal);

        List<ModContentPack> running = LoadedModManager.RunningModsListForReading;
        if (running == null) {
            return;
        }

        for (int i = 0; i < running.Count; i++) {
            ModContentPack mcp = running[i];
            if (mcp == null || string.IsNullOrEmpty(mcp.PackageId)) {
                continue;
            }

            string facing = string.IsNullOrEmpty(mcp.PackageIdPlayerFacing) ? mcp.PackageId : mcp.PackageIdPlayerFacing;
            string[] modPath = facing.Split('.');

            packageIdToPath[ModChannelPrefix + PackageIdSanitizer.ToChannelSegment(mcp.PackageId)] = modPath;
            fullFacingToPath[facing.ToLowerInvariant()] = modPath;
            if (modPath.Length > 0) {
                moduleNameToPath[modPath[modPath.Length - 1].ToLowerInvariant()] = modPath;
            }
        }
    }

    public static string[] PathFor(string? channel) {
        EnsureBuilt();

        if (string.IsNullOrEmpty(channel) || channel == "(root)" || channel == VanillaGroup) {
            return new[] { VanillaGroup };
        }

        if (channel!.StartsWith(ModChannelPrefix, StringComparison.Ordinal)) {
            if (packageIdToPath != null && packageIdToPath.TryGetValue(channel, out string[] mapped)) {
                return Prepend(ModGroup, mapped);
            }
            return Prepend(ModGroup, channel.Substring(ModChannelPrefix.Length).Split('.'));
        }

        string[] segs = channel.Split('.');
        return ResolveNative(segs);
    }

    private static string[] ResolveNative(string[] segs) {
        if (fullFacingToPath != null && segs.Length >= 2) {
            string twoKey = (segs[0] + "." + segs[1]).ToLowerInvariant();
            if (fullFacingToPath.TryGetValue(twoKey, out string[] modPath2)) {
                return BuildNative(modPath2, segs, 2);
            }
        }

        if (moduleNameToPath != null && segs.Length >= 1 && moduleNameToPath.TryGetValue(segs[0].ToLowerInvariant(), out string[] modPath1)) {
            return BuildNative(modPath1, segs, 1);
        }

        return Prepend(ModGroup, segs);
    }

    public static string JoinPath(string[] path) {
        return string.Join("/", path);
    }

    private static string[] BuildNative(string[] modPath, string[] segs, int consumed) {
        int remaining = segs.Length - consumed;
        string[] result = new string[1 + modPath.Length + remaining];
        result[0] = ModGroup;
        Array.Copy(modPath, 0, result, 1, modPath.Length);
        Array.Copy(segs, consumed, result, 1 + modPath.Length, remaining);
        return result;
    }

    private static string[] Prepend(string head, string[] tail) {
        string[] result = new string[tail.Length + 1];
        result[0] = head;
        Array.Copy(tail, 0, result, 1, tail.Length);
        return result;
    }
}
