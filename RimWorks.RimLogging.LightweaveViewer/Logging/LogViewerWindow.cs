using System;
using System.Collections.Generic;
using Cosmere.Lightweave.Hooks;
using Cosmere.Lightweave.Feedback;
using Cosmere.Lightweave.Icons;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using RimWorks.RimLogging.Settings;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Surfaces;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Typography;
using Cosmere.Lightweave.Types;
using RimWorks.RimLogging;
using RimWorks.RimLogging.Filtering;
using UnityEngine;
using Verse;
using Chip = Cosmere.Lightweave.Data.Chip;
using KeyValue = Cosmere.Lightweave.Data.KeyValue;
using LogEntry = RimWorks.RimLogging.LogEntry;
using LwList = Cosmere.Lightweave.Data.List;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace RimWorks.RimLogging.LightweaveViewer;

internal sealed class LogViewerWindow : LightweaveWindow {
    private static readonly LogLevel[] ChipLevels = {
        LogLevel.Trace, LogLevel.Debug, LogLevel.Info, LogLevel.Warn, LogLevel.Error,
    };

    private static readonly string[] ChipLevelKeys = {
        "CL_LogViewer_Level_Trace", "CL_LogViewer_Level_Debug", "CL_LogViewer_Level_Info",
        "CL_LogViewer_Level_Warn", "CL_LogViewer_Level_Error",
    };

    private readonly LightweaveLogSink sink;
    private readonly LogViewerState state = new LogViewerState();

    public LogViewerWindow(LightweaveLogSink sink) {
        this.sink = sink;
    }

    public LogViewerWindow() : this(LogViewerBoot.Sink ?? new LightweaveLogSink()) {
    }

    protected override float WidthFraction => 0.85f;
    protected override float HeightFraction => 0.72f;

    protected override EdgeInsets? CardPadding => EdgeInsets.Zero;

    protected override bool DrawVignette => false;

    protected override bool DrawScrim => false;

    protected override bool KeepOnTop => true;

    protected override LightweaveNode? Header() {
        Action invalidate = MakeInvalidate();

        return WindowHeader.Create(
            headerContent: BuildTitlebar(invalidate),
            headerContentPadding: new EdgeInsets(
                Top: new Rem(1.125f),
                Right: new Rem(0.875f),
                Bottom: new Rem(1.125f),
                Left: new Rem(0.875f)
            ),
            draggable: true,
            drawDivider: true
        );
    }

    protected override LightweaveNode Body() {
        Action invalidate = MakeInvalidate();

        int sinkRev = sink.Revision;
        int levelsSig = PackLevels(state.Levels);
        bool dslOk = string.IsNullOrEmpty(state.DslSource) || state.DslError == null;
        int expandedSig = SignExpandedChannels(state.ExpandedChannels);

        IReadOnlyList<LogEntry> snapshot = Cosmere.Lightweave.Hooks.Hooks.UseMemo(
            () => sink.Snapshot(),
            new object[] { sinkRev }
        );
        List<LogEntry> filtered = Cosmere.Lightweave.Hooks.Hooks.UseMemo(
            () => LogFilter.Apply(snapshot, state),
            new object[] { sinkRev, state.ActiveChannel, levelsSig, state.DslSource, dslOk }
        );
        List<LogChannel> channels = Cosmere.Lightweave.Hooks.Hooks.UseMemo(
            () => LogFilter.BuildChannels(snapshot, state),
            new object[] { sinkRev, state.ChannelFilter, expandedSig }
        );

        LightweaveNode listColumn = BuildListColumn(filtered, invalidate);
        LightweaveNode listAndDetail = state.Selected == null
            ? listColumn
            : SplitPane.Create(
                first: listColumn,
                second: BuildDetail(invalidate),
                orientation: SplitOrientation.Horizontal,
                initialFraction: 0.68f,
                minFirst: new Rem(20f),
                minSecond: new Rem(14f),
                style: Fill
            );

        if (!state.ChannelsOpen) {
            return listAndDetail;
        }

        return SplitPane.Create(
            first: BuildChannelPane(channels, invalidate),
            second: listAndDetail,
            orientation: SplitOrientation.Horizontal,
            initialFraction: 0.20f,
            minFirst: new Rem(11.25f),
            minSecond: new Rem(24f),
            style: Fill
        );
    }

    private static int PackLevels(bool[] levels) {
        int sig = 0;
        for (int i = 0; i < levels.Length && i < 32; i++) {
            if (levels[i]) {
                sig |= (1 << i);
            }
        }
        return sig;
    }

    private static int SignExpandedChannels(Dictionary<string, bool> map) {
        int sig = map.Count;
        foreach (KeyValuePair<string, bool> kvp in map) {
            int h = kvp.Key.GetHashCode();
            if (kvp.Value) {
                h = ~h;
            }
            sig = sig * 31 + h;
        }
        return sig;
    }

    private static Style Fill => new Style { Width = Length.Stretch, Height = Length.Stretch };

    private static Action MakeInvalidate() {
        Cosmere.Lightweave.Hooks.Hooks.StateHandle<int> tick = Cosmere.Lightweave.Hooks.Hooks.UseState(0);
        return () => tick.Set(tick.Value + 1);
    }

    private LightweaveNode BuildTitlebar(Action invalidate) {
        return Box.Create(
            children: outer => outer.Add(HStack.Create(
                gap: new Rem(1f),
                children: row => {
                    row.AddHug(IconButton.Create(
                        icon: Glyph.Create(
                            state.ChannelsOpen ? Cosmere.Lightweave.Icons.Phosphor.SidebarSimple : Cosmere.Lightweave.Icons.Phosphor.Sidebar,
                            style: new Style { TextColor = state.ChannelsOpen ? ThemeSlot.SurfaceAccent : ThemeSlot.TextSecondary }
                        ),
                        onClick: () => {
                            state.ChannelsOpen = !state.ChannelsOpen;
                            invalidate();
                        },
                        variant: Variant.Ghost,
                        active: state.ChannelsOpen,
                        tooltipKey: state.ChannelsOpen ? "CL_LogViewer_HideChannels" : "CL_LogViewer_ShowChannels",
                        id: "logviewer-sidebar-toggle"
                    ));

                    row.AddHug(Box.Create(
                        children: titleBox => titleBox.Add(Text.Create(
                            (string)"CL_LogViewer_Title".Translate(),
                            style: new Style {
                                FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Display),
                                FontSize = new Rem(1.25f),
                                TextColor = ThemeSlot.TextPrimary,
                                LetterSpacing = Tracking.Of(0.04f),
                            }
                        )),
                        style: new Style {
                            Padding = new EdgeInsets(Top: new Rem(4f / 16f)),
                        }
                    ));

                    row.AddHug(HStack.Create(
                        gap: new Rem(0.375f),
                        children: live => {
                            live.AddHug(PulseDot.Create(
                                color: ThemeSlot.StatusSuccess,
                                size: new Rem(0.375f)
                            ));
                            live.AddHug(Text.Create(
                                (string)"CL_LogViewer_Tailing".Translate(),
                                style: new Style {
                                    FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Mono),
                                    FontSize = new Rem(0.625f),
                                    TextColor = ThemeSlot.StatusSuccess,
                                    LetterSpacing = Tracking.Of(0.22f),
                                }
                            ));
                        }
                    ));

                    row.AddFlex(Box.Create());

                    row.AddHug(IconButton.Create(
                        icon: Glyph.Create(Cosmere.Lightweave.Icons.Phosphor.Terminal),
                        onClick: LogViewerBoot.OpenVanilla,
                        variant: Variant.Ghost,
                        tooltipKey: "CL_LogViewer_OpenVanilla",
                        id: "logviewer-open-vanilla"
                    ));

                    row.AddHug(IconButton.Create(
                        icon: Glyph.Create(Cosmere.Lightweave.Icons.Phosphor.UploadSimple),
                        onClick: () => LogBundleShare.Upload(sink, state, invalidate),
                        variant: Variant.Ghost,
                        disabled: state.Uploading,
                        tooltipKey: "CL_LogViewer_ShareBundle",
                        id: "logviewer-share-bundle"
                    ));

                    row.AddHug(IconButton.Create(
                        icon: Glyph.Create(Cosmere.Lightweave.Icons.Phosphor.X),
                        onClick: () => Close(),
                        variant: Variant.Ghost,
                        tooltipKey: "CL_LogViewer_Close",
                        id: "logviewer-close"
                    ));
                }
            )),
            style: new Style {
                Width = Length.Stretch,
            }
        );
    }

    

    

    private LightweaveNode BuildChannelPane(List<LogChannel> channels, Action invalidate) {
        return Stack.Create(
            gap: new Rem(0f),
            children: col => {
                col.Add(Box.Create(
                    children: c => c.Add(SearchField.Create(
                        value: state.ChannelFilter,
                        onChange: text => {
                            state.ChannelFilter = text;
                            invalidate();
                        },
                        placeholder: (string)"CL_LogViewer_FilterChannels".Translate(),
                        variant: SearchFieldVariant.Borderless,
                        id: "logviewer-channel-filter",
                        style: new Style { Width = Length.Stretch }
                    )),
                    style: new Style {
                        Width = Length.Stretch,
                        Background = BackgroundSpec.Of(ThemeSlot.ShelfTint),
                        Border = new BorderSpec(Bottom: new Rem(1f / 16f), Color: ThemeSlot.BorderSubtle),
                        Padding = EdgeInsets.Horizontal(new Rem(0.875f)),
                    }
                ));
                col.AddFlex(LwList.Create(
                    items: channels,
                    rowBuilder: (channel, index) => BuildChannelRow(channel, invalidate),
                    rowHeight: new Rem(1.75f).ToPixels(),
                    virtualize: true,
                    overscan: 4,
                    style: new Style { Width = Length.Stretch, Height = Length.Stretch }
                ));
            },
            style: Fill
        );
    }

    private LightweaveNode BuildChannelRow(LogChannel channel, Action invalidate) {
        bool active = channel.Id == state.ActiveChannel;
        string label = channel.Name;
        bool isAllChannels = channel.Id == LogViewerState.AllChannels;
        bool hasChildren = channel.HasChildren && !isAllChannels;
        bool expanded = channel.Expanded;
        string channelId = channel.Id;
        int channelDepth = channel.Depth;

        return Button.Create(
            label: string.Empty,
            onClick: () => {
                state.ActiveChannel = channelId;
                if (hasChildren) {
                    state.ToggleChannel(channelId, channelDepth);
                }
                invalidate();
            },
            variant: Variant.Ghost,
            ghost: true,
            body: HStack.Create(
                gap: new Rem(0.375f),
                children: row => FillChannelRow(row, channel, label, active, hasChildren, expanded)
            ),
            style: ChannelRowStyle(active),
            id: "logviewer-channel-" + channelId
        );
    }

    private static Style ChannelRowStyle(bool active) {
        EdgeInsets padding = new EdgeInsets(Top: new Rem(0.3125f), Bottom: new Rem(0.3125f), Left: new Rem(0.875f), Right: new Rem(0.875f));
        if (!active) {
            return new Style {
                Width = Length.Stretch,
                Padding = padding,
            };
        }
        return new Style {
            Width = Length.Stretch,
            Background = BackgroundSpec.Of(ThemeSlot.ActiveTint),
            Border = new BorderSpec(Left: Spacing.StripeWidth, Color: ThemeSlot.SurfaceAccent),
            Padding = padding,
        };
    }

    private static void FillChannelRow(HStackBuilder row, LogChannel channel, string label, bool active, bool hasChildren, bool expanded) {
        ThemeSlot caretColor = active ? ThemeSlot.SurfaceAccent : ThemeSlot.TextMuted;
        ThemeSlot labelColor = active ? ThemeSlot.SurfaceAccent : ThemeSlot.TextSecondary;
        ThemeSlot countColor = channel.HasError ? ThemeSlot.StatusDanger : caretColor;

        if (channel.Depth > 0) {
            row.Add(Box.Create(), new Rem(channel.Depth * 0.875f).ToPixels());
        }

        if (hasChildren) {
            row.AddHug(Glyph.Create(
                expanded ? Cosmere.Lightweave.Icons.Phosphor.CaretDown : Cosmere.Lightweave.Icons.Phosphor.CaretRight,
                style: new Style {
                    FontSize = new Rem(0.6875f),
                    TextColor = caretColor,
                }
            ));
        } else {
            row.Add(Box.Create(), new Rem(0.6875f).ToPixels());
        }

        row.AddFlex(Text.Create(
            label,
            style: new Style {
                FontSize = new Rem(0.8125f),
                TextColor = labelColor,
            }
        ));
        row.AddHug(Text.Create(
            channel.Count.ToString("N0"),
            style: new Style {
                FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Mono),
                FontSize = new Rem(0.6875f),
                TextColor = countColor,
            }
        ));
    }

    private LightweaveNode BuildListColumn(List<LogEntry> filtered, Action invalidate) {
        return Stack.Create(
            gap: new Rem(0f),
            children: col => {
                col.Add(BuildFilterBar(invalidate));
                col.AddFlex(LwList.Create(
                    items: filtered,
                    rowBuilder: (entry, index) => BuildLogRow(entry, invalidate),
                    rowHeight: new Rem(1.625f).ToPixels(),
                    virtualize: true,
                    overscan: 6,
                    style: new Style { Width = Length.Stretch, Height = Length.Stretch }
                ));
            },
            style: Fill
        );
    }

    private LightweaveNode BuildFilterBar(Action invalidate) {
        return Column.Create(
            gap: new Rem(0f),
            children: col => {
                col.Add(Box.Create(
                    children: c => c.Add(SearchField.Create(
                        value: state.DslSource,
                        onChange: text => {
                            state.DslSource = text;
                            if (string.IsNullOrWhiteSpace(text)) {
                                state.DslError = null;
                            }
                            else {
                                FilterExpression.TryParse(text, out _, out string? err);
                                state.DslError = err;
                            }
                            invalidate();
                        },
                        placeholder: (string)"CL_LogViewer_DslPlaceholder".Translate(),
                        variant: SearchFieldVariant.Borderless,
                        id: "logviewer-search",
                        style: new Style { Width = Length.Stretch }
                    )),
                    style: new Style {
                        Width = Length.Stretch,
                        Background = BackgroundSpec.Of(ThemeSlot.ShelfTint),
                        Border = new BorderSpec(Bottom: new Rem(1f / 16f), Color: ThemeSlot.BorderSubtle),
                        Padding = EdgeInsets.Horizontal(new Rem(0.875f)),
                    }
                ));

                if (!string.IsNullOrEmpty(state.DslError)) {
                    col.Add(Box.Create(
                        children: c => c.Add(Text.Create(
                            state.DslError ?? "",
                            style: new Style {
                                FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Mono),
                                FontSize = new Rem(0.6875f),
                                TextColor = ThemeSlot.StatusDanger,
                            }
                        )),
                        style: new Style {
                            Width = Length.Stretch,
                            Background = BackgroundSpec.Of(ThemeSlot.ShelfTint),
                            Padding = new EdgeInsets(Top: new Rem(0.375f), Bottom: new Rem(0.375f), Left: new Rem(0.875f), Right: new Rem(0.875f)),
                            Border = new BorderSpec(Bottom: new Rem(1f / 16f), Color: ThemeSlot.BorderSubtle),
                        }
                    ));
                }

                col.Add(Box.Create(
                    children: c => c.Add(HStack.Create(
                        gap: new Rem(0.875f),
                        children: row => {
                            row.AddHug(Text.Create(
                                ((string)"CL_LogViewer_LevelGroup".Translate()).ToUpperInvariant(),
                                style: new Style {
                                    FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Mono),
                                    FontSize = new Rem(0.59375f),
                                    TextColor = ThemeSlot.TextMuted,
                                    LetterSpacing = Tracking.Of(0.22f),
                                }
                            ));
                            for (int i = 0; i < ChipLevels.Length; i++) {
                                int index = i;
                                LogLevel level = ChipLevels[index];
                                row.AddHug(Chip.Create(
                                    label: (string)ChipLevelKeys[index].Translate(),
                                    interactive: true,
                                    state: state.Levels[(int)level],
                                    onToggle: value => {
                                        state.Levels[(int)level] = value;
                                        invalidate();
                                    },
                                    variant: LogFilter.LevelVariant(level),
                                    id: "logviewer-chip-" + level
                                ));
                            }
                        }
                    )),
                    style: new Style {
                        Width = Length.Stretch,
                        Background = BackgroundSpec.Of(ThemeSlot.ShelfTint),
                        Padding = new EdgeInsets(Top: new Rem(0.625f), Bottom: new Rem(0.625f), Left: new Rem(0.875f), Right: new Rem(0.875f)),
                        Border = new BorderSpec(Bottom: new Rem(1f / 16f), Color: ThemeSlot.BorderSubtle),
                    }
                ));
            }
        );
    }

    

    private LightweaveNode BuildLogRow(LogEntry entry, Action invalidate) {
        bool selected = ReferenceEquals(entry, state.Selected);
        string timestamp = entry.Timestamp.ToString("HH:mm:ss.fff");
        string levelLabel = entry.Level.ToString().ToUpperInvariant();
        ThemeSlot levelSlot = LogFilter.LevelSlot(entry.Level);
        string channel = string.IsNullOrEmpty(entry.Channel) ? "" : "[" + entry.Channel + "]";
        bool hasChannel = !string.IsNullOrEmpty(channel);
        string source = entry.Source.IsCallerProvided
            ? "[" + entry.Source.File + ":" + entry.Source.Line + "]"
            : "";
        bool hasSource = !string.IsNullOrEmpty(source);

        bool isError = entry.Level == LogLevel.Error || entry.Level == LogLevel.Fatal;

        LightweaveNode levelBadge = Box.Create(
            children: b => b.Add(Text.Create(
                levelLabel,
                style: new Style {
                    FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Mono),
                    FontSize = new Rem(0.59375f),
                    TextColor = levelSlot,
                    TextAlign = TextAlign.Center,
                    LetterSpacing = Tracking.Of(0.22f),
                }
            )),
            style: new Style {
                Border = BorderSpec.All(new Rem(1f / 16f), levelSlot),
                Padding = new EdgeInsets(Top: new Rem(0.0625f), Bottom: new Rem(0.0625f), Left: new Rem(0.3125f), Right: new Rem(0.3125f)),
            }
        );

        LightweaveNode body = HStack.Create(
            gap: new Rem(0.5f),
            children: row => {
                row.AddHug(Text.Create(
                    timestamp,
                    style: new Style { FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Mono), FontSize = new Rem(0.65625f), TextColor = ThemeSlot.TextMuted, LetterSpacing = Tracking.Of(0.06f) }
                ));
                row.AddHug(Column.Create(
                    justify: FlexJustify.Center,
                    align: FlexAlign.Start,
                    children: cell => cell.Add(levelBadge)
                ));
                if (hasChannel) {
                    row.AddHug(Text.Create(
                        channel,
                        style: new Style { FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Mono), FontSize = new Rem(0.65625f), TextColor = ChannelColors.For(entry.Channel), LetterSpacing = Tracking.Of(0.1f) }
                    ));
                }
                if (hasSource) {
                    row.AddHug(Text.Create(
                        source,
                        style: new Style { FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Mono), FontSize = new Rem(0.65625f), TextColor = ThemeSlot.SurfaceAccent, LetterSpacing = Tracking.Of(0.1f) }
                    ));
                }
                row.AddFlex(Text.Create(
                    entry.RenderedMessage,
                    style: new Style { FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Mono), FontSize = new Rem(0.6875f), TextColor = isError ? ThemeSlot.TextPrimary : ThemeSlot.TextSecondary, LetterSpacing = Tracking.Of(0.02f) }
                ));
            }
        );

        Style rowStyle = selected
            ? new Style {
                Background = BackgroundSpec.Of(ThemeSlot.ActiveTint),
                Border = new BorderSpec(Bottom: new Rem(1f / 16f), Left: new Rem(2f / 16f), Color: ThemeSlot.SurfaceAccent),
                Padding = EdgeInsets.Horizontal(new Rem(0.875f)),
            }
            : new Style {
                Border = new BorderSpec(Bottom: new Rem(1f / 16f), Color: ThemeSlot.BorderSubtle),
                Padding = EdgeInsets.Horizontal(new Rem(0.875f)),
            };

        return Button.Create(
            label: string.Empty,
            onClick: () => {
                state.Selected = selected ? null : entry;
                invalidate();
            },
            variant: Variant.Ghost,
            ghost: true,
            body: body,
            style: rowStyle,
            id: "logviewer-row"
        );
    }

    private LightweaveNode BuildDetail(Action invalidate) {
        LogEntry? entry = state.Selected;
        if (entry == null) {
            return BuildEmptyDetail();
        }

        LogEntry detail = entry;
        bool combined = LoggingMod.Settings.logViewerCombinedDetail;

        return Stack.Create(
            gap: new Rem(0f),
            children: col => {
                col.Add(Box.Create(
                    children: hd => hd.Add(Column.Create(
                        gap: new Rem(0.375f),
                        children: stack => {
                            stack.Add(HStack.Create(
                                children: row => {
                                    row.AddFlex(Text.Create(
                                        ((string)"CL_LogViewer_Detail_Crumb".Translate()).ToUpperInvariant(),
                                        style: new Style { FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Mono), FontSize = new Rem(0.625f), TextColor = ThemeSlot.TextMuted, LetterSpacing = Tracking.Of(0.22f) }
                                    ));
                                    row.AddHug(IconButton.Create(
                                        icon: Glyph.Create(Cosmere.Lightweave.Icons.Phosphor.X),
                                        onClick: () => {
                                            state.Selected = null;
                                            invalidate();
                                        },
                                        variant: Variant.Ghost,
                                        tooltipKey: "CL_LogViewer_Deselect",
                                        id: "logviewer-deselect"
                                    ));
                                }
                            ));
                            stack.Add(Text.Create(
                                detail.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                                style: new Style { FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Mono), FontSize = new Rem(0.6875f), TextColor = ThemeSlot.TextMuted, LetterSpacing = Tracking.Of(0.06f) }
                            ));
                        }
                    )),
                    style: new Style {
                        Width = Length.Stretch,
                        Padding = new EdgeInsets(Top: new Rem(0.875f), Bottom: new Rem(0.875f), Left: new Rem(1.125f), Right: new Rem(1.125f)),
                        Border = new BorderSpec(Bottom: new Rem(1f / 16f), Color: ThemeSlot.BorderSubtle),
                    }
                ));

                col.AddFlex(Stack.Create(
                    gap: new Rem(0.4375f),
                    children: body => FillDetailBody(body, detail, combined),
                    style: new Style {
                        Width = Length.Stretch,
                        Height = Length.Stretch,
                        Padding = new EdgeInsets(Top: new Rem(1f), Bottom: new Rem(1f), Left: new Rem(1.125f), Right: new Rem(1.125f)),
                    }
                ));
            },
            style: Fill
        );
    }

    private LightweaveNode BuildEmptyDetail() {
        return Column.Create(
            gap: new Rem(0.625f),
            align: FlexAlign.Center,
            justify: FlexJustify.Center,
            children: col => {
                col.Add(Text.Create(
                    ((string)"CL_LogViewer_NoSelection".Translate()).ToUpperInvariant(),
                    style: new Style { FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Mono), FontSize = new Rem(0.65625f), TextColor = ThemeSlot.TextMuted, LetterSpacing = Tracking.Of(0.22f), TextAlign = TextAlign.Center }
                ));
                col.Add(Text.Create(
                    (string)"CL_LogViewer_NoSelectionHint".Translate(),
                    style: new Style { FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Body), FontSize = new Rem(0.8125f), TextColor = ThemeSlot.TextMuted, TextAlign = TextAlign.Center }
                ));
            },
            style: Fill
        );
    }

    private void FillDetailBody(StackBuilder body, LogEntry detail, bool combined) {
        body.Add(DetailRow(
            (string)"CL_LogViewer_Detail_Level".Translate(),
            detail.Level.ToString().ToUpperInvariant(),
            LogFilter.LevelSlot(detail.Level)
        ));
        body.Add(DetailRow(
            (string)"CL_LogViewer_Detail_Channel".Translate(),
            detail.Channel,
            ChannelColors.For(detail.Channel)
        ));
        if (!string.IsNullOrEmpty(detail.Mod)) {
            body.Add(DetailRow(
                (string)"CL_LogViewer_Detail_Mod".Translate(),
                detail.Mod!,
                ChannelColors.For(detail.Mod)
            ));
        }
        body.Add(DetailRow(
            (string)"CL_LogViewer_Detail_Source".Translate(),
            detail.Source.IsCallerProvided ? detail.Source.File + ":" + detail.Source.Line : (string)"CL_LogViewer_Detail_NoSource".Translate(),
            ThemeSlot.SurfaceAccent
        ));

        if (detail.Context != null && detail.Context.Count > 0) {
            foreach (KeyValuePair<string, object?> pair in detail.Context) {
                body.Add(DetailRow(
                    pair.Key.ToUpperInvariant(),
                    pair.Value?.ToString() ?? "null",
                    ThemeSlot.TextSecondary
                ));
            }
        }

        string trace = detail.StackTrace ?? detail.Exception?.ToString() ?? string.Empty;

        if (combined) {
            body.Add(Text.Create(
                ((string)"CL_LogViewer_Detail_MessageAndStack".Translate()).ToUpperInvariant(),
                style: new Style {
                    FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Mono),
                    FontSize = new Rem(0.625f),
                    TextColor = ThemeSlot.TextMuted,
                    LetterSpacing = Tracking.Of(0.18f),
                }
            ));
            string combinedText = string.IsNullOrEmpty(trace)
                ? detail.RenderedMessage
                : detail.RenderedMessage + "\n\n" + trace;
            body.AddFlex(ScrollArea.Create(
                content: TextArea.Create(
                    value: combinedText,
                    onChange: _ => { },
                    readOnly: true,
                    minRows: 1,
                    maxRows: int.MaxValue,
                    instanceKey: "message-and-stack",
                    id: "logviewer-message-and-stack",
                    style: new Style { Width = Length.Stretch, Height = Length.Stretch }
                ),
                resetKey: detail.Timestamp,
                edge: true,
                stretchContent: true,
                style: new Style { Width = Length.Stretch, Height = Length.Stretch }
            ));
        }
        else {
            body.Add(Text.Create(
                ((string)"CL_LogViewer_Detail_Message".Translate()).ToUpperInvariant(),
                style: new Style {
                    FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Mono),
                    FontSize = new Rem(0.625f),
                    TextColor = ThemeSlot.TextMuted,
                    LetterSpacing = Tracking.Of(0.18f),
                }
            ));
            body.AddFlex(ScrollArea.Create(
                content: TextArea.Create(
                    value: detail.RenderedMessage,
                    onChange: _ => { },
                    readOnly: true,
                    minRows: 1,
                    maxRows: int.MaxValue,
                    instanceKey: "message",
                    id: "logviewer-message",
                    style: new Style { Width = Length.Stretch, Height = Length.Stretch }
                ),
                resetKey: detail.Timestamp,
                edge: true,
                stretchContent: true,
                style: new Style { Width = Length.Stretch, Height = Length.Stretch }
            ));
            body.Add(Text.Create(
                ((string)"CL_LogViewer_Detail_Stack".Translate()).ToUpperInvariant(),
                style: new Style {
                    FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Mono),
                    FontSize = new Rem(0.625f),
                    TextColor = ThemeSlot.TextMuted,
                    LetterSpacing = Tracking.Of(0.18f),
                }
            ));
            body.AddFlex(ScrollArea.Create(
                content: TextArea.Create(
                    value: trace,
                    onChange: _ => { },
                    placeholder: (string)"CL_LogViewer_Detail_NoStack".Translate(),
                    readOnly: true,
                    minRows: 1,
                    maxRows: int.MaxValue,
                    instanceKey: "stack",
                    id: "logviewer-stack",
                    style: new Style { Width = Length.Stretch, Height = Length.Stretch }
                ),
                resetKey: detail.Timestamp,
                edge: true,
                stretchContent: true,
                style: new Style { Width = Length.Stretch, Height = Length.Stretch }
            ));
        }
    }

    private static LightweaveNode DetailRow(string keyLabel, string value, ColorRef valueColor, bool wrap = false) {
        return KeyValue.Create(
            keyLabel.ToUpperInvariant(),
            Text.Create(
                value,
                wrap: wrap,
                style: new Style { FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Mono), FontSize = new Rem(0.71875f), TextColor = valueColor }
            ),
            labelWidth: new Rem(5.375f),
            gap: new Rem(0.5f),
            labelStyle: new Style {
                FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Mono),
                FontSize = new Rem(0.625f),
                TextColor = ThemeSlot.TextMuted,
                LetterSpacing = Tracking.Of(0.18f),
            }
        );
    }
}
