using RimWorks.RimLogging.Channels;
using UnityEngine;

namespace RimWorks.RimLogging.Viewer;

/// <summary>Colors the viewer draws levels and channel dots in.</summary>
internal static class LevelColors
{
    private static readonly Color TraceColor = new Color(0.541f, 0.561f, 0.580f);
    private static readonly Color DebugColor = new Color(0.498f, 0.659f, 0.788f);
    private static readonly Color InfoColor = new Color(0.863f, 0.863f, 0.863f);
    private static readonly Color WarnColor = new Color(0.910f, 0.820f, 0.290f);
    private static readonly Color ErrorColor = new Color(0.878f, 0.361f, 0.278f);

    private static readonly Color ChannelFallback = new Color(0.561f, 0.722f, 0.647f);

    public static Color For(LogLevel level)
    {
        switch (level)
        {
            case LogLevel.Trace:
                return TraceColor;
            case LogLevel.Debug:
                return DebugColor;
            case LogLevel.Info:
                return InfoColor;
            case LogLevel.Warn:
                return WarnColor;
            default:
                return ErrorColor;
        }
    }

    /// <summary>The channel's own <c>ChannelDef.color</c>, or a neutral fallback when it declares none.</summary>
    public static Color ForChannel(string channelName)
    {
        return ChannelRegistry.TryResolve(channelName)?.color ?? ChannelFallback;
    }
}
