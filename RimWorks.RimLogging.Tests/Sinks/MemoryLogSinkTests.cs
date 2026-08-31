using System;
using RimWorks.RimLogging;
using RimWorks.RimLogging.Sinks;
using Xunit;

namespace RimWorks.RimLogging.Tests.Sinks;

public class MemoryLogSinkTests
{
    private static LogEntry MakeEntry(LogLevel level = LogLevel.Info, string message = "test")
    {
        return new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Channel = "test",
            MessageTemplate = message,
            RenderedMessage = message,
            Context = null,
            Source = default,
            StackTrace = null,
            Exception = null,
        };
    }

    [Fact]
    public void Write_BasicEntries_EntriesReturnInInsertionOrder()
    {
        MemoryLogSink sink = new MemoryLogSink();

        LogEntry a = MakeEntry(message: "a");
        LogEntry b = MakeEntry(message: "b");
        LogEntry c = MakeEntry(message: "c");

        sink.Write(a);
        sink.Write(b);
        sink.Write(c);

        System.Collections.Generic.IReadOnlyList<LogEntry> entries = sink.Entries;
        Assert.Equal(3, entries.Count);
        Assert.Same(a, entries[0]);
        Assert.Same(b, entries[1]);
        Assert.Same(c, entries[2]);
    }

    [Fact]
    public void Write_BelowMinLevel_EntryIsDropped()
    {
        MemoryLogSink sink = new MemoryLogSink(minLevel: LogLevel.Warn);

        sink.Write(MakeEntry(LogLevel.Info));
        sink.Write(MakeEntry(LogLevel.Warn, "kept"));

        System.Collections.Generic.IReadOnlyList<LogEntry> entries = sink.Entries;
        LogEntry kept = Assert.Single(entries);
        Assert.Equal("kept", kept.RenderedMessage);
    }

    [Fact]
    public void Write_RingBufferFull_OldestEntriesDropped()
    {
        MemoryLogSink sink = new MemoryLogSink(capacity: 3);

        for (int i = 1; i <= 5; i++)
            sink.Write(MakeEntry(message: i.ToString()));

        System.Collections.Generic.IReadOnlyList<LogEntry> entries = sink.Entries;
        Assert.Equal(3, entries.Count);
        Assert.Equal("3", entries[0].RenderedMessage);
        Assert.Equal("4", entries[1].RenderedMessage);
        Assert.Equal("5", entries[2].RenderedMessage);
    }

    [Fact]
    public void Clear_AfterWrites_EntriesIsEmpty()
    {
        MemoryLogSink sink = new MemoryLogSink();

        sink.Write(MakeEntry());
        sink.Write(MakeEntry());
        sink.Clear();

        Assert.Empty(sink.Entries);
    }

    [Fact]
    public void Constructor_ZeroCapacity_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MemoryLogSink(0));
    }

    [Fact]
    public void Constructor_NegativeCapacity_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MemoryLogSink(-1));
    }

    [Fact]
    public void Name_ReturnsMemory()
    {
        MemoryLogSink sink = new MemoryLogSink();
        Assert.Equal("Memory", sink.Name);
    }
}
