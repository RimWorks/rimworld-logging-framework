using Concord;
using LudeonTK;
using RimWorld;
using Verse;

namespace RimWorks.RimLogging.Patches.Concord;

internal static class DevAttributionProbeTarget
{
    // the log call sits inside the patched method on purpose: capture reads the live stack, so
    // attribution only has something to name while this frame is still on it
    public static void Run()
    {
        Log.ErrorTo("probe", "concord attribution probe: this entry should name DevAttributionProbePatch");
    }
}

[Patch(typeof(DevAttributionProbeTarget))]
internal static class DevAttributionProbePatch
{
    [Inject(At.Head, nameof(DevAttributionProbeTarget.Run))]
    private static Control Head() => Control.Continue;
}

/// <summary>Raises an error from a Concord-patched method so patch attribution can be checked in game.</summary>
internal static class DevAttributionProbeAction
{
    [DebugAction("RimLogging", "Probe patch attribution", allowedGameStates = AllowedGameStates.Invalid)]
    private static void Probe()
    {
        DevAttributionProbeTarget.Run();
        Messages.Message("CRL_Dev_ProbeFired".Translate(), MessageTypeDefOf.TaskCompletion, false);
    }
}
