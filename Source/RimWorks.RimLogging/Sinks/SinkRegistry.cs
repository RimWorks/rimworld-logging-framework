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

    private static readonly List<ILogSink> _sinks = new List<ILogSink>();
    private static readonly Queue<LogEntry> _history = new Queue<LogEntry>();
    private static readonly System.Threading.ReaderWriterLockSlim _lock = new System.Threading.ReaderWriterLockSlim();
    private static bool _historyCapped;

    internal static void Register(ILogSink sink)
    {
        _lock.EnterWriteLock();
        try
        {
            ReplayHistoryTo(sink);
            _sinks.Add(sink);
        }
        finally { _lock.ExitWriteLock(); }
    }

    internal static bool Remove(ILogSink sink)
    {
        _lock.EnterWriteLock();
        try { return _sinks.Remove(sink); }
        finally { _lock.ExitWriteLock(); }
    }

    internal static IReadOnlyList<ILogSink> Snapshot()
    {
        _lock.EnterReadLock();
        try { return _sinks.ToArray(); }
        finally { _lock.ExitReadLock(); }
    }

    internal static void DispatchSynchronously(LogEntry entry)
    {
        if (ReentryGuard.IsInsideSink) return;
        ILogSink[] snap;
        _lock.EnterWriteLock();
        try
        {
            AppendHistory(entry);
            snap = _sinks.ToArray();
        }
        finally { _lock.ExitWriteLock(); }
        Channels.ChannelSettings channel = Logging.SettingsFor(entry.Channel);

        // a channel naming only sinks that are not registered would otherwise go dark, so a
        // destination list that matches nothing at all is treated as no restriction
        bool restrict = false;
        if (channel.HasDestinations)
        {
            for (int i = 0; i < snap.Length; i++)
            {
                if (channel.AllowsSink(snap[i].Name)) { restrict = true; break; }
            }
        }

        using (ReentryGuard.Enter())
        {
            for (int i = 0; i < snap.Length; i++)
            {
                // MinLevel is documented on ILogSink as a per-sink gate, so it has to be
                // honoured here; a sink cannot filter what it was never told about.
                if (entry.Level < snap[i].MinLevel) continue;
                if (restrict && !channel.AllowsSink(snap[i].Name)) continue;
                try { snap[i].Write(entry); }
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
}
