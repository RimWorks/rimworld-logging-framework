using System;

namespace RimWorks.RimLogging.Sinks;

/// <summary>
/// Shared file lifecycle for the rolling sinks: per-session stream, retention, serialised
/// writes. Subclasses supply only <see cref="Name"/> and <see cref="FormatLine"/>.
/// </summary>
public abstract class RollingFileSink : ILogSink
{
    private readonly System.IO.StreamWriter _writer;
    private readonly object _lock = new object();
    private bool _disposed;

    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <summary>Gets or sets the minimum level; entries below this level are dropped.</summary>
    public LogLevel MinLevel { get; set; }

    /// <summary>Gets the full path of the log file created for this session.</summary>
    public string FilePath { get; }

    /// <summary>
    /// Opens the session file (creating <paramref name="logDirectory"/> if absent) after
    /// pruning older files of the same extension beyond <paramref name="retainCount"/>.
    /// </summary>
    /// <param name="logDirectory">Directory in which log files are created; created if absent.</param>
    /// <param name="retainCount">Maximum number of log files to keep; oldest beyond this count are deleted.</param>
    /// <param name="minLevel">Entries below this level are silently dropped.</param>
    /// <param name="extension">File extension without the leading dot (e.g. <c>log</c>, <c>ndjson</c>).</param>
    protected RollingFileSink(string logDirectory, int retainCount, LogLevel minLevel, string extension)
    {
        if (!System.IO.Directory.Exists(logDirectory))
            System.IO.Directory.CreateDirectory(logDirectory);

        ApplyRetention(logDirectory, retainCount, extension);

        string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
        FilePath = System.IO.Path.Combine(logDirectory,
            $"RimLogging-{stamp}-{System.Diagnostics.Process.GetCurrentProcess().Id}.{extension}");
        System.IO.FileStream fs = new System.IO.FileStream(FilePath, System.IO.FileMode.Append,
            System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite);
        _writer = new System.IO.StreamWriter(fs) { AutoFlush = false };
        MinLevel = minLevel;
    }

    /// <summary>Renders a single entry into the line written to the file.</summary>
    protected abstract string FormatLine(LogEntry entry);

    /// <inheritdoc/>
    public void Write(LogEntry entry)
    {
        if (entry.Level < MinLevel) return;
        if (_disposed) return;
        string line = FormatLine(entry);
        lock (_lock)
        {
            if (_disposed) return;
            _writer.WriteLine(line);
            if (entry.Level >= LogLevel.Error) _writer.Flush();
        }
    }

    /// <inheritdoc/>
    public void Flush()
    {
        lock (_lock) { if (!_disposed) _writer.Flush(); }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Flushes and closes the session writer. Idempotent and thread-safe.</summary>
    /// <param name="disposing"><c>true</c> when called from <see cref="Dispose()"/>; the sink holds only managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            try { _writer.Flush(); _writer.Dispose(); }
            catch
            {
                // Best-effort close during shutdown; the OS reclaims the file handle regardless.
            }
        }
    }

    private static void ApplyRetention(string dir, int retain, string extension)
    {
        string[] files = System.IO.Directory.GetFiles(dir, $"RimLogging-*.{extension}");
        if (files.Length <= retain) return;
        Array.Sort(files, (string a, string b) => string.Compare(a, b, StringComparison.Ordinal));
        for (int i = 0; i < files.Length - retain; i++)
        {
            try { System.IO.File.Delete(files[i]); }
            catch
            {
                // A locked or already-removed old log isn't fatal; skip it and keep pruning.
            }
        }
    }
}
