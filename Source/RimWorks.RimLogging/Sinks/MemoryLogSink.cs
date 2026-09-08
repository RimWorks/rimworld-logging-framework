using System;
using System.Collections.Generic;

namespace RimWorks.RimLogging.Sinks;

/// <summary>
/// An in-memory, ring-buffered <see cref="ILogSink"/> intended for use in tests and diagnostics.
/// Thread-safe. Bounded by <c>capacity</c> to prevent unbounded growth during long-running test sessions.
/// </summary>
public sealed class MemoryLogSink : ILogSink
{
    private readonly object _lock = new object();
    private readonly LogEntry[] _ring;
    private int _writeIndex;
    private int _count;
    private long _droppedCount;

    /// <summary>
    /// Initializes a new <see cref="MemoryLogSink"/>.
    /// </summary>
    /// <param name="capacity">Maximum number of entries retained. Must be &gt;= 1. Defaults to 1024.</param>
    /// <param name="minLevel">Entries below this level are silently dropped. Defaults to <see cref="LogLevel.Trace"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity"/> is less than 1.</exception>
    public MemoryLogSink(int capacity = 1024, LogLevel minLevel = LogLevel.Trace)
    {
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
        _ring = new LogEntry[capacity];
        MinLevel = minLevel;
    }

    /// <inheritdoc/>
    // settable so a consumer can match a channel's destination list, which filters on sink name
    public string Name { get; set; } = "Memory";

    /// <summary>Gets or sets the minimum level; entries below this level are dropped.</summary>
    public LogLevel MinLevel { get; set; }

    /// <summary>
    /// Gets a snapshot of retained entries in insertion order (oldest first).
    /// When the buffer is full, the oldest entries are overwritten and will not appear here.
    /// </summary>
    public IReadOnlyList<LogEntry> Entries
    {
        get
        {
            lock (_lock)
            {
                LogEntry[] snapshot = new LogEntry[_count];
                for (int i = 0; i < _count; i++)
                {
                    int idx = (_writeIndex - _count + i + _ring.Length) % _ring.Length;
                    snapshot[i] = _ring[idx];
                }
                return snapshot;
            }
        }
    }

    /// <inheritdoc/>
    public void Write(LogEntry entry)
    {
        if (entry.Level < MinLevel) return;
        lock (_lock)
        {
            if (_count == _ring.Length) _droppedCount++;
            _ring[_writeIndex] = entry;
            _writeIndex = (_writeIndex + 1) % _ring.Length;
            if (_count < _ring.Length) _count++;
        }
    }

    /// <summary>
    /// How many entries the ring overwrote before a caller could read them. A full buffer that
    /// never wrapped reads the same as one that dropped thousands, so a gate has to check this.
    /// </summary>
    public long DroppedCount
    {
        get
        {
            lock (_lock) { return _droppedCount; }
        }
    }

    /// <summary>Clears all retained entries and resets the ring buffer.</summary>
    public void Clear()
    {
        lock (_lock)
        {
            Array.Clear(_ring, 0, _ring.Length);
            _writeIndex = 0;
            _count = 0;
            _droppedCount = 0;
        }
    }

    /// <inheritdoc/>
    public void Flush() { }

    /// <inheritdoc/>
    public void Dispose() { }
}
