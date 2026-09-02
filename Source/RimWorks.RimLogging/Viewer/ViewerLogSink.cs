using System;
using System.Collections.Generic;
using RimWorks.RimLogging.Sinks;

namespace RimWorks.RimLogging.Viewer;

/// <summary>In-memory ring buffer of recent entries, backing the log viewer. Holds the last 20000 entries.</summary>
public sealed class ViewerLogSink : ILogSink {
    private readonly object syncRoot = new object();
    private readonly LogEntry[] ring = new LogEntry[20000];
    private readonly Dictionary<string, ChannelTally> tallies = new Dictionary<string, ChannelTally>(StringComparer.Ordinal);
    private int writeIndex;
    private int count;

    /// <summary>Raised after each entry lands in the buffer.</summary>
    public event Action<LogEntry>? EntryAdded;

    /// <inheritdoc/>
    public string Name => "ViewerLogSink";
    /// <inheritdoc/>
    public LogLevel MinLevel { get; set; } = LogLevel.Trace;

    /// <summary>Bumped on every write and on clear, so the viewer knows when to refilter.</summary>
    public int Revision { get; private set; }

    /// <summary>Row and error counts per channel, kept as entries land so the viewer never rescans.</summary>
    internal IReadOnlyDictionary<string, ChannelTally> ChannelTallies() {
        lock (syncRoot) {
            return new Dictionary<string, ChannelTally>(tallies, StringComparer.Ordinal);
        }
    }

    /// <summary>Rows currently held, which is the count the channel tree shows for "all".</summary>
    public int Count {
        get { lock (syncRoot) { return count; } }
    }

    /// <summary>Copies the buffer out in oldest-first order.</summary>
    public IReadOnlyList<LogEntry> Snapshot() {
        lock (syncRoot) {
            LogEntry[] snapshot = new LogEntry[count];
            for (int i = 0; i < count; i++) {
                int index = (writeIndex - count + i + ring.Length) % ring.Length;
                snapshot[i] = ring[index];
            }
            return snapshot;
        }
    }

    /// <summary>Drops every buffered entry. The viewer's Clear button calls this.</summary>
    public void Clear() {
        lock (syncRoot) {
            Array.Clear(ring, 0, ring.Length);
            tallies.Clear();
            writeIndex = 0;
            count = 0;
            Revision++;
        }
    }

    /// <inheritdoc/>
    public void Write(LogEntry entry) {
        if (entry.Level < MinLevel) {
            return;
        }
        lock (syncRoot) {
            // collapse a repeat into the previous row instead of adding one, the way vanilla's
            // LogMessageQueue does. a spamming mod would otherwise make the scrollbar unusable.
            int last = (writeIndex - 1 + ring.Length) % ring.Length;
            if (count > 0 && RepeatsPrevious(ring[last], entry)) {
                // replace rather than mutate: other sinks and earlier snapshots hold this
                // instance, and LogEntry is documented immutable
                ring[last] = Repeated(ring[last]);
                Revision++;
            }
            else {
                if (count == ring.Length) {
                    Untally(ring[writeIndex]);
                }
                ring[writeIndex] = entry;
                Tally(entry);
                writeIndex = (writeIndex + 1) % ring.Length;
                if (count < ring.Length) {
                    count++;
                }
                Revision++;
            }
        }
        EntryAdded?.Invoke(entry);
    }

    private void Tally(LogEntry entry) {
        string key = LogFilter.KeyFor(entry.Channel);
        tallies.TryGetValue(key, out ChannelTally tally);
        tally.Count++;
        if (entry.Level >= LogLevel.Error) {
            tally.ErrorCount++;
        }
        tallies[key] = tally;
    }

    private void Untally(LogEntry entry) {
        string key = LogFilter.KeyFor(entry.Channel);
        if (!tallies.TryGetValue(key, out ChannelTally tally)) {
            return;
        }
        tally.Count--;
        if (entry.Level >= LogLevel.Error) {
            tally.ErrorCount--;
        }
        if (tally.Count <= 0) {
            tallies.Remove(key);
        }
        else {
            tallies[key] = tally;
        }
    }

    private static LogEntry Repeated(LogEntry previous) {
        return new LogEntry {
            Timestamp = previous.Timestamp,
            Level = previous.Level,
            Channel = previous.Channel,
            MessageTemplate = previous.MessageTemplate,
            RenderedMessage = previous.RenderedMessage,
            Context = previous.Context,
            Source = previous.Source,
            StackTrace = previous.StackTrace,
            Exception = previous.Exception,
            Mod = previous.Mod,
            Repeats = previous.Repeats + 1,
        };
    }

    private static bool RepeatsPrevious(LogEntry previous, LogEntry entry) {
        return previous != null
            && previous.Level == entry.Level
            && string.Equals(previous.Channel, entry.Channel, StringComparison.Ordinal)
            && string.Equals(previous.RenderedMessage, entry.RenderedMessage, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public void Flush() {
        // No-op: entries are written synchronously into the in-memory ring buffer; nothing is buffered to flush.
    }

    /// <inheritdoc/>
    public void Dispose() {
        // No-op: the ring buffer holds only managed LogEntry values, no unmanaged or disposable resources.
    }
}
