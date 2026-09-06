using System;
using System.Collections.Generic;
using System.Linq;

namespace RimWorks.RimLogging.Sinks;

internal static class SinkLoader
{
    // only the sinks this loader made, so a reload cannot take out the viewer's own sink
    private static readonly List<ILogSink> Loaded = new List<ILogSink>();

    /// <summary>
    /// Reads the destination directory and retention count from global settings, then loads every  in the
    /// DefDatabase. This is the single point that couples sink loading to .
    /// </summary>
    internal static void LoadDefaults()
    {
        Settings.LoggingSettings s = Settings.LoggingMod.Settings;
        LoadFrom(Verse.DefDatabase<SinkDef>.AllDefs, s.logDirectory, s.retentionCount,
            s.sinkOverrideNames, s.sinkOverrideStates);
    }

    /// <summary>
    /// Builds and registers every enabled sink. Unknown types and constructor failures are
    /// warned about and skipped rather than aborting the rest.
    /// </summary>
    /// <param name="defs">The sink defs to load.</param>
    /// <param name="logDirectory">Destination directory for file sinks.</param>
    /// <param name="retentionCount">Number of rolled files to retain for file sinks.</param>
    /// <param name="overrideNames">Sink def names the user set explicitly.</param>
    /// <param name="overrideStates">On/off states parallel to <paramref name="overrideNames"/>.</param>
    internal static void LoadFrom(
        IEnumerable<SinkDef> defs,
        string logDirectory,
        int retentionCount,
        IReadOnlyList<string>? overrideNames = null,
        IReadOnlyList<bool>? overrideStates = null)
    {
        Dictionary<Type, Func<LogLevel, ILogSink?>> factories = BuildFactories(logDirectory, retentionCount);

        IEnumerable<SinkSpec> specs = defs.Select(def =>
            new SinkSpec(def.defName, def.sinkClass, def.minLevel,
                SinkToggles.IsEnabled(def.defName, def.enabledByDefault, overrideNames, overrideStates)));

        foreach (ILogSink sink in SinkPlan.Build(specs, factories, Bootstrap.PanicLog.Warn))
        {
            Logging.RegisterSink(sink);
            Loaded.Add(sink);
        }
    }

    /// <summary>Drops the sinks this loader made and builds them again from current settings.</summary>
    internal static void Reload()
    {
        foreach (ILogSink sink in Loaded)
        {
            SinkRegistry.Remove(sink);
            // a file sink holds an open handle, so the old one has to close before the new one opens
            try
            {
                sink.Dispose();
            }
            catch (Exception ex)
            {
                // the sink set is being swapped out, so this cannot go through our own sinks
                Bootstrap.PanicLog.Warn($"[RimLogging] sink '{sink.Name}' threw while closing: {ex.Message}");
            }
        }
        Loaded.Clear();
        LoadDefaults();
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
