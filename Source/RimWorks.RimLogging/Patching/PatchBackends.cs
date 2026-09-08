using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            InstallAttribution(found);
            Logging.CaptureBackend = backend.Name;

            // our own status line: routing it through Verse.Log made the hijack capture it, and
            // the caller walk skips our frames, so it landed on Mod.Unknown blaming our own patch
            Log.InfoTo(Log.SelfChannel, $"patched via {backend.Name}{IdleSuffix(found, backend)}");
            return;
        }

        PanicLog.Write("[RimLogging] every patching backend failed; Verse.Log capture is off for this run.");
    }

    /// <summary>
    /// Installs attribution from every loaded backend, not only the one that won patching.
    /// Patching needs a single winner; attribution does not, and a target Concord routed into a
    /// Harmony-patched method runs inside Harmony's replacement, which only Harmony can resolve.
    /// </summary>
    private static void InstallAttribution(List<IPatchBackend> found)
    {
        List<IPatchAttributionSource> sources = new List<IPatchAttributionSource>(found.Count);
        foreach (IPatchBackend backend in found)
        {
            if (backend is IPatchAttributionSource source) sources.Add(source);
        }
        if (sources.Count == 0) return;

        Logging.AttributionProvider = frame => MergeOwners(sources, frame);
    }

    private static IReadOnlyList<string>? MergeOwners(List<IPatchAttributionSource> sources, StackFrame frame)
    {
        List<string>? owners = null;
        for (int i = 0; i < sources.Count; i++)
        {
            IReadOnlyList<string>? answer = sources[i].OwnersFor(frame);

            // one source that could not answer makes the whole result unreliable: a partial
            // list presented as complete is the same lie as claiming a method is clean
            if (answer == null) return null;

            for (int j = 0; j < answer.Count; j++)
            {
                owners ??= new List<string>();
                if (!owners.Contains(answer[j])) owners.Add(answer[j]);
            }
        }
        return (IReadOnlyList<string>?)owners ?? Array.Empty<string>();
    }

    private static string IdleSuffix(List<IPatchBackend> found, IPatchBackend winner)
    {
        List<string> others = found
            .Where(backend => !ReferenceEquals(backend, winner))
            .Select(backend => backend.Name)
            .ToList();
        return others.Count == 0 ? string.Empty : $"; idle: {string.Join(", ", others.ToArray())}";
    }

    private static List<IPatchBackend> Discover()
    {
        List<IPatchBackend> found = new List<IPatchBackend>();
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type type in TypesOf(assembly))
            {
                if (!IsBackend(type))
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
            return ex.Types.Where(type => type != null).ToList();
        }
        catch (Exception)
        {
            // a dynamic or half-loaded assembly cannot be enumerated at all, so skip it whole.
            return Array.Empty<Type>();
        }
    }

    /// <summary>Tests a scanned type without letting a broken one stop the scan.</summary>
    // IsAssignableFrom walks the interface map, so a type whose interfaces live in a missing
    // assembly throws here. stays quiet: a half-loaded mod can hit this for hundreds of types.
    private static bool IsBackend(Type type)
    {
        try
        {
            return !type.IsAbstract && !type.IsInterface && typeof(IPatchBackend).IsAssignableFrom(type);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
