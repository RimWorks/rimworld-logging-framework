namespace RimWorks.RimLogging.Bootstrap;

/// <summary>
/// Second-phase bootstrap that runs once defs are loaded. Core logging and the Verse.Log hijack
/// are already live from <see cref="Settings.LoggingMod"/>'s constructor; this only wires the
/// def-backed configuration (channels and sinks) that cannot be read at mod-construction time,
/// plus the log viewer's in-memory sink.
/// Entries captured before the sinks register are replayed to them via the history buffer.
/// </summary>
[Verse.StaticConstructorOnStartup]
internal static class StaticInit
{
    static StaticInit()
    {
        try
        {
            Channels.ChannelRegistry.Boot();
            Sinks.SinkLoader.LoadDefaults();
            Viewer.LogViewerBoot.Init();
        }
        catch (System.Exception ex)
        {
            Verse.Log.Error("[RimLogging] def bootstrap failed: " + ex);
        }
    }
}
