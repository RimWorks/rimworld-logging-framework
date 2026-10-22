using System;
using System.IO;
using RimWorks.RimLogging.Bundle;
using Xunit;

namespace RimWorks.RimLogging.Tests.Bundle;

public class WorkshopIdReaderTests : IDisposable
{
    private readonly string root;

    public WorkshopIdReaderTests()
    {
        root = Path.Combine(Path.GetTempPath(), "rimlogging-workshop-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "About"));
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(root, recursive: true);
        }
        catch (IOException)
        {
        }

        GC.SuppressFinalize(this);
    }

    private void WriteId(string contents)
    {
        File.WriteAllText(Path.Combine(root, "About", "PublishedFileId.txt"), contents);
    }

    [Fact]
    public void ReadsTheIdAWorkshopModCarries()
    {
        WriteId("2009463077");

        Assert.Equal("2009463077", WorkshopIdReader.TryGetWorkshopId(root));
    }

    [Fact]
    public void TrimsTheTrailingNewlineSteamWrites()
    {
        WriteId("3733484696\r\n");

        Assert.Equal("3733484696", WorkshopIdReader.TryGetWorkshopId(root));
    }

    [Fact]
    public void ReturnsNullForAModWithNoSuchFile()
    {
        Assert.Null(WorkshopIdReader.TryGetWorkshopId(root));
    }

    [Fact]
    public void ReturnsNullForAnEmptyFile()
    {
        WriteId("   \n");

        Assert.Null(WorkshopIdReader.TryGetWorkshopId(root));
    }

    [Theory]
    [InlineData("not-an-id")]
    [InlineData("2009463077x")]
    [InlineData("-2009463077")]
    [InlineData("2009 463077")]
    public void ReturnsNullForAnythingButDigits(string contents)
    {
        WriteId(contents);

        Assert.Null(WorkshopIdReader.TryGetWorkshopId(root));
    }

    [Fact]
    public void ReturnsNullForAMissingRoot()
    {
        Assert.Null(WorkshopIdReader.TryGetWorkshopId(Path.Combine(root, "nothing-here")));
    }
}
