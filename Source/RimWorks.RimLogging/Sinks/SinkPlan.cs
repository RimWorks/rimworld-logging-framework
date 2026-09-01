using System;
using System.Collections.Generic;

namespace RimWorks.RimLogging.Sinks;

/// <summary>
/// Turns <see cref="SinkSpec"/>s into sinks. Verse-free, so it tests without the game.
/// Prefers a <c>(LogLevel)</c> constructor so the def-supplied min level is honored.
/// </summary>
internal static class SinkPlan
{
    internal static List<ILogSink> Build(
        IEnumerable<SinkSpec> specs,
        IReadOnlyDictionary<Type, Func<LogLevel, ILogSink?>> factories,
        Action<string> warn)
    {
        List<ILogSink> sinks = [];
        foreach (SinkSpec spec in specs)
        {
            if (!spec.EnabledByDefault) continue;
            ILogSink? sink = TryCreate(spec, factories, warn);
            if (sink == null) continue;
            sinks.Add(sink);
        }
        return sinks;
    }

    internal static ILogSink? TryCreate(
        SinkSpec spec,
        IReadOnlyDictionary<Type, Func<LogLevel, ILogSink?>> factories,
        Action<string> warn)
    {
        try
        {
            Type? type = Type.GetType(spec.SinkClass, throwOnError: false);
            if (type == null)
            {
                warn($"[RimLogging] SinkDef '{spec.DefName}' references unknown type '{spec.SinkClass}', skipping.");
                return null;
            }

            if (factories.TryGetValue(type, out Func<LogLevel, ILogSink?>? factory))
                return factory(spec.MinLevel);

            System.Reflection.ConstructorInfo? levelCtor = type.GetConstructor([typeof(LogLevel)]);
            if (levelCtor != null)
                return (ILogSink?)levelCtor.Invoke([spec.MinLevel]);

            return (ILogSink?)Activator.CreateInstance(type);
        }
        catch (Exception ex)
        {
            Exception root = ex;
            while (root.InnerException != null) root = root.InnerException;
            warn($"[RimLogging] SinkDef '{spec.DefName}' failed to instantiate: {root.GetType().Name}: {root.Message}");
            return null;
        }
    }
}
