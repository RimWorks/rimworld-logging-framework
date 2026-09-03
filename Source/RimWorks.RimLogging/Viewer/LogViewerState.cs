using System.Collections.Generic;

namespace RimWorks.RimLogging.Viewer;

internal sealed class LogViewerState
{
    public bool ChannelsOpen = true;
    public string ActiveChannel = AllChannels;

    public string ChannelFilter
    {
        get => LogViewerSession.ChannelFilter;
        set => LogViewerSession.ChannelFilter = value;
    }

    public bool[] Levels => LogViewerSession.Levels;

    public string DslSource
    {
        get => LogViewerSession.DslSource;
        set => LogViewerSession.DslSource = value;
    }

    public string? DslError
    {
        get => LogViewerSession.DslError;
        set => LogViewerSession.DslError = value;
    }

    public LogEntry? Selected = null;

    /// <summary>Set on the UI thread, cleared from the upload continuation, so the read must not be hoisted.</summary>
    public volatile bool Uploading = false;

    public readonly Dictionary<string, bool> ExpandedChannels = new Dictionary<string, bool>(System.StringComparer.Ordinal);

    public const string AllChannels = "*all*";

    public bool IsChannelExpanded(string id, int depth)
    {
        if (ExpandedChannels.TryGetValue(id, out bool value))
        {
            return value;
        }
        return depth < 2;
    }

    public void ToggleChannel(string id, int depth)
    {
        bool current = IsChannelExpanded(id, depth);
        ExpandedChannels[id] = !current;
    }
}

internal static class LogViewerSession
{
    public static readonly bool[] Levels = { false, false, true, true, true, true };
    public static string DslSource { get; set; } = "";
    public static string ChannelFilter { get; set; } = "";
    public static string? DslError { get; set; }
}
