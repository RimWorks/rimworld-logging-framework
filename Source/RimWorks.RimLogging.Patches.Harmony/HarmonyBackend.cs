using HarmonyLib;
using LudeonTK;
using RimWorks.RimLogging.Hijack;
using RimWorks.RimLogging.Patching;
using RimWorks.RimLogging.Viewer;
using Verse;

namespace RimWorks.RimLogging.Patches.Harmony;

/// <summary>RimLogging's hooks as Harmony patches. Ranked below Concord.</summary>
public sealed class HarmonyBackend : IPatchBackend
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
        harmony.Patch(AccessTools.Method(typeof(Verse.Log), nameof(Verse.Log.TryOpenLogWindow)),
            prefix: Prefix(nameof(TryOpenLogWindowPrefix)));
        harmony.Patch(AccessTools.Method(typeof(DebugWindowsOpener), "ToggleLogWindow"),
            prefix: Prefix(nameof(ToggleLogWindowPrefix)));
        harmony.Patch(AccessTools.Method(typeof(UIRoot), "CheckOpenLogWindow"),
            prefix: Prefix(nameof(CheckOpenLogWindowPrefix)));
    }

    // a Harmony prefix returning false skips the original, which is what the hooks already mean
    private static bool ErrorPrefix(string text) => LogHooks.OnError(text);

    private static bool WarningPrefix(string text) => LogHooks.OnWarning(text);

    private static bool MessagePrefix(string text) => LogHooks.OnMessage(text);

    private static bool TryOpenLogWindowPrefix() => ViewerHooks.OnTryOpenLogWindow();

    private static bool ToggleLogWindowPrefix() => ViewerHooks.OnToggleLogWindow();

    private static bool CheckOpenLogWindowPrefix() => ViewerHooks.OnCheckOpenLogWindow();
}
