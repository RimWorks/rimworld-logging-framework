using System.Collections.Generic;
using RimWorks.RimLogging.Filtering;
using LogEntry = RimWorks.RimLogging.LogEntry;

namespace RimWorks.RimLogging.Viewer;

internal static class LogFilter
{
    public static List<LogChannel> BuildChannels(IReadOnlyList<LogEntry> snapshot, LogViewerState state, ChannelLabels labels)
    {
        return BuildChannels(TallyByChannel(snapshot), snapshot.Count, state, labels);
    }

    /// <summary>Builds the tree from tallies the sink already keeps, so it never walks the entries.</summary>
    public static List<LogChannel> BuildChannels(
        IReadOnlyDictionary<string, ChannelTally> perChannel, int totalRows, LogViewerState state, ChannelLabels labels)
    {
        ChannelClassifier.EnsureBuilt();

        Dictionary<string, NodeAccum> nodes = AccumulateNodes(perChannel);

        string channelFilter = state.ChannelFilter;
        bool hasFilter = !string.IsNullOrEmpty(channelFilter);
        HashSet<string>? keepIds = hasFilter ? ComputeKeepIds(nodes, channelFilter.ToLowerInvariant()) : null;

        HashSet<string> hasChildrenSet = ComputeHasChildren(nodes);
        List<string> sortedIds = SortIds(nodes);

        List<LogChannel> result = new List<LogChannel>(sortedIds.Count + 1) {
            new LogChannel(
                LogViewerState.AllChannels,
                labels.All,
                totalRows,
                0,
                false,
                false,
                true
            ),
        };

        AppendVisibleChannels(result, sortedIds, nodes, state, hasChildrenSet, keepIds, labels);
        return result;
    }

    /// <summary>Rolls a snapshot up per channel. Only used when no tally is supplied.</summary>
    internal static Dictionary<string, ChannelTally> TallyByChannel(IReadOnlyList<LogEntry> snapshot)
    {
        Dictionary<string, ChannelTally> perChannel = new Dictionary<string, ChannelTally>(System.StringComparer.Ordinal);
        for (int i = 0; i < snapshot.Count; i++)
        {
            LogEntry entry = snapshot[i];
            perChannel.TryGetValue(KeyFor(entry.Channel), out ChannelTally tally);
            tally.Count++;
            if (entry.Level >= LogLevel.Error)
            {
                tally.ErrorCount++;
            }
            perChannel[KeyFor(entry.Channel)] = tally;
        }
        return perChannel;
    }

    /// <summary>Channel key used by the tree. Empty channels group under "(root)".</summary>
    internal static string KeyFor(string? channel)
    {
        return string.IsNullOrEmpty(channel) ? "(root)" : channel!;
    }

    // PathFor allocates and each depth needs a string.Join, so this walks the handful of
    // distinct channels rather than every entry.
    private static Dictionary<string, NodeAccum> AccumulateNodes(IReadOnlyDictionary<string, ChannelTally> perChannel)
    {
        Dictionary<string, NodeAccum> nodes = new Dictionary<string, NodeAccum>(System.StringComparer.Ordinal);
        foreach (KeyValuePair<string, ChannelTally> channelTally in perChannel)
        {
            string[] path = ChannelClassifier.PathFor(channelTally.Key);
            int count = channelTally.Value.Count;
            bool isError = channelTally.Value.ErrorCount > 0;

            for (int d = 1; d <= path.Length; d++)
            {
                string id = string.Join("/", path, 0, d);
                if (!nodes.TryGetValue(id, out NodeAccum acc))
                {
                    acc = new NodeAccum
                    {
                        Id = id,
                        Name = path[d - 1],
                        Depth = d - 1,
                        Count = 0,
                        HasError = false,
                    };
                }
                acc.Count += count;
                if (isError)
                {
                    acc.HasError = true;
                }
                nodes[id] = acc;
            }
        }
        return nodes;
    }

    private static HashSet<string> ComputeKeepIds(Dictionary<string, NodeAccum> nodes, string filterLower)
    {
        HashSet<string> keepIds = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (string id in nodes.Keys)
        {
            if (id.ToLowerInvariant().IndexOf(filterLower, System.StringComparison.Ordinal) < 0)
            {
                continue;
            }
            string[] parts = id.Split('/');
            for (int d = 1; d <= parts.Length; d++)
            {
                keepIds.Add(string.Join("/", parts, 0, d));
            }
        }
        return keepIds;
    }

    private static HashSet<string> ComputeHasChildren(Dictionary<string, NodeAccum> nodes)
    {
        HashSet<string> hasChildrenSet = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (string id in nodes.Keys)
        {
            int sep = id.LastIndexOf('/');
            if (sep > 0)
            {
                hasChildrenSet.Add(id.Substring(0, sep));
            }
        }
        return hasChildrenSet;
    }

    private static List<string> SortIds(Dictionary<string, NodeAccum> nodes)
    {
        List<string> sortedIds = new List<string>(nodes.Keys);
        sortedIds.Sort(static (a, b) =>
        {
            int ra = TopRank(a);
            int rb = TopRank(b);
            if (ra != rb)
            {
                return ra - rb;
            }
            return string.Compare(a, b, System.StringComparison.OrdinalIgnoreCase);
        });
        return sortedIds;
    }

    private static void AppendVisibleChannels(
        List<LogChannel> result,
        List<string> sortedIds,
        Dictionary<string, NodeAccum> nodes,
        LogViewerState state,
        HashSet<string> hasChildrenSet,
        HashSet<string>? keepIds,
        ChannelLabels labels)
    {
        string modLabel = labels.ModGroup;
        string vanillaLabel = labels.VanillaGroup;

        HashSet<string> visible = new HashSet<string>(System.StringComparer.Ordinal);
        for (int i = 0; i < sortedIds.Count; i++)
        {
            string id = sortedIds[i];
            if (keepIds != null && !keepIds.Contains(id))
            {
                continue;
            }
            NodeAccum acc = nodes[id];

            int sep = id.LastIndexOf('/');
            string? parentId = sep > 0 ? id.Substring(0, sep) : null;

            if (!AreAncestorsVisible(parentId, acc, keepIds != null, visible, state))
            {
                continue;
            }

            visible.Add(id);

            bool hasChildren = hasChildrenSet.Contains(id);
            bool expanded = state.IsChannelExpanded(id, acc.Depth);
            string displayName = DisplayNameFor(acc, modLabel, vanillaLabel);
            result.Add(new LogChannel(id, displayName, acc.Count, acc.Depth, acc.HasError, hasChildren, expanded));
        }
    }

    private static bool AreAncestorsVisible(string? parentId, NodeAccum acc, bool hasFilter, HashSet<string> visible, LogViewerState state)
    {
        if (parentId == null)
        {
            return true;
        }
        if (hasFilter)
        {
            return true;
        }
        int parentDepth = acc.Depth - 1;
        return visible.Contains(parentId) && state.IsChannelExpanded(parentId, parentDepth);
    }

    private static string DisplayNameFor(NodeAccum acc, string modLabel, string vanillaLabel)
    {
        if (acc.Depth != 0)
        {
            return acc.Name;
        }
        if (acc.Name == ChannelClassifier.ModGroupId)
        {
            return modLabel;
        }
        if (acc.Name == ChannelClassifier.VanillaGroupId)
        {
            return vanillaLabel;
        }
        return acc.Name;
    }

    public static List<LogEntry> Apply(IReadOnlyList<LogEntry> snapshot, LogViewerState state)
    {
        FilterExpression? dsl = null;
        bool useDsl = false;
        if (!string.IsNullOrEmpty(state.DslSource) && state.DslError == null)
        {
            useDsl = FilterExpression.TryParse(state.DslSource, out dsl, out _);
        }

        bool allChannels = state.ActiveChannel == LogViewerState.AllChannels;
        string activePrefix = allChannels ? string.Empty : state.ActiveChannel + "/";

        List<LogEntry> result = new List<LogEntry>(snapshot.Count);
        for (int i = 0; i < snapshot.Count; i++)
        {
            LogEntry entry = snapshot[i];
            if (Matches(entry, state, allChannels, activePrefix, useDsl, dsl))
            {
                result.Add(entry);
            }
        }

        return result;
    }

    private static bool Matches(LogEntry entry, LogViewerState state, bool allChannels, string activePrefix, bool useDsl, FilterExpression? dsl)
    {
        if (!allChannels && !ChannelMatches(entry, state, activePrefix))
        {
            return false;
        }

        int levelIndex = (int)entry.Level;
        if (levelIndex >= 0 && levelIndex < state.Levels.Length && !state.Levels[levelIndex])
        {
            return false;
        }

        if (useDsl && dsl != null && !dsl.Match(entry))
        {
            return false;
        }

        return true;
    }

    private static bool ChannelMatches(LogEntry entry, LogViewerState state, string activePrefix)
    {
        string channel = string.IsNullOrEmpty(entry.Channel) ? "(root)" : entry.Channel;
        string pathKey = ChannelClassifier.JoinPath(ChannelClassifier.PathFor(channel));
        return pathKey == state.ActiveChannel || pathKey.StartsWith(activePrefix, System.StringComparison.Ordinal);
    }

    private static int TopRank(string id)
    {
        if (id.StartsWith(ChannelClassifier.ModGroupId, System.StringComparison.Ordinal))
        {
            int len = ChannelClassifier.ModGroupId.Length;
            if (id.Length == len || id[len] == '/')
            {
                return 1;
            }
        }
        if (id.StartsWith(ChannelClassifier.VanillaGroupId, System.StringComparison.Ordinal))
        {
            int len = ChannelClassifier.VanillaGroupId.Length;
            if (id.Length == len || id[len] == '/')
            {
                return 2;
            }
        }
        return 1;
    }

    private struct NodeAccum
    {
        public string Id;
        public string Name;
        public int Depth;
        public int Count;
        public bool HasError;
    }
}
