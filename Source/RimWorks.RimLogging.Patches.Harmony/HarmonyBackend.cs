using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using LudeonTK;
using RimWorks.RimLogging.Hijack;
using RimWorks.RimLogging.Patching;
using RimWorks.RimLogging.Viewer;
using Verse;

namespace RimWorks.RimLogging.Patches.Harmony;

/// <summary>RimLogging's hooks as Harmony patches. Ranked below Concord.</summary>
public sealed class HarmonyBackend : IPatchBackend, IPatchAttributionSource
{
    private const string HarmonyId = "rimworks.rimlogging";

    /// <inheritdoc/>
    public string Name => "Harmony";

    /// <inheritdoc/>
    public int Priority => BackendPriority.Harmony;

    /// <inheritdoc/>
    public void Apply()
    {
        HarmonyLib.Harmony harmony = new HarmonyLib.Harmony(HarmonyId);
        HarmonyMethod Prefix(string name) => new HarmonyMethod(typeof(HarmonyBackend), name);

        harmony.Patch(AccessTools.Method(typeof(Verse.Log), nameof(Verse.Log.Error), [typeof(string)]),
            prefix: Prefix(nameof(ErrorPrefix)));
        harmony.Patch(AccessTools.Method(typeof(Verse.Log), nameof(Verse.Log.Warning), [typeof(string)]),
            prefix: Prefix(nameof(WarningPrefix)));
        harmony.Patch(AccessTools.Method(typeof(Verse.Log), nameof(Verse.Log.Message), [typeof(string)]),
            prefix: Prefix(nameof(MessagePrefix)));
        harmony.Patch(AccessTools.Method(typeof(DebugWindowsOpener), "ToggleLogWindow"),
            prefix: Prefix(nameof(ToggleLogWindowPrefix)));
        harmony.Patch(AccessTools.Method(typeof(UIRoot), "CheckOpenLogWindow"),
            prefix: Prefix(nameof(CheckOpenLogWindowPrefix)));
    }

    // Concord's bridge into an already-Harmony-patched target, not a mod; never surfaced as one
    private const string ConcordBridgeOwnerId = "concord.bridge";

    // keyed by the resolved original, not the frame: repeat frames for the same patched
    // method are common (a hot error loop), copy-on-write like ChannelRegistry.SettingsFor
    private static volatile Dictionary<MethodBase, IReadOnlyList<string>> _ownerCache =
        new Dictionary<MethodBase, IReadOnlyList<string>>();
    private static readonly object CacheLock = new object();

    /// <inheritdoc/>
    public IReadOnlyList<string>? OwnersFor(StackFrame frame)
    {
        // key on the frame's own method first. resolving through Harmony costs more than the
        // dictionary hit, and most frames are unpatched and repeat constantly across captures,
        // so paying the resolve every time made attribution 7% of a capture instead of 1%.
        MethodBase? raw = frame.GetMethod();
        Dictionary<MethodBase, IReadOnlyList<string>> snapshot = _ownerCache;
        if (raw != null && snapshot.TryGetValue(raw, out IReadOnlyList<string>? rawHit)) return rawHit;

        // a null raw method is the Mono replacement-frame case, where only the frame carries
        // enough for Harmony's native-address fallback
        MethodBase? original = HarmonyLib.Harmony.GetOriginalMethodFromStackframe(frame);
        if (original == null) return Array.Empty<string>();
        if (snapshot.TryGetValue(original, out IReadOnlyList<string>? hit))
        {
            if (raw != null) Remember(raw, hit);
            return hit;
        }

        global::HarmonyLib.Patches? info = HarmonyLib.Harmony.GetPatchInfo(original);
        List<string> owners = new List<string>(info?.Owners.Count ?? 0);
        if (info != null)
        {
            foreach (string owner in info.Owners)
            {
                if (owner != ConcordBridgeOwnerId) owners.Add(owner);
            }
        }

        Remember(original, owners);
        if (raw != null && !ReferenceEquals(raw, original)) Remember(raw, owners);
        return owners;
    }

    private static void Remember(MethodBase key, IReadOnlyList<string> owners)
    {
        lock (CacheLock)
        {
            _ownerCache = new Dictionary<MethodBase, IReadOnlyList<string>>(_ownerCache) { [key] = owners };
        }
    }

    // a Harmony prefix returning false skips the original, which is what the hooks already mean
    private static bool ErrorPrefix(string text) => LogHooks.OnError(text);

    private static bool WarningPrefix(string text) => LogHooks.OnWarning(text);

    private static bool MessagePrefix(string text) => LogHooks.OnMessage(text);

    private static bool ToggleLogWindowPrefix() => ViewerHooks.OnToggleLogWindow();

    private static bool CheckOpenLogWindowPrefix() => ViewerHooks.OnCheckOpenLogWindow();
}
