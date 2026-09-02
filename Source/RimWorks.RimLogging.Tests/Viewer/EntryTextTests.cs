using System;
using System.Collections.Generic;
using RimWorks.RimLogging;
using RimWorks.RimLogging.Viewer;
using Xunit;

namespace RimWorks.RimLogging.Tests.Viewer;

public class EntryTextTests
{
    private static LogEntry Entry(string message, string? stack = null, Exception? ex = null, IReadOnlyList<string>? patchedBy = null) =>
        new LogEntry
        {
            Timestamp = new DateTime(2026, 9, 1, 4, 31, 26, DateTimeKind.Utc),
            Level = LogLevel.Error,
            Channel = "Mod.Unknown",
            MessageTemplate = message,
            RenderedMessage = message,
            StackTrace = stack,
            Exception = ex,
            PatchedBy = patchedBy ?? Array.Empty<string>(),
        };

    [Fact]
    public void Trace_PrefersTheStackTraceWhenPresent()
    {
        Assert.Equal("at Foo.Bar()", EntryText.Trace(Entry("boom", stack: "at Foo.Bar()")));
    }

    [Fact]
    public void Trace_NoStackButAnException_FallsBackToTheException()
    {
        string trace = EntryText.Trace(Entry("boom", ex: new InvalidOperationException("nope")));

        Assert.Contains("nope", trace);
    }

    [Fact]
    public void Trace_NeitherPresent_IsEmptyRatherThanNull()
    {
        Assert.Equal(string.Empty, EntryText.Trace(Entry("boom")));
    }

    [Fact]
    public void WithStack_HasATrace_SeparatesMessageAndTraceByABlankLine()
    {
        Assert.Equal("boom\n\nat Foo.Bar()", EntryText.WithStack(Entry("boom", stack: "at Foo.Bar()")));
    }

    [Fact]
    public void WithStack_NoTrace_DoesNotLeaveTrailingBlankLines()
    {
        Assert.Equal("boom", EntryText.WithStack(Entry("boom")));
    }

    [Fact]
    public void Full_CarriesTheHeaderFieldsAndTheBody()
    {
        string text = EntryText.Full(Entry("boom", stack: "at Foo.Bar()"));

        Assert.Contains("Level: ERROR", text);
        Assert.Contains("Channel: Mod.Unknown", text);
        Assert.Contains("Timestamp: 2026-09-01 04:31:26.000", text);
        Assert.Contains("boom", text);
        Assert.Contains("at Foo.Bar()", text);
    }

    [Fact]
    public void Full_NoMod_OmitsTheModLineRatherThanPrintingItEmpty()
    {
        Assert.DoesNotContain("Mod:", EntryText.Full(Entry("boom")));
    }

    [Fact]
    public void Full_NoPatchedBy_OmitsThePatchedByLine()
    {
        Assert.DoesNotContain("Patched by:", EntryText.Full(Entry("boom")));
    }

    [Fact]
    public void Full_HasPatchedBy_ListsTheOwnersCommaSeparated()
    {
        string text = EntryText.Full(Entry("boom", patchedBy: ["mod.a", "mod.b"]));

        Assert.Contains("Patched by: mod.a, mod.b", text);
    }

    [Fact]
    public void Full_NoStack_DoesNotEndInBlankLines()
    {
        string text = EntryText.Full(Entry("boom"));

        Assert.EndsWith("boom", text);
    }
}
