using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace RimWorks.RimLogging.Capture;

internal static class SourceCache
{
    private static readonly ConcurrentDictionary<(MethodBase Method, int ILOffset), SourceLocation> _frames =
        new ConcurrentDictionary<(MethodBase Method, int ILOffset), SourceLocation>();

    private static long _resolutions;

    internal static long Resolutions => Interlocked.Read(ref _resolutions);

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static SourceLocation FirstCallerFrame(StackTrace? walk = null)
    {
        StackTrace? here = walk == null ? new StackTrace(0, false) : null;
        StackTrace frames = walk ?? here!;
        for (int i = 0; i < frames.FrameCount; i++)
        {
            StackFrame? frame = frames.GetFrame(i);
            MethodBase? method = frame?.GetMethod();
            System.Type? declaringType = method?.DeclaringType;
            if (CallerFrameClassifier.IsInternalFrame(declaringType?.FullName, declaringType?.Assembly.GetName().Name)) continue;

            int ilOffset = frame!.GetILOffset();
            bool cacheable = method is not System.Reflection.Emit.DynamicMethod;
            if (!cacheable || !_frames.TryGetValue((method!, ilOffset), out SourceLocation src))
            {
                here ??= new StackTrace(0, false);
                int depth = here.FrameCount - frames.FrameCount + i;
                StackFrame? resolved = depth >= 0 ? new StackFrame(depth, true) : null;
                Interlocked.Increment(ref _resolutions);
                if (resolved == null || !Equals(resolved.GetMethod(), method) || resolved.GetILOffset() != ilOffset) continue;

                string? file = resolved.GetFileName();
                src = file == null
                    ? SourceLocation.Empty
                    : new SourceLocation(StackWalker.NormalizePath(file, declaringType), resolved.GetFileLineNumber(), method!.Name);
                if (cacheable) _frames.TryAdd((method!, ilOffset), src);
            }

            if (src.File.Length > 0) return src;
            if (StackWalker.FirstCallerEndsWithoutFile) return new SourceLocation(string.Empty, 0, method?.Name);
        }
        return SourceLocation.Empty;
    }
}
