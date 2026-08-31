using System.Diagnostics.CodeAnalysis;
using Concord;
using RimWorks.RimLogging.Capture;
using RimWorks.RimLogging.Pipeline;

namespace RimWorks.RimLogging.Hijack;

[Patch(typeof(Verse.Log))]
internal static class VerseLog_Warning_Patch
{
    [Inject(At.Head, nameof(Verse.Log.Warning), parameterTypes: [typeof(string)])]
    [SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed",
        Justification = "Invoked by Concord via the woven wrapper")]
    static Control Prefix(string text)
    {
        if (ReentryGuard.IsInsideSink) return Control.Continue;
        (string channel, string? mod) = VerseLogPatchHelpers.ResolveCaller();
        Log.EmitCaptured(LogLevel.Warn, channel, text, mod: mod);
        return Control.Cancel;
    }
}
