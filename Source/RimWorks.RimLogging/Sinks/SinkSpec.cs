namespace RimWorks.RimLogging.Sinks;

internal readonly struct SinkSpec
{
    public SinkSpec(string defName, string sinkClass, LogLevel minLevel, bool enabled)
    {
        DefName = defName;
        SinkClass = sinkClass;
        MinLevel = minLevel;
        Enabled = enabled;
    }

    public string DefName { get; }
    public string SinkClass { get; }
    public LogLevel MinLevel { get; }

    /// <summary>The def default after the user's override is applied, not the raw XML flag.</summary>
    public bool Enabled { get; }
}
