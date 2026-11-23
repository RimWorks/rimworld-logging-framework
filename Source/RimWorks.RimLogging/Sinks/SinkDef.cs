namespace RimWorks.RimLogging.Sinks;

/// <summary>
/// RimWorld Def that describes a log sink loaded from XML. Each def names an  implementation that the
/// bootstrap phase instantiates and registers during game startup.
/// </summary>
public class SinkDef : Verse.Def
{
    /// <summary>Assembly-qualified type name of the <see cref="ILogSink"/> implementation to instantiate.</summary>
    public string sinkClass = string.Empty;

    /// <summary>Minimum <see cref="LogLevel"/> this sink will receive. Entries below this level are not forwarded.</summary>
    public LogLevel minLevel = LogLevel.Trace;

    /// <summary>Whether this sink is registered on startup unless explicitly disabled by the user.</summary>
    public bool enabledByDefault = true;

    /// <summary>
    /// Level at or above which a text sink prints the stored stack trace, or <c>null</c> to follow
    /// <see cref="Logging.PrintStackAt"/>. Sink types without a print level ignore it.
    /// </summary>
    public LogLevel? printStackAt = null;
}
