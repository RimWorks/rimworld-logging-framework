using System.IO;

namespace RimWorks.RimLogging.Settings;

/// <summary>Resolves and normalizes the log directory path under a base directory.</summary>
public static class LogDirectoryResolver
{
    /// <summary>Returns the "RimLogging" subdirectory under <paramref name="baseDir"/>, creating it if it does not exist.</summary>
    /// <param name="baseDir">The base directory to place the log folder under.</param>
    /// <returns>The full path to the ensured log directory.</returns>
    public static string EnsureUnderBase(string baseDir)
    {
        string dir = Path.Combine(baseDir, "RimLogging");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        return dir;
    }


    /// <summary>Returns <paramref name="current"/> if it is non-blank; otherwise falls back to the ensured directory under <paramref name="baseDir"/>.</summary>
    /// <param name="current">The currently configured directory, possibly empty.</param>
    /// <param name="baseDir">The base directory used to derive the fallback.</param>
    /// <returns>The normalized log directory path.</returns>
    public static string Normalize(string current, string baseDir)
    {
        if (!string.IsNullOrWhiteSpace(current)) return current;
        return EnsureUnderBase(baseDir);
    }
}
