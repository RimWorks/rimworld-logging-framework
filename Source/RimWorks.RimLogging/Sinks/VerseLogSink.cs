using RimWorks.RimLogging.Channels;
using RimWorks.RimLogging.Format;

namespace RimWorks.RimLogging.Sinks;

/// <summary>
/// Writes log entries back into vanilla's <c>Verse.Log</c> buffer, wrapping the
/// prefix in a <c>&lt;color=#RRGGBB&gt;</c> rich-text tag sourced from the channel's
/// <see cref="ChannelDef.ColorHex"/> or, when absent, from
/// <see cref="SeverityColors.GetHex"/>.
/// </summary>
/// <remarks>
/// The actual writeback into <c>Verse.Log</c> is delegated through
/// <see cref="VanillaWriter"/>, which the Hijack layer wires at startup. This
/// keeps the <c>Sinks</c> module free of any dependency on <c>Hijack</c>.
/// This file lives in <c>Sinks/</c> and is excluded from test projects that
/// cannot reference Verse.
/// </remarks>
public sealed class VerseLogSink : ILogSink
{
    /// <summary>
    /// Writer seam invoked to push the formatted line back into the vanilla
    /// <c>Verse.Log</c> buffer. Wired by the Hijack layer at startup; <c>null</c>
    /// when hijack installation was skipped (degraded mode), in which case writes
    /// are silently dropped.
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
        string prefix = DefaultFormat.RenderPrefixOnly(FormatTemplate, entry, stripRichText: false);
        string colored = "<color=#" + colorHex + ">" + prefix + "</color> " + entry.RenderedMessage;

        VanillaWriter?.Invoke(entry.Level, colored);
    }

    /// <inheritdoc/>
    public void Flush() { }

    /// <inheritdoc/>
    public void Dispose() { }
}
