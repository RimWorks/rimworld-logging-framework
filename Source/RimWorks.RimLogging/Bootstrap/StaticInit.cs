namespace RimWorks.RimLogging.Bootstrap;

/// <summary>
/// Second-phase bootstrap, once defs exist. Wires the def-backed channels and sinks that
/// could not be read at mod-construction time. Earlier entries replay from the history buffer.
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
            Logging.TickProvider = CurrentTick;
        }
        catch (System.Exception ex)
        {
            PanicLog.Write("[RimLogging] def bootstrap failed: " + ex);
        }
    }

    // Find.TickManager is Current.Game.tickManager, and Current.Game is null until a save is
    // loaded, so reaching through Find throws rather than returning null on the main menu
    private static int? CurrentTick() => Verse.Current.Game?.tickManager?.TicksGame;
}
