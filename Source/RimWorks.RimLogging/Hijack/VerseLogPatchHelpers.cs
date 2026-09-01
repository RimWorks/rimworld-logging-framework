using System;
using System.Diagnostics;
using System.Reflection;
using RimWorks.RimLogging.Capture;

namespace RimWorks.RimLogging.Hijack;

internal static class VerseLogPatchHelpers
{
    /// <summary>
    /// Walks the stack past patcher and pipeline frames to the first external assembly, and
    /// returns its channel and mod name. Falls back to <c>("Vanilla", null)</c>.
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
