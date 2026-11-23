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
            if (!TryCached(method, ilOffset, out SourceLocation src))
            {
                here ??= new StackTrace(0, false);
                int depth = here.FrameCount - frames.FrameCount + i;
                Interlocked.Increment(ref _resolutions);
                if (depth < 0 || !TryResolve(new StackFrame(depth, true), method, ilOffset, out src)) continue;
            }

            if (src.File.Length > 0) return src;
            if (StackWalker.FirstCallerEndsWithoutFile) return new SourceLocation(string.Empty, 0, method?.Name);
        }
        return SourceLocation.Empty;
    }

    private static bool TryCached(MethodBase? method, int ilOffset, out SourceLocation src)
    {
        src = SourceLocation.Empty;
        return method is not null and not System.Reflection.Emit.DynamicMethod
            && _frames.TryGetValue((method, ilOffset), out src);
    }

    private static bool TryResolve(StackFrame resolved, MethodBase? method, int ilOffset, out SourceLocation src)
    {
        src = SourceLocation.Empty;
        if (method == null || !Equals(resolved.GetMethod(), method) || resolved.GetILOffset() != ilOffset) return false;

        string? file = resolved.GetFileName();
        if (file != null)
            src = new SourceLocation(StackWalker.NormalizePath(file, method.DeclaringType), resolved.GetFileLineNumber(), method.Name);
        if (method is not System.Reflection.Emit.DynamicMethod) _frames.TryAdd((method, ilOffset), src);
        return true;
    }
}
