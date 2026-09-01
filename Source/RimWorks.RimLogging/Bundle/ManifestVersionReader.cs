namespace RimWorks.RimLogging.Bundle;

/// <summary>
/// Reads a mod's version string from its <c>About/Manifest.xml</c> file.
/// </summary>
public static class ManifestVersionReader
{
    /// <summary>
    /// Attempts to read the <c>version</c> element from <c>About/Manifest.xml</c> under the given mod root.
    /// Returns <c>null</c> if the file is missing or any error occurs while reading it.
    /// </summary>
    /// <param name="rootDir">The mod's root directory.</param>
    /// <returns>The manifest version string, or <c>null</c> if unavailable.</returns>
    public static string? TryGetVersion(string rootDir)
    {
        try
        {
            string path = System.IO.Path.Combine(rootDir, "About", "Manifest.xml");
            if (!System.IO.File.Exists(path)) return null;
            System.Xml.Linq.XDocument doc = System.Xml.Linq.XDocument.Load(path);
            System.Xml.Linq.XElement? v = doc.Root?.Element("version");
            return v?.Value;
        }
        catch
        {
            return null;
        }
    }
}
