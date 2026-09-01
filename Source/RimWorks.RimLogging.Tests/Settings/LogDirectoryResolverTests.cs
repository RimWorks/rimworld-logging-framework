using System;
using System.IO;
using RimWorks.RimLogging.Settings;
using Xunit;

namespace RimWorks.RimLogging.Tests.Settings;

public sealed class LogDirectoryResolverTests : IDisposable
{
    private readonly string _tempBase;

    public LogDirectoryResolverTests()
    {
        _tempBase = Path.Combine(Path.GetTempPath(), "RimLogTest_" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempBase))
            Directory.Delete(_tempBase, recursive: true);
    }

    [Fact]
    public void EnsureUnderBase_CreatesRimLoggingSubdir_WhenBaseDoesNotExist()
    {
        string result = LogDirectoryResolver.EnsureUnderBase(_tempBase);

        Assert.True(Directory.Exists(result));
    }

    [Fact]
    public void EnsureUnderBase_CreatesRimLoggingSubdir_WhenBaseExistsButSubdirDoesNot()
    {
        Directory.CreateDirectory(_tempBase);

        string result = LogDirectoryResolver.EnsureUnderBase(_tempBase);

        Assert.True(Directory.Exists(result));
    }

    [Fact]
    public void EnsureUnderBase_IsIdempotent_WhenRimLoggingSubdirAlreadyExists()
    {
        string expected = Path.Combine(_tempBase, "RimLogging");
        Directory.CreateDirectory(expected);

        string result = LogDirectoryResolver.EnsureUnderBase(_tempBase);

        Assert.Equal(expected, result);
        Assert.True(Directory.Exists(result));
    }

    [Fact]
    public void EnsureUnderBase_ReturnsPathCombinedWithRimLogging()
    {
        string result = LogDirectoryResolver.EnsureUnderBase(_tempBase);

        Assert.Equal(Path.Combine(_tempBase, "RimLogging"), result);
    }


    [Fact]
    public void Normalize_ReturnsExistingValue_WhenCurrentNonEmpty()
    {
        string current = Path.Combine(_tempBase, "MyCustomDir");
        string result = LogDirectoryResolver.Normalize(current, _tempBase);
        Assert.Equal(current, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_FallsBackToEnsureUnderBase_WhenCurrentEmpty(string current)
    {
        string result = LogDirectoryResolver.Normalize(current, _tempBase);
        Assert.Equal(Path.Combine(_tempBase, "RimLogging"), result);
        Assert.True(Directory.Exists(result));
    }
}
