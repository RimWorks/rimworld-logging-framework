using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RimWorks.RimLogging.Patching;

/// <summary>
/// Guards <see cref="Logging.AttributionProvider"/> calls. A throw becomes "could not answer"
/// rather than "nothing patched it", because reporting a clean method we never managed to check
/// is a wrong answer, not a missing one.
/// </summary>
internal static class PatchAttributionGuard
{
    internal static IReadOnlyList<string>? OwnersFor(StackFrame frame)
    {
        Func<StackFrame, IReadOnlyList<string>?>? provider = Logging.AttributionProvider;
        if (provider == null) return Array.Empty<string>();
        try
        {
            return provider(frame);
        }
        catch
        {
            return null;
        }
    }
}
