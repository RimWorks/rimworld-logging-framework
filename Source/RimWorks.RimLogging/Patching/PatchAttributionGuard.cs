using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RimWorks.RimLogging.Patching;

/// <summary>Guards <see cref="Logging.AttributionProvider"/> calls: no provider, a throw, or a
/// null result all become empty, so a bad backend never takes down a log call.</summary>
internal static class PatchAttributionGuard
{
    private static readonly IReadOnlyList<string> Empty = Array.Empty<string>();

    internal static IReadOnlyList<string> OwnersFor(StackFrame frame)
    {
        Func<StackFrame, IReadOnlyList<string>>? provider = Logging.AttributionProvider;
        if (provider == null) return Empty;
        try
        {
            return provider(frame) ?? Empty;
        }
        catch
        {
            return Empty;
        }
    }
}
