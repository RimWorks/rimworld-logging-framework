using System;
using System.Collections.Generic;

namespace RimWorks.RimLogging.Channels;

/// <summary>
/// A channel's optional overrides of the global gates. Holds the raw nullable values rather than
/// resolved ones, so changing a global at runtime takes effect without invalidating any cache.
/// </summary>
internal readonly struct ChannelSettings
{
    /// <summary>A channel that overrides nothing and follows the globals.</summary>
    internal static readonly ChannelSettings Inherit = new ChannelSettings(null, null, null, null);

    /// <summary>Initializes the overrides for one channel.</summary>
    /// <param name="minLevel">The channel's minimum level, or <c>null</c> to follow the global.</param>
    /// <param name="captureStackAt">The level at or above which to capture a trace, or <c>null</c> to follow the global.</param>
    /// <param name="destinations">Names of the only sinks this channel writes to, or <c>null</c> for all of them.</param>
    /// <param name="format">A format template overriding the sink's own, or <c>null</c> to use the sink's.</param>
    internal ChannelSettings(
        LogLevel? minLevel,
        LogLevel? captureStackAt,
        IReadOnlyList<string>? destinations,
        string? format)
    {
        MinLevel = minLevel;
        CaptureStackAt = captureStackAt;
        Destinations = destinations != null && destinations.Count > 0 ? destinations : null;
        Format = string.IsNullOrEmpty(format) ? null : format;
    }

    /// <summary>The channel's minimum level override, or <c>null</c> when it follows the global.</summary>
    internal LogLevel? MinLevel { get; }

    /// <summary>The channel's stack-capture threshold, or <c>null</c> when it follows the global.</summary>
    internal LogLevel? CaptureStackAt { get; }

    /// <summary>The sinks this channel is restricted to, or <c>null</c> when it writes to all of them.</summary>
    internal IReadOnlyList<string>? Destinations { get; }

    /// <summary>The channel's format template override, or <c>null</c> when the sink's own is used.</summary>
    internal string? Format { get; }

    /// <summary>Whether this channel names the sinks it writes to.</summary>
    internal bool HasDestinations => Destinations != null;

    /// <summary>Returns the level this channel actually gates at.</summary>
    internal LogLevel MinLevelOr(LogLevel globalMin) => MinLevel ?? globalMin;

    /// <summary>Returns the template to render with, preferring the channel's own.</summary>
    internal string TemplateOr(string sinkTemplate) => Format ?? sinkTemplate;

    /// <summary>
    /// Whether an entry on this channel is allowed to reach the named sink. Case-insensitive,
    /// because these are hand-typed in XML.
    /// </summary>
    internal bool AllowsSink(string sinkName)
    {
        if (Destinations == null) return true;
        for (int i = 0; i < Destinations.Count; i++)
        {
            if (string.Equals(Destinations[i], sinkName, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    /// <summary>
    /// Whether an entry at this level captures a stack trace. The global switch wins outright, so
    /// turning it off stays an absolute kill switch no matter what a channel asks for.
    /// </summary>
    internal bool ShouldCaptureStack(LogLevel level, bool globalCapture)
        => globalCapture && (CaptureStackAt is null || level >= CaptureStackAt.Value);
}
