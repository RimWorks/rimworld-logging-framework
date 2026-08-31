using System;
using System.Collections.Generic;

namespace RimWorks.RimLogging.Sinks;

/// <summary>
/// Verse-free sink resolution + planning. Decides which <see cref="SinkSpec"/>s
/// become <see cref="ILogSink"/>s: applies the enabled-by-default gate, resolves
/// the type, dispatches through the supplied factory map, then prefers a
/// <c>(LogLevel)</c> constructor (so the def-supplied min level is honored)
/// before falling back to a parameterless constructor. Diagnostics are emitted
/// via the supplied warn callback so the helper can be unit-tested without the
/// Verse runtime.
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
