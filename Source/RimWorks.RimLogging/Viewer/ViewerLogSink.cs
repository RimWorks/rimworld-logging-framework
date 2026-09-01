using System;
using System.Collections.Generic;
using RimWorks.RimLogging.Sinks;

namespace RimWorks.RimLogging.Viewer;

/// <summary>In-memory ring buffer of recent entries, backing the log viewer. Holds the last 1000 entries.</summary>
public sealed class ViewerLogSink : ILogSink {
    private readonly object syncRoot = new object();
    private readonly LogEntry[] ring = new LogEntry[1000];
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
                ring[last].Repeats++;
                Revision++;
            }
            else {
                ring[writeIndex] = entry;
                writeIndex = (writeIndex + 1) % ring.Length;
                if (count < ring.Length) {
                    count++;
                }
                Revision++;
            }
        }
        EntryAdded?.Invoke(entry);
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
