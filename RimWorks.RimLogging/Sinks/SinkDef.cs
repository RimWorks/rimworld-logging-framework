namespace RimWorks.RimLogging.Sinks;

/// <summary>
/// RimWorld Def that describes a log sink loaded from XML.
/// Each def names an <see cref="ILogSink"/> implementation that the bootstrap
/// phase instantiates and registers during game startup.
/// </summary>
public class SinkDef : Verse.Def
{
    /// <summary>Assembly-qualified type name of the <see cref="ILogSink"/> implementation to instantiate.</summary>
    public string sinkClass = "";

    /// <summary>Minimum <see cref="LogLevel"/> this sink will receive. Entries below this level are not forwarded.</summary>
    public LogLevel minLevel = LogLevel.Trace;

    /// <summary>Whether this sink is registered on startup unless explicitly disabled by the user.</summary>
    public bool enabledByDefault = true;
}
