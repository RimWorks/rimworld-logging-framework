using RimWorks.RimLogging.Capture;

namespace RimWorks.RimLogging.Hijack;

/// <summary>
/// Drains <see cref="Verse.Log.Messages"/> once before patching, so entries logged from
/// process start to hijack install are not missing from our sinks.
/// </summary>
internal static class VerseLogBackfill
{
    /// <summary>
    /// Emits every entry currently in  through the pipeline on the Vanilla channel. Must be called before the
    /// Harmony prefix is applied so the buffered set and the live-captured set do not overlap.
    /// </summary>
    internal static void Drain()
    {
        foreach (Verse.LogMessage message in Verse.Log.Messages)
        {
            LogLevel level = VerseLevelMapping.FromVerseMessageTypeId((int)message.type);
            Log.EmitCaptured(level, "Vanilla", message.text, message.StackTrace);
        }
    }
}
