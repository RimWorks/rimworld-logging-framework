using RimWorks.RimLogging.Capture;

namespace RimWorks.RimLogging.Hijack;

/// <summary>
/// Imports messages RimWorld buffered before our hijack went live. Engine, Unity and
/// early mod-load logging all land in <see cref="Verse.Log.Messages"/> from process start,
/// but our Harmony prefix only captures from the moment it is applied. Everything before
/// that point would otherwise be missing from our sinks while still present in the vanilla
/// debug window. Draining the buffer once, immediately before patching, recovers it.
/// </summary>
internal static class VerseLogBackfill
{
    /// <summary>
    /// Emits every entry currently in <see cref="Verse.Log.Messages"/> through the pipeline
    /// on the <c>Vanilla</c> channel. Must be called before the Harmony prefix is applied so
    /// the buffered set and the live-captured set do not overlap.
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
