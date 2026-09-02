namespace RimWorks.RimLogging.Channels;

/// <summary>
/// A channel's optional overrides of the global gates. Holds the raw nullable values rather than
/// resolved ones, so changing a global at runtime takes effect without invalidating any cache.
/// </summary>
internal readonly struct ChannelSettings
{
    /// <summary>A channel that overrides nothing and follows the globals.</summary>
    internal static readonly ChannelSettings Inherit = new ChannelSettings(null, null);

    /// <summary>Initializes the overrides for one channel.</summary>
    /// <param name="minLevel">The channel's minimum level, or <c>null</c> to follow the global.</param>
    /// <param name="captureStackAt">The level at or above which to capture a trace, or <c>null</c> to follow the global.</param>
    internal ChannelSettings(LogLevel? minLevel, LogLevel? captureStackAt)
    {
        MinLevel = minLevel;
        CaptureStackAt = captureStackAt;
    }

    /// <summary>The channel's minimum level override, or <c>null</c> when it follows the global.</summary>
    internal LogLevel? MinLevel { get; }

    /// <summary>The channel's stack-capture threshold, or <c>null</c> when it follows the global.</summary>
    internal LogLevel? CaptureStackAt { get; }

    /// <summary>Returns the level this channel actually gates at.</summary>
    internal LogLevel MinLevelOr(LogLevel globalMin) => MinLevel ?? globalMin;

    /// <summary>
    /// Whether an entry at this level captures a stack trace. The global switch wins outright, so
    /// turning it off stays an absolute kill switch no matter what a channel asks for.
    /// </summary>
    internal bool ShouldCaptureStack(LogLevel level, bool globalCapture)
        => globalCapture && (CaptureStackAt is null || level >= CaptureStackAt.Value);
}
