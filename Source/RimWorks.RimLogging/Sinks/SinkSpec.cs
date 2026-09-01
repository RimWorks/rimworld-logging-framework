namespace RimWorks.RimLogging.Sinks;

internal readonly struct SinkSpec
{
    public SinkSpec(string defName, string sinkClass, LogLevel minLevel, bool enabledByDefault)
    {
        DefName = defName;
        SinkClass = sinkClass;
        MinLevel = minLevel;
        EnabledByDefault = enabledByDefault;
    }

    public string DefName { get; }
    public string SinkClass { get; }
    public LogLevel MinLevel { get; }
    public bool EnabledByDefault { get; }
}
