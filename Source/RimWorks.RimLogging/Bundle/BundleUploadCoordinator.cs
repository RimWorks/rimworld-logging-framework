using System.Collections.Generic;
using RimWorks.RimLogging.Sinks;

namespace RimWorks.RimLogging.Bundle;

/// <summary>
/// Verse-free helpers backing the settings-window "Upload bundle" action: locating the in-memory
/// log buffer to bundle and turning an upload outcome into a user-facing message. Kept free of any
/// RimWorld dependency so the decision logic is unit-testable.
/// </summary>
public static class BundleUploadCoordinator
{
    /// <summary>Returns the first registered <see cref="MemoryLogSink"/>, or <c>null</c> when none is registered.</summary>
    /// <param name="sinks">The registered sinks to scan.</param>
    public static MemoryLogSink? FindMemorySink(IReadOnlyList<ILogSink> sinks)
    {
        for (int i = 0; i < sinks.Count; i++)
        {
            if (sinks[i] is MemoryLogSink memory) return memory;
        }
        return null;
    }

    /// <summary>Formats an upload outcome into a single user-facing line: the gist URL on success, the error otherwise.</summary>
    /// <param name="result">The upload outcome to describe.</param>
    public static string DescribeResult(ProxyResult result)
    {
        if (result.Success) return $"Bundle uploaded: {result.GistUrl}";
        return $"Bundle upload failed: {result.ErrorMessage}";
    }
}
