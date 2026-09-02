using System;
using RimWorks.RimLogging.Pipeline;
using RimWorks.RimLogging.Sinks;

namespace RimWorks.RimLogging;

/// <summary>Framework lifecycle and sink registration: init/shutdown, global minimum level, and sink management. The public emit API is <see cref="Log"/>.</summary>
public static class Logging
{
    private static MpscQueue<LogEntry>? _queue;
    private static BackgroundDrain? _drain;
    internal static Action<BackgroundDrain>? InstallShutdownHook { get; set; }

    /// <summary>Global minimum level; entries below this are dropped before dispatch.</summary>
    public static LogLevel GlobalMinLevel { get; set; } = LogLevel.Trace;

    /// <summary>When <c>true</c>, every emitted entry captures and stores a formatted stack trace. Defaults to <c>true</c>.</summary>
    public static bool CaptureStackTraces { get; set; } = true;

    /// <summary>Supplies the current game tick, or <c>null</c> when no game is running.</summary>
    public static System.Func<int?>? TickProvider { get; set; }

    /// <summary>
    /// The current tick, or <c>null</c>. A provider that throws is swallowed: a logging call must
    /// never take down its caller, and this one runs on every emit.
    /// </summary>
    internal static int? CurrentTick()
    {
        System.Func<int?>? provider = TickProvider;
        if (provider == null) return null;
        try
        {
            return provider();
        }
        catch
        {
            return null;
        }
    }

    internal static System.Func<bool>? IsDegradedProvider { get; set; }

    // set by ChannelRegistry at boot; Log.cs cannot reach it directly because the registry is
    // Verse-bound and dropped from the test compile
    internal static System.Func<string, Channels.ChannelSettings>? ChannelSettingsProvider { get; set; }

    // set by PatchBackends once the winning backend applies; takes a StackFrame, not a
    // MethodBase, so Mono's null-GetMethod() replacement frames can still resolve (see StackWalker).
    internal static System.Func<System.Diagnostics.StackFrame, System.Collections.Generic.IReadOnlyList<string>?>? AttributionProvider { get; set; }

    /// <summary>The overrides for a channel, or none when no registry is installed.</summary>
    internal static Channels.ChannelSettings SettingsFor(string channel)
        => ChannelSettingsProvider?.Invoke(channel) ?? Channels.ChannelSettings.Inherit;

    /// <summary><c>true</c> when this instance is the primary (non-degraded) logger; <c>false</c> if a degraded-mode provider reports otherwise.</summary>
    public static bool IsPrimary => !(IsDegradedProvider?.Invoke() ?? false);

    /// <summary>Emit a log entry, applying the global minimum-level filter.</summary>
    internal static void Emit(LogEntry entry)
    {
        if (entry.Level < GlobalMinLevel) return;

        if (SynchronousBypass.ShouldBypass(entry.Level) || _drain == null)
        {
            DispatchSync(entry);
            return;
        }
        _drain.Enqueue(entry);
    }

    internal static void DispatchSync(LogEntry entry)
    {
        SinkRegistry.DispatchSynchronously(entry);
    }

    /// <summary>Lazy initializer used by internal paths.</summary>
    internal static void EnsureStarted()
    {
        if (_queue != null) return;
        _queue = new MpscQueue<LogEntry>(65536);
        _drain = new BackgroundDrain(_queue, DispatchSync);
    }

    /// <summary>
    /// Explicit public lifecycle entry point. Idempotent.
    /// Invokes <see cref="InstallShutdownHook"/> if non-null (wired by Bootstrap; null in tests).
    /// </summary>
    public static void Init()
    {
        EnsureStarted();
        if (_drain != null) InstallShutdownHook?.Invoke(_drain);
    }

    /// <summary>Tears down the drain and clears queue/drain references. After this, Emit routes synchronously.</summary>
    public static void Shutdown()
    {
        _drain?.Dispose();
        _drain = null;
        _queue = null;
    }

    /// <summary>Registers a sink to receive dispatched log entries.</summary>
    /// <param name="sink">The sink to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sink"/> is <c>null</c>.</exception>
    public static void RegisterSink(ILogSink sink)
    {
        if (sink == null) throw new ArgumentNullException(nameof(sink));
        Sinks.SinkRegistry.Register(sink);
    }

    /// <summary>Removes a previously registered sink.</summary>
    /// <param name="sink">The sink to remove.</param>
    /// <returns><c>true</c> if the sink was registered and removed; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sink"/> is <c>null</c>.</exception>
    public static bool RemoveSink(ILogSink sink)
    {
        if (sink == null) throw new ArgumentNullException(nameof(sink));
        return Sinks.SinkRegistry.Remove(sink);
    }

    /// <summary>A snapshot of the currently registered sinks.</summary>
    public static IReadOnlyList<ILogSink> RegisteredSinks => Sinks.SinkRegistry.Snapshot();

    internal static void StopForTests() => Shutdown();

    /// <summary>Test seam: installs a shutdown hook, standing in for the Bootstrap wiring.</summary>
    internal static void SetShutdownHookForTests(Action<BackgroundDrain>? hook) => InstallShutdownHook = hook;

    /// <summary>Test seam: clears any installed shutdown hook so tests start from a known state.</summary>
    internal static void ResetShutdownHookForTests() => InstallShutdownHook = null;
}
