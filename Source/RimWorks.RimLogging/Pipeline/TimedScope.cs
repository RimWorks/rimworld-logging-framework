using System;
using System.Diagnostics;

namespace RimWorks.RimLogging.Pipeline;

/// <summary>
/// Times a using-block and logs the elapsed milliseconds when it closes. A struct, so a scope in
/// a tick loop costs no allocation; use it in a <c>using</c> and never store it.
/// </summary>
public struct TimedScope : IDisposable
{
    private readonly long startTicks;
    private readonly LogLevel level;
    private readonly string channel;
    private readonly string message;
    private readonly int line;
    private readonly string file;
    private bool armed;

    internal TimedScope(LogLevel level, string channel, string message, int line, string file)
    {
        this.level = level;
        this.channel = channel;
        this.message = message;
        this.line = line;
        this.file = file;
        startTicks = Now();
        armed = true;
    }

    /// <summary>Test seam: stands in for the monotonic clock so a test need not sleep.</summary>
    internal static Func<long>? TimestampProvider { get; set; }

    /// <summary>Emits the entry with an <c>elapsed_ms</c> context value. A second call does nothing.</summary>
    public void Dispose()
    {
        if (!armed) return;
        armed = false;
        double ms = (Now() - startTicks) * 1000d / Stopwatch.Frequency;
        Log.EmitTimed(level, channel, message, Math.Round(ms, 1), line, file);
    }

    private static long Now() => TimestampProvider?.Invoke() ?? Stopwatch.GetTimestamp();
}
