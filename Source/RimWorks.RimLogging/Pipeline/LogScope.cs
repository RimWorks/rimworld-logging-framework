using System;
using System.Collections.Generic;

namespace RimWorks.RimLogging.Pipeline;

/// <summary>
/// Ambient key/value pairs that attach to every entry emitted inside a scope. Per-thread, because
/// a scope belongs to the call stack that opened it and logging happens on several threads.
/// </summary>
internal static class LogScope
{
    [ThreadStatic]
    private static List<KeyValuePair<string, object?>>? frames;

    /// <summary>How many pairs are currently in scope on this thread.</summary>
    internal static int Depth => frames?.Count ?? 0;

    /// <summary>Adds a pair to the ambient scope until the returned handle is disposed.</summary>
    internal static IDisposable Push(string key, object? value)
    {
        if (string.IsNullOrEmpty(key)) return NullFrame.Instance;
        frames ??= new List<KeyValuePair<string, object?>>();
        frames.Add(new KeyValuePair<string, object?>(key, value));
        return new Frame(frames.Count - 1);
    }

    /// <summary>
    /// Merges the ambient pairs under the entry's own context. Explicit context wins, since the
    /// caller naming a key at the call site meant that one.
    /// </summary>
    internal static IReadOnlyDictionary<string, object?>? Merge(IReadOnlyDictionary<string, object?>? explicitContext)
    {
        List<KeyValuePair<string, object?>>? scope = frames;
        if (scope == null || scope.Count == 0) return explicitContext;

        Dictionary<string, object?> merged = new Dictionary<string, object?>(
            scope.Count + (explicitContext?.Count ?? 0), StringComparer.Ordinal);
        for (int i = 0; i < scope.Count; i++) merged[scope[i].Key] = scope[i].Value;
        if (explicitContext != null)
        {
            foreach (KeyValuePair<string, object?> pair in explicitContext) merged[pair.Key] = pair.Value;
        }
        return merged;
    }

    /// <summary>Drops every frame on this thread. For tests, and for a drain thread starting clean.</summary>
    internal static void Clear() => frames?.Clear();

    private sealed class Frame : IDisposable
    {
        private readonly int index;
        private bool disposed;

        internal Frame(int index) => this.index = index;

        // popping to the index rather than removing one entry keeps out-of-order disposal from
        // leaving a stale frame behind for every later entry on this thread
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            List<KeyValuePair<string, object?>>? scope = frames;
            if (scope != null && index < scope.Count) scope.RemoveRange(index, scope.Count - index);
        }
    }

    private sealed class NullFrame : IDisposable
    {
        internal static readonly NullFrame Instance = new NullFrame();
        public void Dispose() { }
    }
}
