using System;

namespace RimWorks.RimLogging.Pipeline;

/// <summary>
/// Guards against re-entrant logging from within a sink call. When a sink writes a log entry
/// and that write itself tries to log (e.g. an exception handler inside the sink), the guard
/// detects the nested call so the pipeline can short-circuit rather than recurse infinitely.
/// </summary>
internal static class ReentryGuard
{
    [System.ThreadStatic]
    private static bool _inSink;

    /// <summary>Gets a value indicating whether the current thread is already inside a sink write.</summary>
    internal static bool IsInsideSink => _inSink;

    // Routed through this static method (rather than writing _inSink directly in Scope.Dispose)
    // to avoid S2696 (CRITICAL): updating a static field from an instance method. Keep it static;
    // inlining it reintroduces that violation. S3398 (move-to-Scope) is wontfix for this reason.
    private static void ClearInSink() => _inSink = false;

    /// <summary>
    /// RAII scope token returned by <see cref="Enter"/>. Disposing the outermost scope clears
    /// the flag; disposing an inner (nested) scope leaves the flag set so the outer scope remains active.
    /// </summary>
    internal readonly struct Scope : IDisposable
    {
        private readonly bool _wasAlreadySet;
        internal Scope(bool wasAlreadySet) { _wasAlreadySet = wasAlreadySet; }
        public void Dispose() { if (!_wasAlreadySet) ClearInSink(); }
    }

    /// <summary>
    /// Marks the current thread as inside a sink write and returns a <see cref="Scope"/> that
    /// restores the previous state on disposal. Use with a <c>using</c> statement to guarantee cleanup.
    /// </summary>
    internal static Scope Enter()
    {
        bool was = _inSink;
        _inSink = true;
        return new Scope(was);
    }
}
