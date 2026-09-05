using System;
using System.IO;
using RimWorks.RimLogging.Sinks;
using Xunit;

namespace RimWorks.RimLogging.Tests.Sinks;

public class LogFileListTests : IDisposable
{
    private readonly string _tempDir;

    public LogFileListTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
        GC.SuppressFinalize(this);
    }

    private void Touch(string name) => File.WriteAllText(Path.Combine(_tempDir, name), string.Empty);

    [Fact]
    public void InDirectory_MissingDirectory_ReturnsNothing()
    {
        Assert.Empty(LogFileList.InDirectory(Path.Combine(_tempDir, "nope")));
    }

    [Fact]
    public void InDirectory_EmptyDirectory_ReturnsNothing()
    {
        Assert.Empty(LogFileList.InDirectory(_tempDir));
    }

    [Fact]
    public void InDirectory_OrdersNewestFirst()
    {
        Touch("RimLogging-20260101-100000-1.ndjson");
        Touch("RimLogging-20260304-050607-2.ndjson");
        Touch("RimLogging-20250101-100000-3.ndjson");

        var files = LogFileList.InDirectory(_tempDir);

        Assert.Equal(3, files.Count);
        Assert.Equal(new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc), files[0].Started);
        Assert.Equal(new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc), files[2].Started);
    }

    [Fact]
    public void InDirectory_IgnoresTextLogsAndUnrelatedFiles()
    {
        Touch("RimLogging-20260101-100000-1.ndjson");
        Touch("RimLogging-20260101-100000-1.log");
        Touch("notes.txt");

        Assert.Single(LogFileList.InDirectory(_tempDir));
    }

    [Fact]
    public void InDirectory_UnparseableName_SortsLastAndLabelsWithTheFileName()
    {
        Touch("RimLogging-20260101-100000-1.ndjson");
        Touch("RimLogging-garbage.ndjson");

        var files = LogFileList.InDirectory(_tempDir);

        Assert.Equal(2, files.Count);
        Assert.Equal("RimLogging-garbage.ndjson", files[1].Label);
    }

    [Theory]
    [InlineData("RimLogging-garbage.ndjson")]
    [InlineData("Other-20260101-100000-1.ndjson")]
    [InlineData("RimLogging-2026.ndjson")]
    public void StampOf_NamesThatDoNotFit_ReturnDefault(string name)
    {
        Assert.Equal(default, LogFileList.StampOf(name));
    }
}
