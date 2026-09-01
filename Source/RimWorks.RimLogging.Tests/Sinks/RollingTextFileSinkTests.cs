using System;
using System.IO;
using RimWorks.RimLogging;
using RimWorks.RimLogging.Sinks;
using Xunit;

namespace RimWorks.RimLogging.Tests.Sinks;

public class RollingTextFileSinkTests : IDisposable
{
    private readonly string _tempDir;

    public RollingTextFileSinkTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        GC.SuppressFinalize(this);
    }

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
    public void Constructor_DirectoryMissing_CreatesDirectory()
    {
        string missingDir = Path.Combine(_tempDir, "deep", "nested");

        using (RollingTextFileSink sink = new RollingTextFileSink(missingDir))
        {
            Assert.True(Directory.Exists(missingDir));
        }
    }

    [Fact]
    public void Write_InfoEntry_ProducesLineInFile()
    {
        RollingTextFileSink sink = new RollingTextFileSink(_tempDir);
        sink.Write(MakeEntry(LogLevel.Info, "hello world"));
        sink.Dispose();

        string content = File.ReadAllText(sink.FilePath);
        Assert.Contains("hello world", content);
        Assert.Contains("INFO", content);
    }

    [Fact]
    public void Write_BelowMinLevel_EntryDropped()
    {
        RollingTextFileSink sink = new RollingTextFileSink(_tempDir, minLevel: LogLevel.Warn);
        sink.Write(MakeEntry(LogLevel.Info, "dropped"));
        sink.Dispose();

        string content = File.ReadAllText(sink.FilePath);
        Assert.DoesNotContain("dropped", content);
    }

    [Fact]
    public void Retention_OldFilesDeletedBeyondCount()
    {
        // Pre-create N+2 fake log files with predictable timestamps
        Directory.CreateDirectory(_tempDir);
        for (int i = 0; i < 7; i++)
        {
            string stamp = new DateTime(2020, 1, i + 1).ToString("yyyyMMdd-HHmmss");
            string fakePath = Path.Combine(_tempDir, $"RimLogging-{stamp}-999.log");
            File.WriteAllText(fakePath, string.Empty);
        }

        // retainCount=5: oldest 2 should be deleted, leaving 5 old + 1 new session file
        RollingTextFileSink sink = new RollingTextFileSink(_tempDir, retainCount: 5);
        sink.Dispose();

        string[] remaining = Directory.GetFiles(_tempDir, "RimLogging-*.log");
        // 5 retained old files + 1 new session file = 6 total
        Assert.Equal(6, remaining.Length);
    }

    [Fact]
    public void Write_ErrorLevel_FlushedImmediatelyWithoutExplicitFlush()
    {
        RollingTextFileSink sink = new RollingTextFileSink(_tempDir);
        sink.Write(MakeEntry(LogLevel.Error, "error-msg"));

        // Read the file while sink is still alive (FileShare.ReadWrite)
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
        RollingTextFileSink sink = new RollingTextFileSink(_tempDir);
        sink.Dispose();
        Exception ex = Record.Exception((Action)(() => sink.Dispose()));
        Assert.Null(ex);
    }

    [Fact]
    public void Write_RichTextInMessage_TagsStrippedFromOutput()
    {
        RollingTextFileSink sink = new RollingTextFileSink(_tempDir);
        sink.Write(MakeEntry(LogLevel.Info, "<color=red>important</color>"));
        sink.Dispose();

        string content = File.ReadAllText(sink.FilePath);
        Assert.Contains("important", content);
        Assert.DoesNotContain("<color=red>", content);
        Assert.DoesNotContain("</color>", content);
    }
}
