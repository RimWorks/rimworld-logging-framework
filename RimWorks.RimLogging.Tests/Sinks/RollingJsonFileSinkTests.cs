using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RimWorks.RimLogging.Capture;
using RimWorks.RimLogging.Sinks;
using Xunit;

namespace RimWorks.RimLogging.Tests.Sinks;

public class RollingJsonFileSinkTests : IDisposable
{
    private readonly string _tempDir;

    public RollingJsonFileSinkTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        GC.SuppressFinalize(this);
    }

    private static LogEntry MakeEntry(
        LogLevel level = LogLevel.Info,
        string message = "test",
        string? template = null,
        IReadOnlyDictionary<string, object?>? context = null,
        SourceLocation source = default,
        Exception? exception = null,
        string? stackTrace = null)
    {
        return new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Channel = "test.channel",
            MessageTemplate = template ?? message,
            RenderedMessage = message,
            Context = context,
            Source = source,
            StackTrace = stackTrace,
            Exception = exception,
        };
    }

    [Fact]
    public void Constructor_DirectoryMissing_CreatesDirectory()
    {
        string missingDir = Path.Combine(_tempDir, "deep", "nested");

        using (RollingJsonFileSink sink = new RollingJsonFileSink(missingDir))
        {
            Assert.True(Directory.Exists(missingDir));
        }
    }

    [Fact]
    public void Write_SingleEntry_ProducesValidJsonLine()
    {
        RollingJsonFileSink sink = new RollingJsonFileSink(_tempDir);
        sink.Write(MakeEntry(LogLevel.Info, "hello world", template: "hello {Who}"));
        sink.Dispose();

        string[] lines = File.ReadAllLines(sink.FilePath);
        Assert.Single(lines);

        using JsonDocument doc = JsonDocument.Parse(lines[0]);
        JsonElement root = doc.RootElement;

        Assert.Equal("INFO", root.GetProperty("level").GetString());
        Assert.Equal("test.channel", root.GetProperty("channel").GetString());
        Assert.Equal("hello world", root.GetProperty("msg").GetString());
        Assert.Equal("hello {Who}", root.GetProperty("tmpl").GetString());
        Assert.EndsWith("Z", root.GetProperty("ts").GetString());
    }

    [Fact]
    public void Write_ThreeEntries_ProducesThreeSeparateJsonLines()
    {
        RollingJsonFileSink sink = new RollingJsonFileSink(_tempDir);
        sink.Write(MakeEntry(LogLevel.Info, "first"));
        sink.Write(MakeEntry(LogLevel.Warn, "second"));
        sink.Write(MakeEntry(LogLevel.Error, "third"));
        sink.Dispose();

        string[] lines = File.ReadAllLines(sink.FilePath);
        Assert.Equal(3, lines.Length);

        foreach (string line in lines)
        {
            using JsonDocument doc = JsonDocument.Parse(line);
            Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
        }
    }

    [Fact]
    public void Write_BelowMinLevel_EntryDropped()
    {
        RollingJsonFileSink sink = new RollingJsonFileSink(_tempDir, minLevel: LogLevel.Warn);
        sink.Write(MakeEntry(LogLevel.Info, "dropped"));
        sink.Dispose();

        string content = File.ReadAllText(sink.FilePath);
        Assert.DoesNotContain("dropped", content);
    }

    [Fact]
    public void Retention_OldFilesDeletedBeyondCount()
    {
        Directory.CreateDirectory(_tempDir);
        for (int i = 0; i < 7; i++)
        {
            string stamp = new DateTime(2020, 1, i + 1).ToString("yyyyMMdd-HHmmss");
            string fakePath = Path.Combine(_tempDir, $"RimLogging-{stamp}-999.ndjson");
            File.WriteAllText(fakePath, string.Empty);
        }

        // retainCount=5: oldest 2 deleted, leaving 5 old + 1 new session file
        RollingJsonFileSink sink = new RollingJsonFileSink(_tempDir, retainCount: 5);
        sink.Dispose();

        string[] remaining = Directory.GetFiles(_tempDir, "RimLogging-*.ndjson");
        Assert.Equal(6, remaining.Length);
    }

    [Fact]
    public void Write_ErrorLevel_FlushedImmediatelyWithoutExplicitFlush()
    {
        RollingJsonFileSink sink = new RollingJsonFileSink(_tempDir);
        sink.Write(MakeEntry(LogLevel.Error, "error-msg"));

        using (FileStream fs = new FileStream(sink.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 4096))
        using (StreamReader reader = new StreamReader(fs))
        {
            string content = reader.ReadToEnd();
            Assert.Contains("error-msg", content);
        }

        sink.Dispose();
    }

    [Fact]
    public void Dispose_CalledTwice_NoException()
    {
        RollingJsonFileSink sink = new RollingJsonFileSink(_tempDir);
        sink.Dispose();
        Exception? ex = Record.Exception((Action)(() => sink.Dispose()));
        Assert.Null(ex);
    }

    [Fact]
    public void Write_RichTextInMessage_TagsStrippedFromMsg()
    {
        RollingJsonFileSink sink = new RollingJsonFileSink(_tempDir);
        sink.Write(MakeEntry(LogLevel.Info, "<color=red>important</color>"));
        sink.Dispose();

        string[] lines = File.ReadAllLines(sink.FilePath);
        using JsonDocument doc = JsonDocument.Parse(lines[0]);
        string? msg = doc.RootElement.GetProperty("msg").GetString();

        Assert.Contains("important", msg);
        Assert.DoesNotContain("<color=red>", msg);
        Assert.DoesNotContain("</color>", msg);
    }

    [Fact]
    public void Write_WithException_ExcFieldContainsTypeMessageStack()
    {
        InvalidOperationException exception = new InvalidOperationException("boom");
        RollingJsonFileSink sink = new RollingJsonFileSink(_tempDir);
        sink.Write(MakeEntry(LogLevel.Error, "failed", exception: exception));
        sink.Dispose();

        string[] lines = File.ReadAllLines(sink.FilePath);
        using JsonDocument doc = JsonDocument.Parse(lines[0]);
        JsonElement exc = doc.RootElement.GetProperty("exc");

        Assert.Equal("System.InvalidOperationException", exc.GetProperty("type").GetString());
        Assert.Equal("boom", exc.GetProperty("message").GetString());
        // exc.stack may be null since we didn't throw it; just confirm the field exists
        Assert.True(exc.TryGetProperty("stack", out _));
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("exc").GetProperty("stack").ValueKind);
    }

    [Fact]
    public void Write_SourceNotCallerProvided_SrcIsJsonNull()
    {
        RollingJsonFileSink sink = new RollingJsonFileSink(_tempDir);
        sink.Write(MakeEntry(LogLevel.Info, "msg", source: SourceLocation.Empty));
        sink.Dispose();

        string[] lines = File.ReadAllLines(sink.FilePath);
        using JsonDocument doc = JsonDocument.Parse(lines[0]);
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("src").ValueKind);
    }

    [Fact]
    public void Write_SourceCallerProvided_SrcContainsFileAndLine()
    {
        SourceLocation source = new SourceLocation("Player.cs", 42, "Update");
        RollingJsonFileSink sink = new RollingJsonFileSink(_tempDir);
        sink.Write(MakeEntry(LogLevel.Info, "msg", source: source));
        sink.Dispose();

        string[] lines = File.ReadAllLines(sink.FilePath);
        using JsonDocument doc = JsonDocument.Parse(lines[0]);
        Assert.Equal("Player.cs:42", doc.RootElement.GetProperty("src").GetString());
    }

    [Fact]
    public void Write_NullContext_CtxIsJsonNull()
    {
        RollingJsonFileSink sink = new RollingJsonFileSink(_tempDir);
        sink.Write(MakeEntry(LogLevel.Info, "msg", context: null));
        sink.Dispose();

        string[] lines = File.ReadAllLines(sink.FilePath);
        using JsonDocument doc = JsonDocument.Parse(lines[0]);
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("ctx").ValueKind);
    }

    [Fact]
    public void Write_EmptyContext_CtxIsJsonNull()
    {
        IReadOnlyDictionary<string, object?> ctx = new Dictionary<string, object?>();
        RollingJsonFileSink sink = new RollingJsonFileSink(_tempDir);
        sink.Write(MakeEntry(LogLevel.Info, "msg", context: ctx));
        sink.Dispose();

        string[] lines = File.ReadAllLines(sink.FilePath);
        using JsonDocument doc = JsonDocument.Parse(lines[0]);
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("ctx").ValueKind);
    }

    [Fact]
    public void Write_WithContext_CtxPreservesAllEntries()
    {
        Dictionary<string, object?> ctx = new Dictionary<string, object?>
        {
            ["Hp"] = 5,
            ["Name"] = "colonist",
        };
        RollingJsonFileSink sink = new RollingJsonFileSink(_tempDir);
        sink.Write(MakeEntry(LogLevel.Info, "died at 5hp", template: "died at {Hp}hp", context: ctx));
        sink.Dispose();

        string[] lines = File.ReadAllLines(sink.FilePath);
        using JsonDocument doc = JsonDocument.Parse(lines[0]);
        JsonElement ctxEl = doc.RootElement.GetProperty("ctx");

        Assert.Equal(JsonValueKind.Object, ctxEl.ValueKind);
        Assert.Equal(5, ctxEl.GetProperty("Hp").GetInt32());
        Assert.Equal("colonist", ctxEl.GetProperty("Name").GetString());
    }
}
