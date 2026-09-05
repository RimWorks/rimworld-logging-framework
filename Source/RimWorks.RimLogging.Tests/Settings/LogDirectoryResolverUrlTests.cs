using System;
using System.IO;
using RimWorks.RimLogging.Settings;
using Xunit;

namespace RimWorks.RimLogging.Tests.Settings;

public class LogDirectoryResolverUrlTests
{
    [Fact]
    public void FolderUrl_PlainPath_IsAFileUrl()
    {
        string url = LogDirectoryResolver.FolderUrl(Path.Combine(Path.GetTempPath(), "rimlog"));

        Assert.StartsWith("file://", url, StringComparison.Ordinal);
        Assert.EndsWith("/rimlog", url, StringComparison.Ordinal);
    }

    [Fact]
    public void FolderUrl_PathWithSpaces_EscapesThem()
    {
        // the default directory is ".../RimWorld by Ludeon Studios/RimLogging", so this is the norm
        string url = LogDirectoryResolver.FolderUrl(
            Path.Combine(Path.GetTempPath(), "RimWorld by Ludeon Studios", "RimLogging"));

        Assert.Contains("RimWorld%20by%20Ludeon%20Studios", url, StringComparison.Ordinal);
        Assert.DoesNotContain(" ", url, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void FolderUrl_BlankPath_IsEmptySoTheCallerCanRefuse(string? directory)
    {
        Assert.Equal(string.Empty, LogDirectoryResolver.FolderUrl(directory!));
    }

    [Fact]
    public void FolderUrl_PathWithInvalidCharacters_IsEmptyRatherThanThrowing()
    {
        Assert.Equal(string.Empty, LogDirectoryResolver.FolderUrl("\0not a path"));
    }

    [Fact]
    public void FolderUrl_RelativePath_ResolvesAgainstTheWorkingDirectory()
    {
        string url = LogDirectoryResolver.FolderUrl("logs");

        Assert.StartsWith("file://", url, StringComparison.Ordinal);
        Assert.EndsWith("/logs", url, StringComparison.Ordinal);
    }
}
