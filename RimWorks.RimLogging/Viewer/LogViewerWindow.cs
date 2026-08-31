using System;
using System.Collections.Generic;
using LudeonTK;
using RimWorld;
using RimWorks.RimLogging.Filtering;
using RimWorks.RimLogging.Settings;
using UnityEngine;
using Verse;

namespace RimWorks.RimLogging.Viewer;

/// <summary>Three-pane log viewer drawn in RimWorld's dev-window skin.</summary>
internal sealed class LogViewerWindow : EditWindow {
    private const float ToolbarHeight = 26f;
    private const float RowHeight = 22f;
    private const float SplitterThickness = 4f;
    private const float FilterStripHeight = 24f;
    private const float ErrorStripHeight = 16f;
    private const float ScrollbarWidth = 18f;
    private const float IndentPerDepth = 12f;
    private const float CountColumnWidth = 46f;
    private const float TimestampWidth = 74f;
    private const float ChannelColumnWidth = 156f;

    private static readonly LogLevel[] ToggleLevels = {
        LogLevel.Trace, LogLevel.Debug, LogLevel.Info, LogLevel.Warn, LogLevel.Error,
    };

    private static readonly string[] ToggleLevelKeys = {
        "CRL_LogViewer_Level_Trace", "CRL_LogViewer_Level_Debug", "CRL_LogViewer_Level_Info",
        "CRL_LogViewer_Level_Warn", "CRL_LogViewer_Level_Error",
    };

    private readonly ViewerLogSink sink;
    private readonly LogViewerState state = new LogViewerState();

    private Vector2 channelScroll;
    private Vector2 listScroll;
    private Vector2 detailScroll;

    private float channelPaneWidth = 220f;
    private float detailPaneHeight = 220f;
    private float detailPaneWidth = 356f;

    private Splitter dragging = Splitter.None;
    private LogDetailWindow? popout;

    private readonly SuggestField channelField = new SuggestField("rimlog-channel-filter");
    private readonly SuggestField dslField = new SuggestField("rimlog-dsl-filter");
    private List<string> channelNames = new List<string>();

    private int cachedRevision = -1;
    private string cachedSignature = string.Empty;
    private List<LogEntry> filtered = new List<LogEntry>();
    private List<LogChannel> channels = new List<LogChannel>();

    public LogViewerWindow(ViewerLogSink sink) {
        this.sink = sink;
        optionalTitle = "CRL_LogViewer_Title".Translate();
        onlyOneOfTypeAllowed = true;
    }

    public LogViewerWindow() : this(LogViewerBoot.Sink ?? new ViewerLogSink()) {
    }

    public override Vector2 InitialSize => new Vector2(1100f, 700f);

    private enum Splitter {
        None,
        Channels,
        Detail,
    }

    private static DetailPlacement Placement => LoggingMod.Settings.logViewerDetailPlacement;

    public override void PreClose() {
        base.PreClose();
        ClosePopout();
    }

    public override void DoWindowContents(Rect inRect) {
        // Before anything else: a dropdown is painted last but must win the click first.
        channelField.ReserveClicks();
        dslField.ReserveClicks();

        DropPopoutIfUserClosedIt();
        RebuildIfStale();

        Text.Font = GameFont.Tiny;
        DrawToolbar(inRect);
        Text.Font = GameFont.Small;

        Rect body = inRect;
        body.yMin += ToolbarHeight;

        if (state.ChannelsOpen) {
            Rect channelPane = new Rect(body.x, body.y, channelPaneWidth, body.height);
            DrawChannelPane(channelPane);

            Rect handle = new Rect(channelPane.xMax, body.y, SplitterThickness, body.height);
            DoSplitter(handle, Splitter.Channels);
            body.xMin = handle.xMax;
        }

        DrawContent(body);
        DrawSuggestionOverlays();
        ReleaseSplitterOnMouseUp();
    }

    // ---- toolbar ----

    private void DrawToolbar(Rect inRect) {
        float x = inRect.x;
        float y = inRect.y;

        DoRowButton(ref x, y, "CRL_LogViewer_Clear".Translate(), null, ClearEntries);
        DoRowButton(ref x, y, "CRL_LogViewer_Copy".Translate(), null, CopyVisibleToClipboard);
        DoRowButton(ref x, y, "CRL_LogViewer_ShareBundle".Translate(), null,
            () => LogBundleShare.Upload(sink, state, static () => { }));
        DoRowButton(
            ref x,
            y,
            state.ChannelsOpen ? "CRL_LogViewer_HideChannels".Translate() : "CRL_LogViewer_ShowChannels".Translate(),
            null,
            () => state.ChannelsOpen = !state.ChannelsOpen);
        DoRowButton(ref x, y, PlacementLabel(), null, CyclePlacement);
        DoRowButton(ref x, y, "CRL_LogViewer_OpenVanilla".Translate(), null, LogViewerBoot.OpenVanilla);

        for (int i = 0; i < ToggleLevels.Length; i++) {
            DoLevelToggle(ref x, y, i);
        }

        DrawTailingBadge(inRect);
    }

    private void DoLevelToggle(ref float x, float y, int index) {
        LogLevel level = ToggleLevels[index];
        int slot = (int)level;
        bool on = state.Levels[slot];
        string label = ((string)ToggleLevelKeys[index].Translate()).Substring(0, 1).ToUpperInvariant();

        Rect rect = new Rect(x, y, 26f, 24f);
        GUI.color = on ? LevelColors.For(level) : new Color(0.42f, 0.43f, 0.44f);
        if (DevGUI.ButtonText(rect, label)) {
            state.Levels[slot] = !on;
            if (level == LogLevel.Error) {
                state.Levels[(int)LogLevel.Fatal] = !on;
            }
        }

        GUI.color = Color.white;
        TooltipHandler.TipRegion(rect, ToggleLevelKeys[index].Translate());
        x += 30f;
    }

    private void DrawTailingBadge(Rect inRect) {
        Rect rect = new Rect(inRect.xMax - 110f, inRect.y, 110f, 24f);
        Rect dot = new Rect(rect.x, rect.y + 9f, 6f, 6f);
        Widgets.DrawBoxSolid(dot, new Color(0.44f, 0.75f, 0.54f));

        GUI.color = new Color(0.44f, 0.75f, 0.54f);
        Widgets.Label(new Rect(dot.xMax + 6f, rect.y + 2f, rect.width - 12f, 22f), "CRL_LogViewer_Tailing".Translate());
        GUI.color = Color.white;
    }

    private string PlacementLabel() {
        switch (Placement) {
            case DetailPlacement.Right:
                return "CRL_LogViewer_Detail_Right".Translate();
            case DetailPlacement.Popout:
                return "CRL_LogViewer_Detail_Popout".Translate();
            default:
                return "CRL_LogViewer_Detail_Bottom".Translate();
        }
    }

    private void CyclePlacement() {
        LoggingSettings settings = LoggingMod.Settings;
        settings.logViewerDetailPlacement = Placement switch {
            DetailPlacement.Bottom => DetailPlacement.Right,
            DetailPlacement.Right => DetailPlacement.Popout,
            _ => DetailPlacement.Bottom,
        };
        settings.Write();
        SyncPopout();
    }

    // ---- channel pane ----

    private void DrawChannelPane(Rect rect) {
        Widgets.DrawBoxSolid(rect, new Color(1f, 1f, 1f, 0.02f));

        Rect filterBox = new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, FilterStripHeight);
        string nextChannelFilter = channelField.Draw(
            filterBox,
            state.ChannelFilter,
            "CRL_LogViewer_FilterChannels",
            FilterSuggest.ForChannelFilter(state.ChannelFilter, ChannelIds()));
        if (nextChannelFilter != state.ChannelFilter) {
            state.ChannelFilter = nextChannelFilter;
            InvalidateCache();
        }

        Rect listRect = rect;
        listRect.yMin = filterBox.yMax + 2f;

        Rect view = new Rect(0f, 0f, listRect.width - ScrollbarWidth, channels.Count * RowHeight);
        Widgets.BeginScrollView(listRect, ref channelScroll, view);

        for (int i = 0; i < channels.Count; i++) {
            DrawChannelRow(new Rect(0f, i * RowHeight, view.width, RowHeight), channels[i], i);
        }

        Widgets.EndScrollView();
    }

    private void DrawChannelRow(Rect rect, LogChannel channel, int index) {
        if (index % 2 == 1) {
            Widgets.DrawAltRect(rect);
        }
        if (channel.Id == state.ActiveChannel) {
            Widgets.DrawHighlightSelected(rect);
        }
        Widgets.DrawHighlightIfMouseover(rect);

        float x = rect.x + 4f + channel.Depth * IndentPerDepth;

        if (channel.HasChildren) {
            Rect twisty = new Rect(x, rect.y + 4f, 14f, 14f);
            GUI.color = new Color(0.54f, 0.56f, 0.58f);
            Widgets.Label(twisty, channel.Expanded ? "-" : "+");
            GUI.color = Color.white;
            if (Widgets.ButtonInvisible(twisty)) {
                state.ToggleChannel(channel.Id, channel.Depth);
                InvalidateCache();
                Event.current.Use();
            }
        }
        x += 16f;

        Rect dot = new Rect(x, rect.y + 8f, 6f, 6f);
        Widgets.DrawBoxSolid(dot, channel.HasError ? LevelColors.For(LogLevel.Error) : LevelColors.ForChannel(channel.Id));
        x += 12f;

        float nameWidth = rect.xMax - x - CountColumnWidth;
        Widgets.Label(new Rect(x, rect.y - 1f, nameWidth, RowHeight), channel.Name.Truncate(nameWidth));

        Text.Anchor = TextAnchor.MiddleRight;
        Text.Font = GameFont.Tiny;
        GUI.color = new Color(0.54f, 0.56f, 0.58f);
        Widgets.Label(new Rect(rect.xMax - CountColumnWidth, rect.y, CountColumnWidth - 4f, RowHeight), channel.Count.ToString());
        GUI.color = Color.white;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;

        if (Widgets.ButtonInvisible(rect)) {
            state.ActiveChannel = channel.Id;
            InvalidateCache();
        }
    }

    // ---- list, filter strip and detail ----

    private void DrawContent(Rect rect) {
        Rect listArea = rect;
        Rect detailArea = default;

        switch (Placement) {
            case DetailPlacement.Bottom:
                listArea.yMax = rect.yMax - detailPaneHeight - SplitterThickness;
                detailArea = new Rect(rect.x, rect.yMax - detailPaneHeight, rect.width, detailPaneHeight);
                DoSplitter(new Rect(rect.x, listArea.yMax, rect.width, SplitterThickness), Splitter.Detail);
                break;

            case DetailPlacement.Right:
                listArea.xMax = rect.xMax - detailPaneWidth - SplitterThickness;
                detailArea = new Rect(rect.xMax - detailPaneWidth, rect.y, detailPaneWidth, rect.height);
                DoSplitter(new Rect(listArea.xMax, rect.y, SplitterThickness, rect.height), Splitter.Detail);
                break;
        }

        DrawFilterStrip(ref listArea);
        DrawList(listArea);

        if (Placement != DetailPlacement.Popout) {
            LogDetailView.Draw(detailArea, state.Selected, ref detailScroll, LoggingMod.Settings.logViewerCombinedDetail);
        }
    }

    private void DrawFilterStrip(ref Rect area) {
        Rect strip = new Rect(area.x + 2f, area.y + 2f, area.width - 4f, FilterStripHeight);

        Rect dslBox = strip;
        string nextDsl = dslField.Draw(
            dslBox,
            state.DslSource,
            "CRL_LogViewer_DslPlaceholder",
            FilterSuggest.For(state.DslSource, channelNames));
        if (nextDsl != state.DslSource) {
            state.DslSource = nextDsl;
            state.DslError = ParseError(nextDsl);
            InvalidateCache();
        }

        area.yMin = strip.yMax + 2f;

        if (string.IsNullOrEmpty(state.DslError)) {
            return;
        }

        Text.Font = GameFont.Tiny;
        GUI.color = LevelColors.For(LogLevel.Error);
        Widgets.Label(new Rect(area.x + 2f, area.y, area.width - 4f, ErrorStripHeight), state.DslError);
        GUI.color = Color.white;
        Text.Font = GameFont.Small;
        area.yMin += ErrorStripHeight;
    }

    private static string? ParseError(string source) {
        if (string.IsNullOrEmpty(source)) {
            return null;
        }
        return FilterExpression.TryParse(source, out _, out string? error) ? null : error;
    }

    private void DrawList(Rect rect) {
        Widgets.DrawBoxSolid(rect, new Color(1f, 1f, 1f, 0.02f));

        Rect view = new Rect(0f, 0f, rect.width - ScrollbarWidth, filtered.Count * RowHeight);
        Widgets.BeginScrollView(rect, ref listScroll, view);

        for (int i = 0; i < filtered.Count; i++) {
            DrawLogRow(new Rect(0f, i * RowHeight, view.width, RowHeight), filtered[i], i);
        }

        Widgets.EndScrollView();

        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Mouse.IsOver(rect)) {
            state.Selected = null;
        }
    }

    private void DrawLogRow(Rect rect, LogEntry entry, int index) {
        if (index % 2 == 1) {
            Widgets.DrawAltRect(rect);
        }
        if (ReferenceEquals(entry, state.Selected)) {
            Widgets.DrawHighlightSelected(rect);
        }
        Widgets.DrawHighlightIfMouseover(rect);

        Text.Font = GameFont.Tiny;
        GUI.color = new Color(0.54f, 0.56f, 0.58f);
        Widgets.Label(new Rect(rect.x + 4f, rect.y + 1f, TimestampWidth, RowHeight), entry.Timestamp.ToString("HH:mm:ss"));

        GUI.color = LevelColors.ForChannel(entry.Channel);
        Widgets.Label(
            new Rect(rect.x + 4f + TimestampWidth, rect.y + 1f, ChannelColumnWidth, RowHeight),
            entry.Channel.Truncate(ChannelColumnWidth));

        Text.Font = GameFont.Small;
        GUI.color = LevelColors.For(entry.Level);
        float messageX = rect.x + 8f + TimestampWidth + ChannelColumnWidth;
        float messageWidth = rect.xMax - messageX - 4f;
        Widgets.Label(new Rect(messageX, rect.y - 1f, messageWidth, RowHeight), entry.RenderedMessage.Truncate(messageWidth));
        GUI.color = Color.white;

        if (Widgets.ButtonInvisible(rect)) {
            state.Selected = entry;
            Event.current.Use();
        }
    }

    // ---- splitters ----

    private void DoSplitter(Rect handle, Splitter which) {
        Widgets.DrawBoxSolid(handle, new Color(1f, 1f, 1f, Mouse.IsOver(handle) || dragging == which ? 0.20f : 0.08f));

        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Mouse.IsOver(handle)) {
            dragging = which;
            Event.current.Use();
        }

        if (dragging != which || Event.current.type != EventType.MouseDrag) {
            return;
        }

        switch (which) {
            case Splitter.Channels:
                channelPaneWidth = Mathf.Clamp(channelPaneWidth + Event.current.delta.x, 140f, 420f);
                break;
            case Splitter.Detail when Placement == DetailPlacement.Bottom:
                detailPaneHeight = Mathf.Clamp(detailPaneHeight - Event.current.delta.y, 60f, windowRect.height - 200f);
                break;
            case Splitter.Detail:
                detailPaneWidth = Mathf.Clamp(detailPaneWidth - Event.current.delta.x, 240f, windowRect.width - 460f);
                break;
        }

        Event.current.Use();
    }

    private void ReleaseSplitterOnMouseUp() {
        if (dragging != Splitter.None && Event.current.type == EventType.MouseUp) {
            dragging = Splitter.None;
        }
    }

    // ---- state ----

    /// <summary>Refilters only when the sink or a filter input actually changed; this runs every frame.</summary>
    private void RebuildIfStale() {
        string signature = string.Join(
            "",
            state.ActiveChannel,
            state.ChannelFilter,
            state.DslSource,
            state.DslError ?? string.Empty,
            LevelSignature(),
            state.ExpandedChannels.Count.ToString());

        if (sink.Revision == cachedRevision && signature == cachedSignature) {
            return;
        }

        IReadOnlyList<LogEntry> snapshot = sink.Snapshot();
        filtered = LogFilter.Apply(snapshot, state);
        channels = LogFilter.BuildChannels(snapshot, state, new ChannelLabels(
            "CRL_LogViewer_AllChannels".Translate(),
            "CRL_LogViewer_Group_Mod".Translate(),
            "CRL_LogViewer_Group_Vanilla".Translate()));
        channelNames = DistinctChannels(snapshot);
        cachedRevision = sink.Revision;
        cachedSignature = signature;
    }

    private void DrawSuggestionOverlays() {
        string? channelPick = channelField.DrawOverlay();
        if (channelPick != null) {
            state.ChannelFilter = channelPick;
            InvalidateCache();
        }

        string? dslPick = dslField.DrawOverlay();
        if (dslPick != null) {
            state.DslSource = dslPick;
            state.DslError = ParseError(dslPick);
            InvalidateCache();
        }
    }

    private static List<string> DistinctChannels(IReadOnlyList<LogEntry> snapshot) {
        HashSet<string> seen = new HashSet<string>(System.StringComparer.Ordinal);
        List<string> names = new List<string>();
        for (int i = 0; i < snapshot.Count; i++) {
            string channel = snapshot[i].Channel;
            if (!string.IsNullOrEmpty(channel) && seen.Add(channel)) {
                names.Add(channel);
            }
        }
        return names;
    }

    private List<string> ChannelIds() {
        List<string> ids = new List<string>(channels.Count);
        for (int i = 0; i < channels.Count; i++) {
            if (channels[i].Id != LogViewerState.AllChannels) {
                ids.Add(channels[i].Id);
            }
        }
        return ids;
    }

    private string LevelSignature() {
        int packed = 0;
        for (int i = 0; i < state.Levels.Length && i < 32; i++) {
            if (state.Levels[i]) {
                packed |= 1 << i;
            }
        }
        return packed.ToString();
    }

    private void InvalidateCache() {
        cachedRevision = -1;
    }

    private void ClearEntries() {
        sink.Clear();
        state.Selected = null;
        InvalidateCache();
    }

    private void CopyVisibleToClipboard() {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        for (int i = 0; i < filtered.Count; i++) {
            LogEntry entry = filtered[i];
            builder.Append(entry.Timestamp.ToString("HH:mm:ss.fff"))
                .Append(" [").Append(entry.Level).Append("] [").Append(entry.Channel).Append("] ")
                .AppendLine(entry.RenderedMessage);
        }
        GUIUtility.systemCopyBuffer = builder.ToString();
        Messages.Message("CRL_LogViewer_Copy".Translate(), MessageTypeDefOf.TaskCompletion, false);
    }

    // ---- popout ----

    private void SyncPopout() {
        if (Placement == DetailPlacement.Popout) {
            if (popout == null) {
                popout = new LogDetailWindow(state);
                Find.WindowStack.Add(popout);
            }
            return;
        }

        ClosePopout();
    }

    private void DropPopoutIfUserClosedIt() {
        if (popout != null && !Find.WindowStack.IsOpen(popout)) {
            popout = null;
        }
        if (Placement == DetailPlacement.Popout && popout == null) {
            SyncPopout();
        }
    }

    private void ClosePopout() {
        if (popout == null) {
            return;
        }
        popout.Close(false);
        popout = null;
    }
}
