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
[StaticConstructorOnStartup]
internal sealed class LogViewerWindow : EditWindow {
    private const float ToolbarHeight = 26f;
    private const float RowHeight = 22f;
    private const float SplitterThickness = 4f;
    private const float FilterStripHeight = 24f;
    private const float ErrorStripHeight = 16f;
    private const float ScrollbarWidth = 18f;
    private const float RepeatColumnWidth = 26f;
    private const float ExtraLineColumnWidth = 82f;
    private const float IndentPerDepth = 12f;
    private const float CountColumnWidth = 46f;
    private const float TimestampWidth = 74f;
    private const float ChannelColumnWidth = 156f;
    private const float ButtonSize = 24f;
    private const float ButtonGap = 4f;
    private const float GroupGap = 8f;
    private const float PillMinWidth = 26f;
    private const float PillPadding = 12f;

    private const float TailingDotSize = 6f;
    private const float TailingDotGap = 6f;

    // the badge reports state rather than doing anything, so it wants more air than the
    // gap between two buttons or it reads as another control in the run
    private const float TailingLeadGap = 20f;

    // measured, not fixed: a hardcoded reserve wider than the badge leaves dead space to its
    // right and the whole trailing cluster stops looking anchored
    private static float TailingWidth =>
        TailingLeadGap + TailingDotSize + TailingDotGap + Text.CalcSize("CRL_LogViewer_Tailing".Translate()).x;

    // one constant for both the draw and the width accounting, so the two cannot drift apart
    private const float GroupDividerWidth = (2f * GroupGap) + 1f;

    // 16px glyph centred in a 24px face, matching the vanilla icon buttons
    private const float IconInset = 4f;

    // #414141, the face colour sampled from the vanilla dev toolbar
    private float pillsTotalWidth;

    private static readonly Color ButtonFace = new Color(0.255f, 0.255f, 0.255f);
    private static readonly Color TailingGreen = new Color(0.44f, 0.75f, 0.54f);

    private const float FilterGroupFixed = (2f * GroupGap) + ButtonSize + ButtonGap;

    private static readonly Color GroupDivider = new Color(0.29f, 0.31f, 0.32f);
    private static readonly Color SegmentDivider = new Color(0.16f, 0.17f, 0.18f);

    private static Texture2D? nextErrorTexture;
    private static Texture2D NextErrorTexture => nextErrorTexture ??= ContentFinder<Texture2D>.Get("UI/Buttons/RimLogging/NextError");

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
    private float lastListHeight = 1f;
    private float? pendingScroll;
    private Vector2 detailScroll;

    private float channelPaneWidth = 220f;
    private float detailPaneHeight = 220f;
    private float detailPaneWidth = 356f;

    private readonly ListTailing tailing = new ListTailing();
    private float listContentHeight;
    private float channelContentHeight;
    private int listFirstRow;
    private int listLastRow;
    private int channelFirstRow;
    private int channelLastRow;
    private bool selectNewestPending;

    private Splitter dragging = Splitter.None;
    private LogDetailWindow? popout;

    private readonly SuggestField channelField = new SuggestField("rimlog-channel-filter");
    private readonly SuggestField dslField = new SuggestField("rimlog-dsl-filter");
    private List<string> channelNames = new List<string>();

    // Truncate does no caching without one of these, and re-measures the font per character.
    // Keep them string-keyed: the TaggedString overload caches the untruncated value.
    private readonly Dictionary<string, string> messageTruncation = new Dictionary<string, string>();
    private readonly Dictionary<string, (string Head, int Extra)> rowPreview =
        new Dictionary<string, (string Head, int Extra)>(StringComparer.Ordinal);
    private readonly Dictionary<string, string> headTruncation = new Dictionary<string, string>();
    private readonly Dictionary<int, string> extraLabels = new Dictionary<int, string>();
    private readonly Dictionary<string, string> channelTruncation = new Dictionary<string, string>();
    private readonly Dictionary<string, string> channelNameTruncation = new Dictionary<string, string>();
    private float lastMessageWidth = -1f;
    private float lastChannelNameWidth = -1f;

    private int cachedRevision = -1;
    private string cachedSignature = string.Empty;
    private List<LogEntry> filtered = new List<LogEntry>();
    private List<LogChannel> channels = new List<LogChannel>();
    private LevelCounts levelCounts;

    private ToolbarPlan toolbarPlan = new ToolbarPlan(false, false, ToolbarLayout.FilterFloor);
    private readonly string[] pillLabels = new string[ToggleLevels.Length];
    private readonly float[] pillWidths = new float[ToggleLevels.Length];

    public LogViewerWindow(ViewerLogSink sink, bool selectNewest = false) {
        this.sink = sink;
        selectNewestPending = selectNewest;
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
        // IMGUI runs several passes per frame and needs the same layout in each. entries arrive on
        // a background thread, so refreshing mid-frame changes the view rect under the scroll view.
        if (Event.current.type == EventType.Layout || cachedRevision < 0) {
            RebuildIfStale();
        }

        Text.Font = GameFont.Tiny;
        DrawToolbar(inRect);
        Text.Font = GameFont.Small;

        Rect body = inRect;
        body.yMin += toolbarPlan.TwoRows ? ToolbarHeight * 2f : ToolbarHeight;


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


    private void DrawToolbar(Rect inRect) {
        if (Event.current.type == EventType.Layout) {
            BuildToolbarLayout(inRect.width - TailingWidth);
        }

        float x = inRect.x;
        float y = inRect.y;

        DoIconButton(ref x, y, TexButton.Delete, "CRL_Tip_Clear".Translate(), ClearEntries);
        DoIconButton(ref x, y, TexButton.Copy, "CRL_Tip_Copy".Translate(), CopyVisibleToClipboard);
        DoShareButton(ref x, y);
        DoGroupDivider(ref x, y);

        // the trailing cluster is anchored to the right edge and the filter field takes whatever
        // is left, so measurement drift lands in the elastic gap instead of pushing icons off
        float rightX = inRect.xMax - TailingWidth - RightClusterWidth();

        if (!toolbarPlan.TwoRows) {
            DrawFilterGroup(ref x, y, Mathf.Max(ToolbarLayout.FilterFloor, rightX - x - FilterGroupFixed));
        }

        x = rightX;
        DoGroupDivider(ref x, y);
        for (int i = 0; i < ToggleLevels.Length; i++) {
            DoLevelToggle(ref x, y, i);
        }
        DoGroupDivider(ref x, y);

        if (!toolbarPlan.CompactLevels) {
            DoIconButton(ref x, y, NextErrorTexture, "CRL_Tip_NextError".Translate(), JumpToNextError);
        }
        DoIconButton(ref x, y, TexButton.ToggleLog, "CRL_Tip_VanillaLog".Translate(), LogViewerBoot.OpenVanilla);
        DoIconButton(ref x, y, TexButton.OpenDebugActionsMenu, "CRL_Tip_More".Translate(), OpenMoreMenu);

        if (toolbarPlan.TwoRows) {
            float rowTwoX = inRect.x;
            float rowTwoY = y + ToolbarHeight;
            DrawFilterGroup(ref rowTwoX, rowTwoY, toolbarPlan.FilterFieldWidth);
        }

        if (tailing.Following) {
            DrawTailingBadge(inRect);
        }
    }

    /// <summary>Row-one width with counted pills, since that is what decides whether the field
    /// and Presets fit on row one at all.</summary>
    private void BuildToolbarLayout(float availableWidth) {
        float pillsWidth = ComputePillWidths(compact: false);
        float rowOneFixed = (6 * (ButtonSize + ButtonGap)) + (3 * GroupDividerWidth) + pillsWidth;
        // filter group is: gap + presets button (with its own trailing gap) + gap, all baked into DrawFilterGroup.
        float filterGroupWidth = FilterGroupFixed;
        toolbarPlan = ToolbarLayout.Compute(availableWidth, rowOneFixed, filterGroupWidth);
        pillsTotalWidth = toolbarPlan.CompactLevels ? ComputePillWidths(compact: true) : pillsWidth;
    }

    private float ComputePillWidths(bool compact) {
        float total = 0f;
        for (int i = 0; i < ToggleLevels.Length; i++) {
            string name = (string)ToggleLevelKeys[i].Translate();
            if (compact) {
                pillLabels[i] = name.Substring(0, 1).ToUpperInvariant();
                pillWidths[i] = PillMinWidth;
            }
            else {
                string label = name + " " + levelCounts.For(ToggleLevels[i]);
                pillLabels[i] = label;
                pillWidths[i] = Mathf.Max(PillMinWidth, Text.CalcSize(label).x + (2f * PillPadding));
            }
            total += pillWidths[i];
        }
        return total;
    }

    private void DrawFilterGroup(ref float x, float y, float width) {
        Rect fieldRect = new Rect(x, y, width, ButtonSize);
        Rect textRect = fieldRect;
        textRect.xMin += 16f;
        string nextDsl = dslField.Draw(
            textRect,
            state.DslSource,
            "CRL_LogViewer_DslPlaceholder",
            FilterSuggest.For(state.DslSource, channelNames));
        if (nextDsl != state.DslSource) {
            state.DslSource = nextDsl;
            state.DslError = ParseError(nextDsl);
            InvalidateCache();
        }

        Rect searchIconRect = new Rect(fieldRect.x + 2f, fieldRect.y + (fieldRect.height - 13f) / 2f, 13f, 13f);
        GUI.DrawTexture(searchIconRect, TexButton.Search);
        TooltipHandler.TipRegion(fieldRect, "CRL_Tip_Filter".Translate());

        x += width + GroupGap;
        DoIconButton(ref x, y, TexButton.NewFile, "CRL_Tip_Presets".Translate(), OpenPresetMenu);
        x += GroupGap;
    }

    private void OpenMoreMenu() {
        List<FloatMenuOption> options = new List<FloatMenuOption> {
            new FloatMenuOption(
                state.ChannelsOpen ? "CRL_LogViewer_HideChannels".Translate() : "CRL_LogViewer_ShowChannels".Translate(),
                () => state.ChannelsOpen = !state.ChannelsOpen),
            new FloatMenuOption(PlacementLabel(), CyclePlacement),
            new FloatMenuOption(
                LogViewerBoot.AutoOpen ? "CRL_LogViewer_AutoOpenOn".Translate() : "CRL_LogViewer_AutoOpenOff".Translate(),
                () => LogViewerBoot.AutoOpen = !LogViewerBoot.AutoOpen),
        };
        if (toolbarPlan.CompactLevels) {
            options.Add(new FloatMenuOption("CRL_LogViewer_NextError".Translate(), JumpToNextError));
        }
        Find.WindowStack.Add(new FloatMenu(options));
    }

    private void JumpToNextError() {
        int from = state.Selected == null ? -1 : filtered.IndexOf(state.Selected);
        int next = ErrorJump.Next(filtered, from, LogLevel.Error);
        if (next < 0) {
            Messages.Message("CRL_LogViewer_NoErrors".Translate(), MessageTypeDefOf.RejectInput, false);
            return;
        }
        state.Selected = filtered[next];
        pendingScroll = ErrorJump.ScrollTo(next, RowHeight, lastListHeight, filtered.Count);
    }

    private void OpenPresetMenu() {
        LoggingSettings settings = LoggingMod.Settings;
        List<FloatMenuOption> options = new List<FloatMenuOption> {
            new FloatMenuOption("CRL_LogViewer_PresetSave".Translate(), PromptForPresetName),
        };
        foreach (string preset in Filtering.FilterPresets.Usable(settings.filterPresetNames, settings.filterPresetExpressions)) {
            string captured = preset;
            options.Add(new FloatMenuOption(captured, () => ApplyPreset(captured)));
            options.Add(new FloatMenuOption(
                "CRL_LogViewer_PresetDelete".Translate(captured.Named("NAME")),
                () => {
                    Filtering.FilterPresets.Remove(settings.filterPresetNames, settings.filterPresetExpressions, captured);
                    settings.Write();
                }));
        }
        Find.WindowStack.Add(new FloatMenu(options));
    }

    private void PromptForPresetName() {
        string expression = state.DslSource;
        if (string.IsNullOrWhiteSpace(expression)) {
            Messages.Message("CRL_LogViewer_PresetNoFilter".Translate(), MessageTypeDefOf.RejectInput, false);
            return;
        }
        Find.WindowStack.Add(new Dialog_NameFilterPreset(name => {
            LoggingSettings settings = LoggingMod.Settings;
            Filtering.FilterPresets.Save(settings.filterPresetNames, settings.filterPresetExpressions, name, expression);
            settings.Write();
        }));
    }

    private void ApplyPreset(string preset) {
        LoggingSettings settings = LoggingMod.Settings;
        string? expression = Filtering.FilterPresets.Expression(
            settings.filterPresetNames, settings.filterPresetExpressions, preset);
        if (expression == null) {
            return;
        }
        state.DslSource = expression;
        InvalidateCache();
    }

    private void DoShareButton(ref float x, float y) {
        bool busy = state.Uploading;
        Rect rect = new Rect(x, y, ButtonSize, ButtonSize);
        if (DrawIconFace(rect, TexButton.Save) && !busy) {
            StartShare();
        }
        TooltipHandler.TipRegion(rect, "CRL_Tip_Share".Translate());
        x += ButtonSize + ButtonGap;

        if (!busy) {
            return;
        }

        Widgets.DrawRectFast(rect, new Color(0f, 0f, 0f, 0.6f));
        TextAnchor anchor = Text.Anchor;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(rect, Spinner.Frame(Time.realtimeSinceStartup).ToString());
        Text.Anchor = anchor;
    }

    private void StartShare() => _ = LogBundleShare.Upload(sink, state, static () => { });

    private static void DoIconButton(ref float x, float y, Texture2D texture, string tooltip, Action action) {
        Rect rect = new Rect(x, y, ButtonSize, ButtonSize);
        if (DrawIconFace(rect, texture)) {
            action();
        }
        TooltipHandler.TipRegion(rect, tooltip);
        x += ButtonSize + ButtonGap;
    }

    /// <summary>
    /// Draws an icon on a button face. DevGUI.ButtonImage draws the texture alone, so an icon
    /// used bare floats on the bar with no button under it.
    /// </summary>
    private static bool DrawIconFace(Rect rect, Texture2D texture) {
        Widgets.DrawBoxSolid(rect, ButtonFace);
        Widgets.DrawHighlightIfMouseover(rect);
        GUI.DrawTexture(new Rect(rect.x + IconInset, rect.y + IconInset,
            rect.width - (2f * IconInset), rect.height - (2f * IconInset)), texture);
        return Widgets.ButtonInvisible(rect);
    }

    /// <summary>Width of the right-anchored cluster: the level pills and the buttons after them.</summary>
    private float RightClusterWidth() {
        float icons = toolbarPlan.CompactLevels ? 2 : 3;
        return (2f * GroupDividerWidth) + pillsTotalWidth + (icons * (ButtonSize + ButtonGap));
    }

    private static void DoGroupDivider(ref float x, float y) {
        Widgets.DrawBoxSolid(new Rect(x + GroupGap, y + 3f, 1f, ButtonSize - 6f), GroupDivider);
        x += GroupDividerWidth;
    }

    private void DoLevelToggle(ref float x, float y, int index) {
        LogLevel level = ToggleLevels[index];
        int slot = (int)level;
        bool on = state.Levels[slot];
        float width = pillWidths[index];

        Rect rect = new Rect(x, y, width, ButtonSize);
        GUI.color = on ? LevelColors.For(level) : new Color(0.30f, 0.31f, 0.32f);
        if (DevGUI.ButtonText(rect, pillLabels[index])) {
            state.Levels[slot] = !on;
            if (level == LogLevel.Error) {
                state.Levels[(int)LogLevel.Fatal] = !on;
            }
        }
        GUI.color = Color.white;

        string action = on ? "hide" : "show";
        TooltipHandler.TipRegion(rect, "CRL_Tip_Level".Translate(
            ToggleLevelKeys[index].Translate().Named("LEVEL"),
            levelCounts.For(level).ToString().Named("COUNT"),
            action.Named("ACTION")));

        // segments butt together and are split by a hairline, so the row reads as one control
        if (index < ToggleLevels.Length - 1) {
            Widgets.DrawBoxSolid(new Rect(rect.xMax - 1f, y + 2f, 1f, ButtonSize - 4f), SegmentDivider);
        }
        x += width;
    }

    private static void DrawTailingBadge(Rect inRect) {
        float width = TailingWidth;
        Rect rect = new Rect(inRect.xMax - width, inRect.y, width, ButtonSize);
        Rect dot = new Rect(rect.x + TailingLeadGap, rect.y + 9f, TailingDotSize, TailingDotSize);
        Widgets.DrawBoxSolid(dot, TailingGreen);

        GUI.color = TailingGreen;
        Widgets.Label(new Rect(dot.xMax + TailingDotGap, rect.y + 2f, rect.width, 22f),
            "CRL_LogViewer_Tailing".Translate());
        GUI.color = Color.white;
        TooltipHandler.TipRegion(rect, "CRL_Tip_Tailing".Translate());
    }

    private static string PlacementLabel() {
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

        if (Event.current.type == EventType.Layout) {
            channelContentHeight = channels.Count * RowHeight;
        }
        Rect view = new Rect(0f, 0f, listRect.width - ScrollbarWidth, channelContentHeight);
        Widgets.BeginScrollView(listRect, ref channelScroll, view);

        if (Event.current.type == EventType.Layout) {
            (channelFirstRow, channelLastRow) = RowWindow.Visible(channelScroll.y, listRect.height, RowHeight, channels.Count);
        }
        int first = Mathf.Min(channelFirstRow, channels.Count);
        int last = Mathf.Min(channelLastRow, channels.Count);
        for (int i = first; i < last; i++) {
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
            Rect twisty = new Rect(x, rect.y, 14f, rect.height);
            GUI.color = new Color(0.54f, 0.56f, 0.58f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(twisty, channel.Expanded ? "-" : "+");
            GUI.color = Color.white;
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Mouse.IsOver(twisty)) {
                state.ToggleChannel(channel.Id, channel.Depth);
                InvalidateCache();
                Event.current.Use();
            }
        }
        x += 16f;

        Text.Anchor = TextAnchor.MiddleLeft;
        Rect dot = new Rect(x, rect.y + 8f, 6f, 6f);
        Widgets.DrawBoxSolid(dot, channel.HasError ? LevelColors.For(LogLevel.Error) : LevelColors.ForChannel(channel.Id));
        x += 12f;

        float nameWidth = rect.xMax - x - CountColumnWidth;
        if (Mathf.Abs(nameWidth - lastChannelNameWidth) > 0.5f) {
            channelNameTruncation.Clear();
            lastChannelNameWidth = nameWidth;
        }
        Widgets.Label(new Rect(x, rect.y, nameWidth, rect.height), channel.Name.Truncate(nameWidth, channelNameTruncation));

        Text.Anchor = TextAnchor.MiddleRight;
        Text.Font = GameFont.Tiny;
        GUI.color = new Color(0.54f, 0.56f, 0.58f);
        Widgets.Label(new Rect(rect.xMax - CountColumnWidth, rect.y, CountColumnWidth - 4f, rect.height), channel.Count.ToString());
        GUI.color = Color.white;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;

        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Mouse.IsOver(rect)) {
            state.ActiveChannel = channel.Id;
            InvalidateCache();
            Event.current.Use();
        }
    }


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

        DrawFilterError(ref listArea);
        DrawList(listArea);

        if (Placement != DetailPlacement.Popout) {
            LogDetailView.Draw(detailArea, state.Selected, ref detailScroll, LoggingMod.Settings.logViewerCombinedDetail);
        }
    }

    private void DrawFilterError(ref Rect area) {
        if (string.IsNullOrEmpty(state.DslError)) {
            return;
        }

        Text.Font = GameFont.Tiny;
        GUI.color = LevelColors.For(LogLevel.Error);
        Widgets.Label(new Rect(area.x + 2f, area.y + 2f, area.width - 4f, ErrorStripHeight), state.DslError);
        GUI.color = Color.white;
        Text.Font = GameFont.Small;
        area.yMin += ErrorStripHeight + 2f;
    }

    private static string? ParseError(string source) {
        if (string.IsNullOrEmpty(source)) {
            return null;
        }
        return FilterExpression.TryParse(source, out _, out string? error) ? null : error;
    }

    private void DrawList(Rect rect) {
        Widgets.DrawBoxSolid(rect, new Color(1f, 1f, 1f, 0.02f));

        if (Event.current.type == EventType.Layout) {
            listContentHeight = filtered.Count * RowHeight;
        }
        float contentHeight = listContentHeight;
        Rect view = new Rect(0f, 0f, rect.width - ScrollbarWidth, contentHeight);

        lastListHeight = rect.height;
        listScroll.y = tailing.BeforeScrollView(rect, listScroll.y, contentHeight);

        // applied after tailing so a jump is not immediately undone by the follow-the-tail pull
        if (pendingScroll.HasValue) {
            listScroll.y = pendingScroll.Value;
            pendingScroll = null;
        }

        Widgets.BeginScrollView(rect, ref listScroll, view);

        // Pick the row range on Layout only. Each row allocates a control id, and IMGUI needs the
        // same ids in every pass of a frame; recomputing mid-drag loses the scrollbar's hot control.
        if (Event.current.type == EventType.Layout) {
            (listFirstRow, listLastRow) = RowWindow.Visible(listScroll.y, rect.height, RowHeight, filtered.Count);
        }
        int first = Mathf.Min(listFirstRow, filtered.Count);
        int last = Mathf.Min(listLastRow, filtered.Count);
        for (int i = first; i < last; i++) {
            DrawLogRow(new Rect(0f, i * RowHeight, view.width, RowHeight), filtered[i], i);
        }

        Widgets.EndScrollView();
        tailing.AfterScrollView(rect, listScroll.y, contentHeight);

        // exclude the scrollbar strip: grabbing it must not clear the selection, which would
        // collapse the detail pane and change the control id count mid-drag
        Rect rowsOnly = new Rect(rect.x, rect.y, rect.width - ScrollbarWidth, rect.height);
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Mouse.IsOver(rowsOnly)) {
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

        Text.Anchor = TextAnchor.MiddleLeft;
        Text.Font = GameFont.Tiny;
        GUI.color = new Color(0.54f, 0.56f, 0.58f);
        Widgets.Label(new Rect(rect.x + 4f, rect.y, TimestampWidth, rect.height), entry.Timestamp.ToString("HH:mm:ss"));

        GUI.color = LevelColors.ForChannel(entry.Channel);
        Widgets.Label(
            new Rect(rect.x + 4f + TimestampWidth, rect.y, ChannelColumnWidth, rect.height),
            entry.Channel.Truncate(ChannelColumnWidth, channelTruncation));

        Text.Font = GameFont.Small;
        GUI.color = LevelColors.For(entry.Level);
        float messageX = rect.x + 8f + TimestampWidth + ChannelColumnWidth;
        float messageWidth = rect.xMax - messageX - 4f;
        // Draw the repeat count separately. Composing a new string per frame missed
        // GenText.Truncate's cache every time and grew it without bound.
        if (entry.Repeats > 1) {
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.54f, 0.56f, 0.58f);
            Widgets.Label(new Rect(messageX, rect.y, RepeatColumnWidth, rect.height), entry.Repeats.ToStringCached());
            Text.Font = GameFont.Small;
            GUI.color = LevelColors.For(entry.Level);
        }
        messageX += RepeatColumnWidth;
        messageWidth -= RepeatColumnWidth;
        // the cache is keyed by string only, so it has to be dropped when the column resizes
        if (Mathf.Abs(messageWidth - lastMessageWidth) > 0.5f) {
            messageTruncation.Clear();
            headTruncation.Clear();
            lastMessageWidth = messageWidth;
        }
        (string head, int extra) = RowMessage(entry.RenderedMessage);
        if (extra == 0) {
            Widgets.Label(new Rect(messageX, rect.y, messageWidth, rect.height),
                head.Truncate(messageWidth, messageTruncation));
        }
        else {
            // the head gets its own cache: it is truncated to a narrower budget than a
            // single-line message, and one cache cannot hold two widths for the same string
            float headWidth = messageWidth - ExtraLineColumnWidth;
            string shown = head.Truncate(headWidth, headTruncation);
            Widgets.Label(new Rect(messageX, rect.y, headWidth, rect.height), shown);

            // measured so the count sits against the text rather than out at the column edge
            float shownWidth = Mathf.Min(Text.CalcSize(shown).x, headWidth);
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.54f, 0.56f, 0.58f);
            Widgets.Label(
                new Rect(messageX + shownWidth + 6f, rect.y, ExtraLineColumnWidth, rect.height),
                ExtraLabel(extra));
            Text.Font = GameFont.Small;
        }
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;

        // no ButtonInvisible here: it allocates a control id, and a per-row id count that moves
        // with scrolling shifts every later slider id, which breaks scrollbar dragging
        if (Event.current.type == EventType.MouseDown && Mouse.IsOver(rect)) {
            state.Selected = entry;
            if (Event.current.button == 1) {
                OpenRowMenu(entry);
            }
            Event.current.Use();
        }
    }

    private void OpenRowMenu(LogEntry entry) {
        string channelId = ChannelClassifier.JoinPath(ChannelClassifier.PathFor(entry.Channel));

        List<FloatMenuOption> options = new List<FloatMenuOption> {
            new FloatMenuOption("CRL_LogViewer_Menu_CopyMessage".Translate(),
                () => CopyToClipboard(entry.RenderedMessage)),
            new FloatMenuOption("CRL_LogViewer_Menu_CopyWithStack".Translate(),
                () => CopyToClipboard(EntryText.WithStack(entry))),
            new FloatMenuOption("CRL_LogViewer_Menu_CopyFullDetail".Translate(),
                () => CopyToClipboard(EntryText.Full(entry))),
            new FloatMenuOption("CRL_LogViewer_Menu_CopyChannel".Translate(),
                () => CopyToClipboard(entry.Channel)),
            new FloatMenuOption("CRL_LogViewer_Menu_FilterChannel".Translate(),
                () => state.ActiveChannel = channelId),
        };

        if (state.ActiveChannel != LogViewerState.AllChannels) {
            options.Add(new FloatMenuOption("CRL_LogViewer_Menu_ShowAllChannels".Translate(),
                () => state.ActiveChannel = LogViewerState.AllChannels));
        }

        Find.WindowStack.Add(new FloatMenu(options));
    }

    private static void CopyToClipboard(string text) {
        GUIUtility.systemCopyBuffer = text;
        Messages.Message("CRL_LogViewer_Copy".Translate(), MessageTypeDefOf.TaskCompletion, false);
    }


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


    /// <summary>Refilters only when the sink or a filter input actually changed; this runs every frame.</summary>
    private void RebuildIfStale() {
        string signature = string.Join(
            "\u0001",
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

        // tree and channel list come from the sink's running tallies, so neither walks the buffer
        IReadOnlyDictionary<string, ChannelTally> tallies = sink.ChannelTallies();
        channels = LogFilter.BuildChannels(tallies, snapshot.Count, state, new ChannelLabels(
            "CRL_LogViewer_AllChannels".Translate(),
            "CRL_LogViewer_Group_Mod".Translate(),
            "CRL_LogViewer_Group_Vanilla".Translate()));
        channelNames = SortedChannelNames(tallies);
        levelCounts = sink.LevelTallies();
        cachedRevision = sink.Revision;
        cachedSignature = signature;

        if (selectNewestPending && filtered.Count > 0) {
            state.Selected = filtered[filtered.Count - 1];
            selectNewestPending = false;
        }
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

    // multi-line messages collapse to one line, cached because building it per frame would miss
    // GenText.Truncate's cache on every pass and grow it without bound
    private (string Head, int Extra) RowMessage(string message) {
        if (message.IndexOf('\n') < 0) {
            return (message, 0);
        }
        if (!rowPreview.TryGetValue(message, out (string Head, int Extra) split)) {
            split = EntryText.SplitRow(message);
            rowPreview[message] = split;
        }
        return split;
    }

    private string ExtraLabel(int extra) {
        if (!extraLabels.TryGetValue(extra, out string? label)) {
            label = EntryText.ExtraLinesLabel(extra);
            extraLabels[extra] = label;
        }
        return label;
    }

    private static List<string> SortedChannelNames(IReadOnlyDictionary<string, ChannelTally> tallies) {
        return tallies.Keys.Where(key => key != "(root)").ToList();
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
        CopyToClipboard(builder.ToString());
    }


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
            // user closed the popout, so fall back to the inline pane instead of respawning it
            popout = null;
            LoggingSettings settings = LoggingMod.Settings;
            settings.logViewerDetailPlacement = DetailPlacement.Bottom;
            settings.Write();
            return;
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
