using System;
using System.Diagnostics;
using System.Reflection;
using RimWorks.RimLogging.Capture;

namespace RimWorks.RimLogging.Hijack;

internal static class VerseLogPatchHelpers
{
    /// <summary>
    /// Walks the call stack (skipping Harmony invoker frames and our own pipeline frames) to find
    /// the first external assembly, then returns its channel name via <see cref="AssemblyChannelCache.Resolve"/>
    /// together with its mod name via <see cref="ModNameCache.ForAssembly"/>. Falls back to
    /// <c>("Vanilla", null)</c> when no external frame is found.
    /// </summary>
    internal static (string Channel, string? Mod) ResolveCaller()
    {
        StackTrace st = new StackTrace(2, false);
        for (int i = 0; i < st.FrameCount; i++)
        {
            MethodBase? m = st.GetFrame(i)?.GetMethod();
            Type? dt = m?.DeclaringType;
            string? ns = dt?.FullName;
            string? asm = dt?.Assembly.GetName().Name;
            if (CallerFrameClassifier.IsInternalFrame(ns, asm)) continue;
            return (AssemblyChannelCache.Resolve(dt!.Assembly), ModNameCache.ForAssembly(dt.Assembly));
        }
        return ("Vanilla", null);
    }
}
