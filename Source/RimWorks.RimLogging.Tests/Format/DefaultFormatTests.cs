using System;
using System.Collections.Generic;
using RimWorks.RimLogging;
using RimWorks.RimLogging.Capture;
using RimWorks.RimLogging.Format;
using Xunit;

namespace RimWorks.RimLogging.Tests.Format;

public class DefaultFormatTests
{
    private static LogEntry MakeEntry(
        string messageTemplate = "msg",
        string renderedMessage = "msg",
        LogLevel level = LogLevel.Info,
        string channel = "default",
        IReadOnlyDictionary<string, object?>? context = null,
        SourceLocation source = default,
        DateTime timestamp = default)
    {
        DateTime ts = timestamp == default ? new DateTime(2025, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc) : timestamp;
        return new LogEntry
        {
            Timestamp = ts,
            Level = level,
            Channel = channel,
            MessageTemplate = messageTemplate,
            RenderedMessage = renderedMessage,
            Context = context,
            Source = source,
            StackTrace = null,
            Exception = null,
        };
    }

    [Fact]
    public void Render_TsToken_FormatsTimestamp()
    {
        LogEntry entry = MakeEntry(timestamp: new DateTime(2025, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc));

        string result = DefaultFormat.Render("{ts}", entry, stripRichText: false);

        Assert.Equal("2025-01-02 03:04:05.678", result);
    }

    [Fact]
    public void Render_LevelToken_UpperCase()
    {
        LogEntry entry = MakeEntry(level: LogLevel.Warn);

        string result = DefaultFormat.Render("{level}", entry, stripRichText: false);

        Assert.Equal("WARN", result);
    }

    [Fact]
    public void Render_ChannelToken_ReturnsChannel()
    {
        LogEntry entry = MakeEntry(channel: "combat");

        string result = DefaultFormat.Render("{channel}", entry, stripRichText: false);

        Assert.Equal("combat", result);
    }

    [Fact]
    public void Render_SourceToken_CallerProvided_ReturnsFileColonLine()
    {
        SourceLocation source = new SourceLocation("MyClass.cs", 42, "MyMethod");
        LogEntry entry = MakeEntry(source: source);

        string result = DefaultFormat.Render("{source}", entry, stripRichText: false);

        Assert.Equal("MyClass.cs:42", result);
    }

    [Fact]
    public void Render_SourceToken_NotCallerProvided_ReturnsEmpty()
    {
        // an unknown source renders empty, not "?:0"
        LogEntry entry = MakeEntry(source: SourceLocation.Empty);

        string result = DefaultFormat.Render("{source}", entry, stripRichText: false);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Render_SourceTokenWrappedInBrackets_NotCallerProvided_DropsBracketsAndTrailingSpace()
    {
        // the whole "[{source}] " group goes, so no empty "[] " is left behind
        LogEntry entry = MakeEntry(source: SourceLocation.Empty);

        string result = DefaultFormat.Render("[{source}] {message}", entry, stripRichText: false);

        Assert.Equal(entry.RenderedMessage, result);
    }

    [Fact]
    public void Render_SourceTokenWrappedInBrackets_CallerProvided_KeepsBrackets()
    {
        LogEntry entry = MakeEntry(source: new SourceLocation("MyMod/Foo", 42, null));

        string result = DefaultFormat.Render("[{source}] {message}", entry, stripRichText: false);

        Assert.Equal($"[MyMod/Foo:42] {entry.RenderedMessage}", result);
    }

    [Fact]
    public void Render_MessageToken_StripFalse_PreservesRichText()
    {
        LogEntry entry = MakeEntry(renderedMessage: "<color=red>error</color>");

        string result = DefaultFormat.Render("{message}", entry, stripRichText: false);

        Assert.Equal("<color=red>error</color>", result);
    }

    [Fact]
    public void Render_MessageToken_StripTrue_RemovesRichText()
    {
        LogEntry entry = MakeEntry(renderedMessage: "<color=red>error</color>");

        string result = DefaultFormat.Render("{message}", entry, stripRichText: true);

        Assert.Equal("error", result);
    }

    [Fact]
    public void Render_CtxToken_NoContext_ReturnsEmpty()
    {
        LogEntry entry = MakeEntry(context: null);

        string result = DefaultFormat.Render("{ctx}", entry, stripRichText: false);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Render_CtxToken_AllKeysConsumedByTemplate_ReturnsEmpty()
    {
        Dictionary<string, object?> ctx = new Dictionary<string, object?> { ["Name"] = "Bob" };
        LogEntry entry = MakeEntry(
            messageTemplate: "hello {Name}",
            renderedMessage: "hello Bob",
            context: ctx);

        string result = DefaultFormat.Render("{ctx}", entry, stripRichText: false);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Render_CtxToken_UnconsumedKeys_RendersKeyValuePairs()
    {
        Dictionary<string, object?> ctx = new Dictionary<string, object?> { ["pawn"] = "Bob", ["hp"] = 5 };
        LogEntry entry = MakeEntry(
            messageTemplate: "died",
            renderedMessage: "died",
            context: ctx);

        string result = DefaultFormat.Render("{ctx}", entry, stripRichText: false);

        Assert.Contains("pawn=Bob", result);
        Assert.Contains("hp=5", result);
        Assert.StartsWith(" {", result);
        Assert.EndsWith("}", result);
    }

    [Fact]
    public void Render_CtxToken_PartiallyConsumed_OnlyUnconsumedAppear()
    {
        Dictionary<string, object?> ctx = new Dictionary<string, object?> { ["Name"] = "Alice", ["hp"] = 10 };
        LogEntry entry = MakeEntry(
            messageTemplate: "hello {Name}",
            renderedMessage: "hello Alice",
            context: ctx);

        string result = DefaultFormat.Render("{ctx}", entry, stripRichText: false);

        Assert.DoesNotContain("Name", result);
        Assert.Contains("hp=10", result);
    }

    [Fact]
    public void Render_UnknownToken_PassesThroughVerbatim()
    {
        LogEntry entry = MakeEntry();

        string result = DefaultFormat.Render("{nope}", entry, stripRichText: false);

        Assert.Equal("{nope}", result);
    }

    [Fact]
    public void Render_LiteralText_PassesThroughUnchanged()
    {
        LogEntry entry = MakeEntry();

        string result = DefaultFormat.Render("hello world", entry, stripRichText: false);

        Assert.Equal("hello world", result);
    }

    [Fact]
    public void Render_DefaultTemplate_EndToEnd()
    {
        SourceLocation source = new SourceLocation("Foo.cs", 10, "Bar");
        LogEntry entry = MakeEntry(
            messageTemplate: "died",
            renderedMessage: "player died",
            level: LogLevel.Info,
            channel: "combat",
            source: source,
            timestamp: new DateTime(2025, 6, 15, 12, 0, 0, 0, DateTimeKind.Utc));

        string result = DefaultFormat.Render(DefaultFormat.Default, entry, stripRichText: false);

        Assert.Equal("[2025-06-15 12:00:00.000] [INFO] [combat] [Foo.cs:10] player died", result);
    }

    [Fact]
    public void Render_DefaultTemplate_WithUnconsumedContext_AppendsCtxSuffix()
    {
        SourceLocation source = new SourceLocation("Foo.cs", 10, "Bar");
        Dictionary<string, object?> ctx = new Dictionary<string, object?> { ["pawn"] = "Bob", ["hp"] = 5 };
        LogEntry entry = MakeEntry(
            messageTemplate: "died",
            renderedMessage: "player died",
            level: LogLevel.Info,
            channel: "combat",
            source: source,
            context: ctx,
            timestamp: new DateTime(2025, 6, 15, 12, 0, 0, 0, DateTimeKind.Utc));

        string result = DefaultFormat.Render(DefaultFormat.Default, entry, stripRichText: false);

        Assert.StartsWith("[2025-06-15 12:00:00.000] [INFO] [combat] [Foo.cs:10] player died", result);
        Assert.Contains("pawn=Bob", result);
        Assert.Contains("hp=5", result);
    }

    [Fact]
    public void Default_Constant_HasExpectedValue()
    {
        Assert.Equal("[{ts}] [{level}] [{channel}] [{source}] {message}{ctx}{exc}", DefaultFormat.Default);
    }

    [Fact]
    public void Render_ExcToken_NullException_ReturnsEmpty()
    {
        LogEntry entry = MakeEntry();

        string result = DefaultFormat.Render("{exc}", entry, stripRichText: false);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Render_ExcToken_WithException_ReturnsNewlinePrefixedString()
    {
        InvalidOperationException ex = new InvalidOperationException("boom");
        LogEntry entry = new LogEntry
        {
            Timestamp = new DateTime(2025, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc),
            Level = LogLevel.Error,
            Channel = "default",
            MessageTemplate = "oops",
            RenderedMessage = "oops",
            Context = null,
            Source = SourceLocation.Empty,
            StackTrace = null,
            Exception = ex,
        };

        string result = DefaultFormat.Render("{exc}", entry, stripRichText: false);

        Assert.StartsWith("\n", result);
        Assert.Contains("InvalidOperationException", result);
        Assert.Contains("boom", result);
    }

    [Fact]
    public void Render_DefaultTemplate_WithException_AppendsExceptionBlock()
    {
        InvalidOperationException ex = new InvalidOperationException("boom");
        SourceLocation source = new SourceLocation("Foo.cs", 10, "Bar");
        LogEntry entry = new LogEntry
        {
            Timestamp = new DateTime(2025, 6, 15, 12, 0, 0, 0, DateTimeKind.Utc),
            Level = LogLevel.Error,
            Channel = "combat",
            MessageTemplate = "save failed",
            RenderedMessage = "save failed",
            Context = null,
            Source = source,
            StackTrace = null,
            Exception = ex,
        };

        string result = DefaultFormat.Render(DefaultFormat.Default, entry, stripRichText: false);

        Assert.StartsWith("[2025-06-15 12:00:00.000] [ERROR] [combat] [Foo.cs:10] save failed", result);
        Assert.Contains("\nSystem.InvalidOperationException: boom", result);
    }
}
