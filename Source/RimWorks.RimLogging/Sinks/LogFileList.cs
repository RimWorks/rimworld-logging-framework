using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace RimWorks.RimLogging.Sinks;

/// <summary>One prior session's log file, with the timestamp pulled out of its name.</summary>
public readonly struct LogFileEntry
{
    /// <summary>Full path to the ndjson file.</summary>
    public readonly string Path;

    /// <summary>When the session started, or <c>default</c> when the name did not parse.</summary>
    public readonly DateTime Started;

    /// <summary>What to show in the picker.</summary>
    public readonly string Label;

    internal LogFileEntry(string path, DateTime started, string label)
    {
        Path = path;
        Started = started;
        Label = label;
    }
}

/// <summary>Lists the ndjson logs left by earlier sessions, newest first.</summary>
public static class LogFileList
{
    private const string Prefix = "RimLogging-";
    private const string StampFormat = "yyyyMMdd-HHmmss";

    /// <summary>Files in the directory, newest first. A missing directory yields none.</summary>
    public static IReadOnlyList<LogFileEntry> InDirectory(string directory)
    {
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) return Array.Empty<LogFileEntry>();

        string[] paths = Directory.GetFiles(directory, Prefix + "*.ndjson");
        List<LogFileEntry> found = new List<LogFileEntry>(paths.Length);
        foreach (string path in paths) found.Add(Describe(path));

        // unparseable names sort to the bottom rather than the top, since default is DateTime.MinValue
        found.Sort((left, right) => right.Started.CompareTo(left.Started));
        return found;
    }

    /// <summary>Reads the session timestamp out of a file name, or <c>default</c> when it does not fit.</summary>
    public static DateTime StampOf(string fileName)
    {
        if (!fileName.StartsWith(Prefix, StringComparison.Ordinal)) return default;

        string rest = fileName.Substring(Prefix.Length);
        if (rest.Length < StampFormat.Length) return default;

        return DateTime.TryParseExact(rest.Substring(0, StampFormat.Length), StampFormat,
            CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out DateTime parsed)
            ? parsed
            : default;
    }

    private static LogFileEntry Describe(string path)
    {
        string name = System.IO.Path.GetFileName(path);
        DateTime stamp = StampOf(name);
        string label = stamp == default
            ? name
            : stamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
        return new LogFileEntry(path, stamp, label);
    }
}
