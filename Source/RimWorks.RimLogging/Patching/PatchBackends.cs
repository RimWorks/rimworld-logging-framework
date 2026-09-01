using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorks.RimLogging.Bootstrap;

namespace RimWorks.RimLogging.Patching;

/// <summary>Finds the loaded patch backends and applies the best one.</summary>
internal static class PatchBackends
{
    private static bool applied;

    // scans assemblies rather than waiting on registration, because the hijack installs from
    // LoggingMod's constructor, before any [StaticConstructorOnStartup] has run
    internal static void ApplyBest()
    {
        if (applied)
        {
            return;
        }

        applied = true;

        List<IPatchBackend> found = BackendChoice.Ranked(Discover());
        if (found.Count == 0)
        {
            PanicLog.Write("[RimLogging] no patching backend loaded; install Harmony or Concord to capture Verse.Log and take over the log window.");
            return;
        }

        foreach (IPatchBackend backend in found)
        {
            try
            {
                backend.Apply();
            }
            catch (Exception ex)
            {
                PanicLog.Write($"[RimLogging] {backend.Name} backend failed to apply patches: {ex}");
                continue;
            }

            Verse.Log.Message($"[RimLogging] patched via {backend.Name}{IdleSuffix(found, backend)}");
            return;
        }

        PanicLog.Write("[RimLogging] every patching backend failed; Verse.Log capture is off for this run.");
    }

    private static string IdleSuffix(List<IPatchBackend> found, IPatchBackend winner)
    {
        List<string> others = new List<string>();
        foreach (IPatchBackend backend in found)
        {
            if (!ReferenceEquals(backend, winner))
            {
                others.Add(backend.Name);
            }
        }
        return others.Count == 0 ? string.Empty : $"; idle: {string.Join(", ", others.ToArray())}";
    }

    private static List<IPatchBackend> Discover()
    {
        List<IPatchBackend> found = new List<IPatchBackend>();
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type type in TypesOf(assembly))
            {
                if (type.IsAbstract || type.IsInterface || !typeof(IPatchBackend).IsAssignableFrom(type))
                {
                    continue;
                }

                try
                {
                    found.Add((IPatchBackend)Activator.CreateInstance(type)!);
                }
                catch (Exception ex)
                {
                    PanicLog.Write($"[RimLogging] could not create backend {type.Name}: {ex.Message}");
                }
            }
        }
        return found;
    }

    private static IEnumerable<Type> TypesOf(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            List<Type> loaded = new List<Type>();
            foreach (Type? type in ex.Types)
            {
                if (type != null)
                {
                    loaded.Add(type);
                }
            }
            return loaded;
        }
    }
}
