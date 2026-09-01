using RimWorks.RimLogging.Capture;
using RimWorks.RimLogging.Pipeline;

namespace RimWorks.RimLogging.Hijack;

/// <summary>Verse.Log capture, stated once so each patch backend only has to call it.</summary>
internal static class LogHooks
{
    /// <summary>Returns true to run vanilla's body, false to replace it.</summary>
    internal static bool OnError(string text) => Capture(LogLevel.Error, text);

    /// <inheritdoc cref="OnError"/>
    internal static bool OnWarning(string text) => Capture(LogLevel.Warn, text);

    /// <inheritdoc cref="OnError"/>
    internal static bool OnMessage(string text) => Capture(LogLevel.Info, text);

    private static bool Capture(LogLevel level, string text)
    {
        if (ReentryGuard.IsInsideSink)
        {
            return true;
        }
        (string channel, string? mod) = VerseLogPatchHelpers.ResolveCaller();
        Log.EmitCaptured(level, channel, text, mod: mod);
        return false;
    }
}
