using RimWorks.RimLogging.Format;

namespace RimWorks.RimLogging.Sinks;

/// <summary>
/// Writes log entries to a per-session plaintext file, stripping rich text tags,
/// and deletes the oldest files beyond the configured retention count.
/// </summary>
public sealed class RollingTextFileSink : RollingFileSink
{
    /// <inheritdoc/>
    public override string Name => "RollingText";

    /// <summary>Gets or sets the format template used to render each log line.</summary>
    public string FormatTemplate { get; set; } = DefaultFormat.Default;

    /// <summary>
    /// Initializes a new <see cref="RollingTextFileSink"/>.
    /// </summary>
    /// <param name="logDirectory">Directory in which log files are created; created if absent.</param>
    /// <param name="retainCount">Maximum number of log files to keep; oldest beyond this count are deleted.</param>
    /// <param name="minLevel">Entries below this level are silently dropped.</param>
    public RollingTextFileSink(string logDirectory, int retainCount = 5, LogLevel minLevel = LogLevel.Trace)
        : base(logDirectory, retainCount, minLevel, "log")
    {
    }

    /// <inheritdoc/>
    protected override string FormatLine(LogEntry entry)
        => DefaultFormat.Render(FormatTemplate, entry, stripRichText: true);
}
