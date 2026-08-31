using System;
using System.Collections.Generic;
using RimWorks.RimLogging;
using RimWorks.RimLogging.Sinks;

namespace RimWorks.RimLogging.LightweaveViewer;

public sealed class LightweaveLogSink : ILogSink {
    private readonly object syncRoot = new object();
    private readonly LogEntry[] ring = new LogEntry[4096];
    private int writeIndex;
    private int count;

    public event Action<LogEntry>? EntryAdded;

    public string Name => "LightweaveLogSink";
    public LogLevel MinLevel { get; set; } = LogLevel.Trace;

    public int Revision { get; private set; }

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

    public void Write(LogEntry entry) {
        if (entry.Level < MinLevel) {
            return;
        }
        lock (syncRoot) {
            ring[writeIndex] = entry;
            writeIndex = (writeIndex + 1) % ring.Length;
            if (count < ring.Length) {
                count++;
            }
            Revision++;
        }
        EntryAdded?.Invoke(entry);
    }

    public void Flush() {
        // No-op: entries are written synchronously into the in-memory ring buffer; nothing is buffered to flush.
    }

    public void Dispose() {
        // No-op: the ring buffer holds only managed LogEntry values, no unmanaged or disposable resources.
    }
}
