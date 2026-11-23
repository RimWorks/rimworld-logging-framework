using System;
using System.IO;
using RimWorks.RimLogging;
using RimWorks.RimLogging.Channels;
using RimWorks.RimLogging.Format;
using RimWorks.RimLogging.Sinks;
using Xunit;

namespace RimWorks.RimLogging.Tests.Format;

public class DefaultFormatPrintLevelTests : IDisposable
{
    private const string Trace = "at Foo.Bar (Foo:12)";

    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public void Dispose()
    {
        Logging.ChannelSettingsProvider = null;
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        GC.SuppressFinalize(this);
    }

    private static LogEntry MakeEntry(LogLevel level, Exception? exception = null) => new LogEntry
    {
        Timestamp = new DateTime(2025, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc),
        Level = level,
        Channel = "test",
        MessageTemplate = "msg",
        RenderedMessage = "msg",
        Context = null,
        Source = default,
        StackTrace = Trace,
        Exception = exception,
    };

    [Fact]
    public void AtTheDefaultLevel_AnInfoEntryWithAStoredTrace_RendersAsOneLine()
    {
        string line = DefaultFormat.Render(DefaultFormat.Default, MakeEntry(LogLevel.Info), stripRichText: false);
        string colored = DefaultFormat.RenderWithColoredPrefix(DefaultFormat.Default, MakeEntry(LogLevel.Info), "FF0000");

        Assert.Equal("[2025-01-02 03:04:05.678] [INFO] [test] msg", line);
        Assert.DoesNotContain("\n", colored, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(LogLevel.Warn)]
    [InlineData(LogLevel.Error)]
    public void AtTheDefaultLevel_WarnAndErrorEntries_RenderTheirTrace(LogLevel level)
    {
        string line = DefaultFormat.Render(DefaultFormat.Default, MakeEntry(level), stripRichText: false);
        string colored = DefaultFormat.RenderWithColoredPrefix(DefaultFormat.Default, MakeEntry(level), "FF0000");

        Assert.EndsWith("\n" + Trace, line, StringComparison.Ordinal);
        Assert.EndsWith("\n" + Trace, colored, StringComparison.Ordinal);
    }

    [Fact]
    public void AtTrace_AnInfoEntry_RendersItsTrace()
    {
        string line = DefaultFormat.Render(DefaultFormat.Default, MakeEntry(LogLevel.Info), stripRichText: false, LogLevel.Trace);
        string colored = DefaultFormat.RenderWithColoredPrefix(DefaultFormat.Default, MakeEntry(LogLevel.Info), "FF0000", LogLevel.Trace);

        Assert.Equal("[2025-01-02 03:04:05.678] [INFO] [test] msg\n" + Trace, line);
        Assert.Equal("<color=#FF0000>[2025-01-02 03:04:05.678] [INFO] [test] </color>msg\n" + Trace, colored);
    }

    [Fact]
    public void AtFatal_AWarnEntryWithAnException_RendersTheExceptionAndItsTrace()
    {
        Exception thrown;
        try
        {
            throw new InvalidOperationException("boom");
        }
        catch (InvalidOperationException ex)
        {
            thrown = ex;
        }

        string line = DefaultFormat.Render(DefaultFormat.Default, MakeEntry(LogLevel.Warn, thrown), stripRichText: false, LogLevel.Fatal);

        Assert.Contains("\n" + thrown, line, StringComparison.Ordinal);
        Assert.Contains(nameof(AtFatal_AWarnEntryWithAnException_RendersTheExceptionAndItsTrace), line, StringComparison.Ordinal);
        Assert.DoesNotContain(Trace, line, StringComparison.Ordinal);
    }

    [Fact]
    public void AChannelFormatWithAStackToken_FollowsThePrintLevel()
    {
        Logging.ChannelSettingsProvider = _ => new ChannelSettings(null, null, null, "<{message}>{stack}");

        RollingTextFileSink sink = new RollingTextFileSink(_tempDir);
        sink.Write(MakeEntry(LogLevel.Info));
        sink.Write(MakeEntry(LogLevel.Error));
        sink.Dispose();

        string content = File.ReadAllText(sink.FilePath);
        Assert.Equal("<msg>" + Environment.NewLine + "<msg>\n" + Trace + Environment.NewLine, content);
    }
}
