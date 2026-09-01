using System;
using System.Collections.Generic;
using System.Linq;

namespace RimWorks.RimLogging.Sinks;

internal static class SinkLoader
{
    /// <summary>
    /// Reads the destination directory and retention count from global settings, then loads every  in the
    /// DefDatabase. This is the single point that couples sink loading to .
    /// </summary>
    internal static void LoadDefaults()
    {
        Settings.LoggingSettings s = Settings.LoggingMod.Settings;
        LoadFrom(Verse.DefDatabase<SinkDef>.AllDefs, s.logDirectory, s.retentionCount);
    }

    /// <summary>
    /// Builds and registers every enabled sink. Unknown types and constructor failures are
    /// warned about and skipped rather than aborting the rest.
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
    /// Per-type constructor table. Anything absent falls through to the parameterless path
    /// in <see cref="SinkPlan.TryCreate"/>.
    /// </summary>
    private static Dictionary<Type, Func<LogLevel, ILogSink?>> BuildFactories(string logDirectory, int retentionCount) => new()
    {
        [typeof(RollingTextFileSink)] = minLevel => new RollingTextFileSink(logDirectory, retentionCount, minLevel),
        [typeof(RollingJsonFileSink)] = minLevel => new RollingJsonFileSink(logDirectory, retentionCount, minLevel),
        [typeof(VerseLogSink)] = minLevel => new VerseLogSink(minLevel),
        [typeof(MemoryLogSink)] = minLevel => new MemoryLogSink(minLevel: minLevel),
    };
}
