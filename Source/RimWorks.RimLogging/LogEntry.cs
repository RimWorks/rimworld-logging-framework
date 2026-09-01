using System;
using System.Collections.Generic;
using RimWorks.RimLogging.Capture;

namespace RimWorks.RimLogging;

/// <summary>
/// Immutable payload delivered to every sink. Carries both the unrendered message
/// template and the fully-rendered string so structured sinks can extract typed
/// fields from <see cref="Context"/> while text sinks can write
/// <see cref="RenderedMessage"/> directly.
/// </summary>
public sealed class LogEntry
{
    private readonly string _channel = string.Empty;
    private readonly string _messageTemplate = string.Empty;
    private readonly string _renderedMessage = string.Empty;

    /// <summary>Gets the UTC timestamp at which the log call was made.</summary>
    public DateTime Timestamp { get; init; }

    /// <summary>Gets the severity level of this entry.</summary>
    public LogLevel Level { get; init; }

    /// <summary>How many times this entry repeated back to back. 1 for a normal entry.</summary>
    public int Repeats { get; internal set; } = 1;

    /// <summary>Gets the channel (dot-separated category) this entry was emitted on. Never <c>null</c>.</summary>
    public string Channel
    {
        get => _channel;
        init => _channel = value ?? throw new ArgumentNullException(nameof(Channel));
    }

    /// <summary>
    /// Gets the unrendered message template, e.g. <c>"died at {Hp}hp"</c>.
    /// Structured sinks use this together with <see cref="Context"/> to extract
    /// typed fields. <c>null</c> assignments are normalized to <see cref="string.Empty"/>.
    /// </summary>
    public string MessageTemplate
    {
        get => _messageTemplate;
        init => _messageTemplate = value ?? string.Empty;
    }

    /// <summary>
    /// Gets the fully-rendered message string, e.g. <c>"died at 5hp"</c>.
    /// Text sinks write this value directly. <c>null</c> assignments are normalized
    /// to <see cref="string.Empty"/>.
    /// </summary>
    public string RenderedMessage
    {
        get => _renderedMessage;
        init => _renderedMessage = value ?? string.Empty;
    }

    /// <summary>
    /// Gets the structured context dictionary, or <c>null</c> when the call site
    /// did not supply an anonymous-object context.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Context { get; init; }

    /// <summary>Gets the source location captured at the call site.</summary>
    public SourceLocation Source { get; init; }

    /// <summary>
    /// Gets the stack trace string, or <c>null</c>. Populated eagerly on the
    /// sync-bypass path at <see cref="LogLevel.Error"/> and
    /// <see cref="LogLevel.Fatal"/> so the stack is captured before the call
    /// stack unwinds.
    /// </summary>
    public string? StackTrace { get; init; }

    /// <summary>
    /// Gets the exception associated with this entry, or <c>null</c> when the
    /// call site used a non-exception overload.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets the originating mod's display name (About.xml <c>&lt;name&gt;</c>), or <c>null</c>
    /// when the entry could not be attributed to a known mod.
    /// </summary>
    public string? Mod { get; init; }
}
