using System;
using System.Collections.Generic;
using System.Linq;

namespace RimWorks.RimLogging.Sinks;

internal static class SinkLoader
{
    /// <summary>
    /// Reads the destination directory and retention count from global settings, then
    /// loads every <see cref="SinkDef"/> in the DefDatabase. This is the single point
    /// that couples sink loading to <see cref="Settings.LoggingMod.Settings"/>.
    /// </summary>
    internal static void LoadDefaults()
    {
        Settings.LoggingSettings s = Settings.LoggingMod.Settings;
        LoadFrom(Verse.DefDatabase<SinkDef>.AllDefs, s.logDirectory, s.retentionCount);
    }

    /// <summary>
    /// Instantiates each enabled sink from <paramref name="defs"/> and registers it with
    /// Logging. File sinks are constructed with the supplied <paramref name="logDirectory"/>
    /// and <paramref name="retentionCount"/>; the minimum level comes from the def. Unknown
    /// types or constructor failures are logged via Verse.Log.Warning and skipped.
    /// </summary>
    /// <param name="defs">The sink defs to load.</param>
    /// <param name="logDirectory">Destination directory for file sinks.</param>
    /// <param name="retentionCount">Number of rolled files to retain for file sinks.</param>
    internal static void LoadFrom(IEnumerable<SinkDef> defs, string logDirectory, int retentionCount)
    {
        Dictionary<Type, Func<LogLevel, ILogSink?>> factories = BuildFactories(logDirectory, retentionCount);

        IEnumerable<SinkSpec> specs = defs.Select(def =>
            new SinkSpec(def.defName, def.sinkClass, def.minLevel, def.enabledByDefault));

        foreach (ILogSink sink in SinkPlan.Build(specs, factories, Verse.Log.Warning))
            Logging.RegisterSink(sink);
    }

    /// <summary>
    /// Builds the per-sink-type constructor table. File sinks close over the supplied
    /// directory and retention count; the minimum level is supplied per def. Types not
    /// present here fall through to the parameterless-constructor path in
    /// <see cref="SinkPlan.TryCreate"/>.
    /// </summary>
    private static Dictionary<Type, Func<LogLevel, ILogSink?>> BuildFactories(string logDirectory, int retentionCount) => new()
    {
        [typeof(RollingTextFileSink)] = minLevel => new RollingTextFileSink(logDirectory, retentionCount, minLevel),
        [typeof(RollingJsonFileSink)] = minLevel => new RollingJsonFileSink(logDirectory, retentionCount, minLevel),
        [typeof(VerseLogSink)] = minLevel => new VerseLogSink(minLevel),
        [typeof(MemoryLogSink)] = minLevel => new MemoryLogSink(minLevel: minLevel),
    };
}
