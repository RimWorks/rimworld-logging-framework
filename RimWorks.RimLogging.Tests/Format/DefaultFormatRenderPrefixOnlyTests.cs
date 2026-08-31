using System;
using RimWorks.RimLogging;
using RimWorks.RimLogging.Capture;
using RimWorks.RimLogging.Format;
using Xunit;

namespace RimWorks.RimLogging.Tests.Format;

public class DefaultFormatRenderPrefixOnlyTests
{
    private static LogEntry MakeEntry(
        string renderedMessage = "the message",
        LogLevel level = LogLevel.Info,
        string channel = "default",
        SourceLocation source = default,
        DateTime timestamp = default)
    {
        DateTime ts = timestamp == default ? new DateTime(2025, 6, 15, 12, 0, 0, 0, DateTimeKind.Utc) : timestamp;
        return new LogEntry
        {
            Timestamp = ts,
            Level = level,
            Channel = channel,
            MessageTemplate = "msg",
            RenderedMessage = renderedMessage,
            Context = null,
            Source = source,
            StackTrace = null,
            Exception = null,
        };
    }

    [Fact]
    public void RenderPrefixOnly_DefaultTemplate_ReturnsEverythingBeforeMessage()
    {
        SourceLocation source = new SourceLocation("Foo.cs", 10, "Bar");
        LogEntry entry = MakeEntry(
            level: LogLevel.Info,
            channel: "combat",
            source: source,
            timestamp: new DateTime(2025, 6, 15, 12, 0, 0, 0, DateTimeKind.Utc));

        string result = DefaultFormat.RenderPrefixOnly(DefaultFormat.Default, entry, stripRichText: false);

        Assert.Equal("[2025-06-15 12:00:00.000] [INFO] [combat] [Foo.cs:10] ", result);
    }

    [Fact]
    public void RenderPrefixOnly_DefaultTemplate_DoesNotIncludeRenderedMessage()
    {
        LogEntry entry = MakeEntry(renderedMessage: "should not appear");

        string result = DefaultFormat.RenderPrefixOnly(DefaultFormat.Default, entry, stripRichText: false);

        Assert.DoesNotContain("should not appear", result);
    }

    [Fact]
    public void RenderPrefixOnly_DefaultTemplate_HasTrailingSpaceBeforeMessageToken()
    {
        LogEntry entry = MakeEntry();

        string result = DefaultFormat.RenderPrefixOnly(DefaultFormat.Default, entry, stripRichText: false);

        Assert.EndsWith(" ", result);
    }

    [Fact]
    public void RenderPrefixOnly_TemplateWithoutMessageToken_ReturnsFullRenderedTemplate()
    {
        LogEntry entry = MakeEntry(level: LogLevel.Warn, channel: "test");

        string result = DefaultFormat.RenderPrefixOnly("[{level}] [{channel}]", entry, stripRichText: false);

        Assert.Equal("[WARN] [test]", result);
    }

    [Fact]
    public void RenderPrefixOnly_TemplateWithTokensAfterMessage_StopsAtMessage()
    {
        LogEntry entry = MakeEntry(
            level: LogLevel.Error,
            channel: "x",
            timestamp: new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

        string result = DefaultFormat.RenderPrefixOnly("{level} {message} {channel}", entry, stripRichText: false);

        Assert.Equal("ERROR ", result);
        Assert.DoesNotContain("{channel}", result);
        Assert.DoesNotContain("x", result);
    }

    [Fact]
    public void RenderPrefixOnly_TsToken_ResolvesCorrectly()
    {
        LogEntry entry = MakeEntry(timestamp: new DateTime(2025, 3, 4, 5, 6, 7, 890, DateTimeKind.Utc));

        string result = DefaultFormat.RenderPrefixOnly("{ts} {message}", entry, stripRichText: false);

        Assert.Equal("2025-03-04 05:06:07.890 ", result);
    }

    [Fact]
    public void RenderPrefixOnly_LevelToken_ResolvesCorrectly()
    {
        LogEntry entry = MakeEntry(level: LogLevel.Fatal);

        string result = DefaultFormat.RenderPrefixOnly("{level} {message}", entry, stripRichText: false);

        Assert.Equal("FATAL ", result);
    }

    [Fact]
    public void RenderPrefixOnly_ChannelToken_ResolvesCorrectly()
    {
        LogEntry entry = MakeEntry(channel: "combat");

        string result = DefaultFormat.RenderPrefixOnly("{channel} {message}", entry, stripRichText: false);

        Assert.Equal("combat ", result);
    }

    [Fact]
    public void RenderPrefixOnly_SourceToken_ResolvesCorrectly()
    {
        SourceLocation source = new SourceLocation("Bar.cs", 99, "Baz");
        LogEntry entry = MakeEntry(source: source);

        string result = DefaultFormat.RenderPrefixOnly("{source} {message}", entry, stripRichText: false);

        Assert.Equal("Bar.cs:99 ", result);
    }
}
