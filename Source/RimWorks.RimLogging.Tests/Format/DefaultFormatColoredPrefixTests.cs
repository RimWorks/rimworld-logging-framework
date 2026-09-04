using System;
using System.Collections.Generic;
using RimWorks.RimLogging;
using RimWorks.RimLogging.Capture;
using RimWorks.RimLogging.Format;
using Xunit;

namespace RimWorks.RimLogging.Tests.Format;

// VerseLogSink is excluded from the test compilation because it is Verse-bound, so the line it
// builds is assembled here instead and tested directly.
public class DefaultFormatColoredPrefixTests
{
    private static LogEntry MakeEntry(
        string renderedMessage = "the message",
        Exception? exception = null,
        IReadOnlyDictionary<string, object?>? context = null)
        => new LogEntry
        {
            Timestamp = new DateTime(2025, 6, 15, 12, 0, 0, 0, DateTimeKind.Utc),
            Level = LogLevel.Error,
            Channel = "default",
            MessageTemplate = "msg",
            RenderedMessage = renderedMessage,
            Context = context,
            Source = new SourceLocation("SuiteRunner.cs", 88, "Run"),
            StackTrace = null,
            Exception = exception,
        };

    [Fact]
    public void RenderWithColoredPrefix_WithAnException_KeepsTheExceptionText()
    {
        LogEntry entry = MakeEntry(
            renderedMessage: "pickle: suite runner error",
            exception: new InvalidOperationException("filter 'rings' matched no scenarios"));

        string line = DefaultFormat.RenderWithColoredPrefix(DefaultFormat.Default, entry, "FF0000");

        Assert.Contains("filter 'rings' matched no scenarios", line, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderWithColoredPrefix_WithContext_KeepsTheContext()
    {
        LogEntry entry = MakeEntry(context: new Dictionary<string, object?> { ["pawn"] = "Cassandra" });

        string line = DefaultFormat.RenderWithColoredPrefix(DefaultFormat.Default, entry, "FF0000");

        Assert.Contains("Cassandra", line, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderWithColoredPrefix_LeavesOneSpaceBeforeTheMessage()
    {
        LogEntry entry = MakeEntry(renderedMessage: "the message");

        string line = DefaultFormat.RenderWithColoredPrefix(DefaultFormat.Default, entry, "FF0000");

        Assert.DoesNotContain("  the message", line, StringComparison.Ordinal);
        Assert.Contains("</color>the message", line, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderWithColoredPrefix_ColorsThePrefixAndNotTheMessage()
    {
        LogEntry entry = MakeEntry(renderedMessage: "the message");

        string line = DefaultFormat.RenderWithColoredPrefix(DefaultFormat.Default, entry, "AABBCC");

        Assert.StartsWith("<color=#AABBCC>", line, StringComparison.Ordinal);
        Assert.EndsWith("the message", line, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderSuffixOnly_DefaultTemplate_ReturnsTheContextAndExceptionTokens()
    {
        LogEntry entry = MakeEntry(exception: new InvalidOperationException("boom"));

        string suffix = DefaultFormat.RenderSuffixOnly(DefaultFormat.Default, entry, stripRichText: false);

        Assert.Contains("boom", suffix, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderSuffixOnly_TemplateWithoutAMessageToken_ReturnsEmpty()
    {
        LogEntry entry = MakeEntry(exception: new InvalidOperationException("boom"));

        Assert.Equal(string.Empty, DefaultFormat.RenderSuffixOnly("[{level}]", entry, stripRichText: false));
    }
}
