using System.Collections.Generic;
using RimWorks.RimLogging.Pipeline;

namespace RimWorks.RimLogging.Sinks;

/// <summary>
/// Holds the active sinks and fans entries out to them. New sinks are replayed a history buffer,
/// so one that registers late still sees entries emitted before it existed.
/// </summary>
internal static class SinkRegistry
{
    /// <summary>
    /// History cap after the first replay. Unbounded until then, so a late sink gets everything.
    /// </summary>
    internal static int PostReplayCap { get; set; } = 10000;

    private static readonly List<Bound> _sinks = new List<Bound>();
    private static readonly Queue<LogEntry> _history = new Queue<LogEntry>();
    private static readonly System.Threading.ReaderWriterLockSlim _lock = new System.Threading.ReaderWriterLockSlim();
    private static bool _historyCapped;

    internal static void Register(ILogSink sink, bool replayHistory = true, string? defName = null)
    {
        _lock.EnterWriteLock();
        try
        {
            if (replayHistory) ReplayHistoryTo(sink);
            _sinks.Add(new Bound(sink, defName));
        }
        finally { _lock.ExitWriteLock(); }
    }

    internal static bool Remove(ILogSink sink)
    {
        _lock.EnterWriteLock();
        try
        {
            for (int i = 0; i < _sinks.Count; i++)
            {
                if (!ReferenceEquals(_sinks[i].Sink, sink)) continue;
                _sinks.RemoveAt(i);
                return true;
            }
            return false;
        }
        finally { _lock.ExitWriteLock(); }
    }

    internal static IReadOnlyList<ILogSink> Snapshot()
    {
        _lock.EnterReadLock();
        try
        {
            ILogSink[] snap = new ILogSink[_sinks.Count];
            for (int i = 0; i < snap.Length; i++) snap[i] = _sinks[i].Sink;
            return snap;
        }
        finally { _lock.ExitReadLock(); }
    }

    internal static void DispatchSynchronously(LogEntry entry)
    {
        if (ReentryGuard.IsInsideSink) return;
        Bound[] snap;
        _lock.EnterWriteLock();
        try
        {
            AppendHistory(entry);
            snap = _sinks.ToArray();
        }
        finally { _lock.ExitWriteLock(); }
        Channels.ChannelSettings channel = Logging.SettingsFor(entry.Channel);

        // destinations name SinkDefs, so a sink registered from code was never in that namespace.
        // a list naming no loaded def would go dark, so it counts as no restriction at all
        bool restrict = false;
        if (channel.HasDestinations)
        {
            for (int i = 0; i < snap.Length; i++)
            {
                if (snap[i].Routable(channel)) { restrict = true; break; }
            }
        }

        using (ReentryGuard.Enter())
        {
            for (int i = 0; i < snap.Length; i++)
            {
                // MinLevel is documented on ILogSink as a per-sink gate, so it has to be
                // honoured here; a sink cannot filter what it was never told about.
                if (entry.Level < snap[i].Sink.MinLevel) continue;
                if (restrict && snap[i].DefName != null && !snap[i].Routable(channel)) continue;
                try { snap[i].Sink.Write(entry); }
                catch { /* swallow: a misbehaving sink must not break dispatch to the others */ }
            }
        }
    }

    internal static void FlushAll()
    {
        IReadOnlyList<ILogSink> snap = Snapshot();
        for (int i = 0; i < snap.Count; i++)
        {
            try { snap[i].Flush(); }
            catch { /* swallow: flush failure in one sink must not block flushing the rest */ }
        }
    }

    internal static void DisposeAll()
    {
        IReadOnlyList<ILogSink> snap = Snapshot();
        _lock.EnterWriteLock();
        try
        {
            _sinks.Clear();
            _history.Clear();
            _historyCapped = false;
        }
        finally { _lock.ExitWriteLock(); }
        for (int i = 0; i < snap.Count; i++)
        {
            try { snap[i].Dispose(); }
            catch { /* swallow: dispose failure in one sink must not block disposing the rest */ }
        }
    }

    private static void AppendHistory(LogEntry entry)
    {
        _history.Enqueue(entry);
        if (_historyCapped)
        {
            while (_history.Count > PostReplayCap) _history.Dequeue();
        }
    }

    private static void ReplayHistoryTo(ILogSink sink)
    {
        if (_history.Count == 0) return;
        using (ReentryGuard.Enter())
        {
            foreach (LogEntry entry in _history)
            {
                try { sink.Write(entry); }
                catch { /* swallow: a sink that throws on replay must not abort registration */ }
            }
        }
        if (_historyCapped) return;
        _historyCapped = true;
        while (_history.Count > PostReplayCap) _history.Dequeue();
    }

    /// <summary>A registered sink paired with the <c>SinkDef</c> that loaded it, if any.</summary>
    private readonly struct Bound
    {
        internal Bound(ILogSink sink, string? defName)
        {
            Sink = sink;
            DefName = defName;
        }

        internal ILogSink Sink { get; }

        /// <summary>The def that loaded this sink, or <c>null</c> when it was registered from code.</summary>
        internal string? DefName { get; }

        internal bool Routable(Channels.ChannelSettings channel)
            => DefName != null && channel.AllowsSink(DefName);
    }
}
