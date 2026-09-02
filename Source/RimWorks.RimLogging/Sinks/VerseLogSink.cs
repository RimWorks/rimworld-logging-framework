using RimWorks.RimLogging.Channels;
using RimWorks.RimLogging.Format;

namespace RimWorks.RimLogging.Sinks;

/// <summary>
/// Writes entries back into vanilla's <c>Verse.Log</c> buffer, colouring the prefix from
/// the channel's <see cref="ChannelDef.ColorHex"/> or <see cref="SeverityColors.GetHex"/>.
/// </summary>
/// <remarks>Goes through <see cref="VanillaWriter"/> so Sinks never references Hijack.</remarks>
public sealed class VerseLogSink : ILogSink
{
    /// <summary>
    /// Pushes the formatted line into vanilla's buffer. Null in degraded mode, so writes drop.
    /// </summary>
    internal static System.Action<LogLevel, string>? VanillaWriter { get; set; }

    /// <inheritdoc/>
    public string Name => "VerseLog";

    /// <summary>Gets or sets the minimum level; entries below this level are dropped.</summary>
    public LogLevel MinLevel { get; set; }

    /// <summary>Gets or sets the format template used to render the log prefix.</summary>
    public string FormatTemplate { get; set; } = DefaultFormat.Default;

    /// <summary>
    /// Initializes a new <see cref="VerseLogSink"/>.
    /// </summary>
    /// <param name="minLevel">Entries below this level are silently dropped. Defaults to <see cref="LogLevel.Trace"/>.</param>
    public VerseLogSink(LogLevel minLevel = LogLevel.Trace) { MinLevel = minLevel; }

    /// <inheritdoc/>
    public void Write(LogEntry entry)
    {
        if (entry.Level < MinLevel) return;

        ChannelDef? def = ChannelRegistry.TryResolve(entry.Channel);
        string colorHex = def?.ColorHex ?? SeverityColors.GetHex(entry.Level);
        string template = Logging.SettingsFor(entry.Channel).TemplateOr(FormatTemplate);
        string prefix = DefaultFormat.RenderPrefixOnly(template, entry, stripRichText: false);
        string colored = "<color=#" + colorHex + ">" + prefix + "</color> " + entry.RenderedMessage;

        VanillaWriter?.Invoke(entry.Level, colored);
    }

    /// <inheritdoc/>
    public void Flush() { }

    /// <inheritdoc/>
    public void Dispose() { }
}
