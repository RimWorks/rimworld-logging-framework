namespace RimWorks.RimLogging.Sinks;

internal readonly struct SinkSpec
{
    public SinkSpec(string defName, string sinkClass, LogLevel minLevel, bool enabled, LogLevel? printStackAt = null)
    {
        DefName = defName;
        SinkClass = sinkClass;
        MinLevel = minLevel;
        Enabled = enabled;
        PrintStackAt = printStackAt;
    }

    public string DefName { get; }
    public string SinkClass { get; }
    public LogLevel MinLevel { get; }

    /// <summary>The def default after the user's override is applied, not the raw XML flag.</summary>
    public bool Enabled { get; }

    /// <summary>The def's print level for stored stack traces, or <c>null</c> to follow <see cref="Logging.PrintStackAt"/>.</summary>
    public LogLevel? PrintStackAt { get; }
}
