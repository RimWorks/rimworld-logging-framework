using Xunit;

namespace RimWorks.RimLogging.Tests.Bundle;

public class ManifestVersionReaderTests : IDisposable
{
    private readonly string _tempRoot;

    public ManifestVersionReaderTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "rimlog-mvr-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, recursive: true); }
        catch { }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void TryGetVersion_MissingDir_ReturnsNull()
    {
        string nonexistent = Path.Combine(_tempRoot, "absent");
        Assert.Null(RimWorks.RimLogging.Bundle.ManifestVersionReader.TryGetVersion(nonexistent));
    }

    [Fact]
    public void TryGetVersion_MissingManifest_ReturnsNull()
    {
        Directory.CreateDirectory(Path.Combine(_tempRoot, "About"));
        Assert.Null(RimWorks.RimLogging.Bundle.ManifestVersionReader.TryGetVersion(_tempRoot));
    }

    [Fact]
    public void TryGetVersion_ManifestWithVersion_ReturnsValue()
    {
        Directory.CreateDirectory(Path.Combine(_tempRoot, "About"));
        File.WriteAllText(
            Path.Combine(_tempRoot, "About", "Manifest.xml"),
            "<Manifest><version>1.2.3</version></Manifest>"
        );
        Assert.Equal("1.2.3", RimWorks.RimLogging.Bundle.ManifestVersionReader.TryGetVersion(_tempRoot));
    }

    [Fact]
    public void TryGetVersion_ManifestWithoutVersionElement_ReturnsNull()
    {
        Directory.CreateDirectory(Path.Combine(_tempRoot, "About"));
        File.WriteAllText(
            Path.Combine(_tempRoot, "About", "Manifest.xml"),
            "<Manifest><identifier>foo</identifier></Manifest>"
        );
        Assert.Null(RimWorks.RimLogging.Bundle.ManifestVersionReader.TryGetVersion(_tempRoot));
    }

    [Fact]
    public void TryGetVersion_MalformedXml_ReturnsNull()
    {
        Directory.CreateDirectory(Path.Combine(_tempRoot, "About"));
        File.WriteAllText(
            Path.Combine(_tempRoot, "About", "Manifest.xml"),
            "<broken xml"
        );
        Assert.Null(RimWorks.RimLogging.Bundle.ManifestVersionReader.TryGetVersion(_tempRoot));
    }
}
