using System;
using System.Collections.Generic;
using System.IO;
using RimWorks.RimLogging.Capture;
using RimWorks.RimLogging.Sinks;
using Xunit;

namespace RimWorks.RimLogging.Tests.Sinks;

public class NdjsonLogReaderTests : IDisposable
{
    private readonly string _tempDir;

    public NdjsonLogReaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
        GC.SuppressFinalize(this);
    }

    private static LogEntry FullyPopulated()
    {
        return new LogEntry
        {
            Timestamp = new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc),
            Level = LogLevel.Error,
            Channel = "Cosmere.Roshar",
            MessageTemplate = "bond formed with {Spren}",
            RenderedMessage = "bond formed with Syl",
            Context = new Dictionary<string, object?> { ["Spren"] = "Syl", ["count"] = 3 },
            Source = new SourceLocation("Surgebinding.cs", 42, "Bond"),
            StackTrace = "at Surgebinding.Bond()",
            Mod = "Stormlight",
            Tick = 999,
            Repeats = 7,
            PatchedBy = new[] { "a.mod", "b.mod" },
        };
    }

    private LogEntry RoundTrip(LogEntry entry)
    {
        RollingJsonFileSink sink = new RollingJsonFileSink(_tempDir);
        sink.Write(entry);
        sink.Dispose();
        return Assert.Single(NdjsonLogReader.ReadFile(sink.FilePath));
    }

    [Fact]
    public void RoundTrip_KeepsEveryFieldTheWriterEmits()
    {
        LogEntry original = FullyPopulated();

        LogEntry back = RoundTrip(original);

        Assert.Equal(original.Timestamp, back.Timestamp);
        Assert.Equal(original.Level, back.Level);
        Assert.Equal(original.Channel, back.Channel);
        Assert.Equal(original.MessageTemplate, back.MessageTemplate);
        Assert.Equal(original.RenderedMessage, back.RenderedMessage);
        Assert.Equal(original.StackTrace, back.StackTrace);
        Assert.Equal(original.Mod, back.Mod);
        Assert.Equal(original.Tick, back.Tick);
        Assert.Equal(original.Repeats, back.Repeats);
        Assert.Equal(original.PatchedBy, back.PatchedBy);
    }

    [Fact]
    public void RoundTrip_KeepsTheSourceFileAndLine()
    {
        LogEntry back = RoundTrip(FullyPopulated());

        Assert.Equal("Surgebinding.cs", back.Source.File);
        Assert.Equal(42, back.Source.Line);
        Assert.True(back.Source.IsCallerProvided);
    }

    [Fact]
    public void RoundTrip_KeepsContextValuesAndTheirTypes()
    {
        LogEntry back = RoundTrip(FullyPopulated());

        Assert.NotNull(back.Context);
        Assert.Equal("Syl", back.Context!["Spren"]);
        // numbers come back as long, not the int that went in; the value survives, the box type does not
        Assert.Equal(3L, Assert.IsType<long>(back.Context["count"]));
    }

    [Fact]
    public void RoundTrip_PatchedByNull_StaysNull()
    {
        LogEntry original = FullyPopulated() with { PatchedBy = null };

        Assert.Null(RoundTrip(original).PatchedBy);
    }

    [Fact]
    public void RoundTrip_PatchedByEmpty_StaysEmptyNotNull()
    {
        LogEntry original = FullyPopulated() with { PatchedBy = Array.Empty<string>() };

        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<string>>(RoundTrip(original).PatchedBy));
    }

    [Fact]
    public void RoundTrip_ExceptionArrivesAsContextBecauseItCannotBeRebuilt()
    {
        LogEntry original = FullyPopulated() with { Exception = new InvalidOperationException("boom") };

        LogEntry back = RoundTrip(original);

        Assert.Null(back.Exception);
        Assert.Equal("System.InvalidOperationException", back.Context!["exception_type"]);
        Assert.Equal("boom", back.Context["exception_message"]);
    }

    [Fact]
    public void ReadLines_TruncatedLastLine_KeepsEveryCompleteLineBeforeIt()
    {
        string good = "{\"level\":\"INFO\",\"channel\":\"a\",\"msg\":\"first\"}";
        string[] lines = { good, good, "{\"level\":\"INFO\",\"channel\":\"a\",\"ms" };

        Assert.Equal(2, NdjsonLogReader.ReadLines(lines).Count);
    }

    [Fact]
    public void ReadLines_BlankLines_AreSkipped()
    {
        string[] lines = { "", "   ", "{\"level\":\"WARN\",\"channel\":\"a\",\"msg\":\"kept\"}" };

        Assert.Single(NdjsonLogReader.ReadLines(lines));
    }

    [Fact]
    public void ReadLines_RowWithoutTheNewerKeys_StillParses()
    {
        // files written before mod, tick, repeats and patched were added to the row
        string[] lines = { "{\"ts\":\"2026-03-04T05:06:07.000Z\",\"level\":\"INFO\",\"channel\":\"a\",\"msg\":\"old\"}" };

        LogEntry entry = Assert.Single(NdjsonLogReader.ReadLines(lines));

        Assert.Equal("old", entry.RenderedMessage);
        Assert.Null(entry.Mod);
        Assert.Null(entry.Tick);
        Assert.Equal(1, entry.Repeats);
        Assert.Null(entry.PatchedBy);
    }

    [Fact]
    public void ReadLines_PastTheCap_KeepsTheNewestRows()
    {
        List<string> lines = new List<string>();
        for (int i = 0; i < 10; i++) lines.Add("{\"level\":\"INFO\",\"channel\":\"a\",\"msg\":\"" + i + "\"}");

        IReadOnlyList<LogEntry> read = NdjsonLogReader.ReadLines(lines, max: 3);

        Assert.Equal(3, read.Count);
        Assert.Equal("7", read[0].RenderedMessage);
        Assert.Equal("9", read[2].RenderedMessage);
    }
}
