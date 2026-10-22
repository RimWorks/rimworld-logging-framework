namespace RimWorks.RimLogging.Bundle;

/// <summary>
/// Reads a mod's Steam Workshop id from its <c>About/PublishedFileId.txt</c> file, the same file
/// RimWorld reads. A mod that is not from the Workshop has no such file.
/// </summary>
public static class WorkshopIdReader
{
    /// <summary>Returns <c>null</c> for anything but a run of digits, so a hand-edited file cannot reach the bundle.</summary>
    /// <param name="rootDir">The mod's root directory.</param>
    /// <returns>The Workshop id, or <c>null</c> when the mod has none.</returns>
    public static string? TryGetWorkshopId(string rootDir)
    {
        try
        {
            string path = System.IO.Path.Combine(rootDir, "About", "PublishedFileId.txt");
            if (!System.IO.File.Exists(path)) return null;
            string id = System.IO.File.ReadAllText(path).Trim();
            if (id.Length == 0) return null;
            return System.Linq.Enumerable.Any(id, c => c < '0' || c > '9') ? null : id;
        }
        catch
        {
            return null;
        }
    }
}
