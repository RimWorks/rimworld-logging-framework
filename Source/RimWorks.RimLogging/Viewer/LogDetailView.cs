using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimWorks.RimLogging.Viewer;

/// <summary>Draws the detail body for one entry. Shared by the inline pane and the popout window.</summary>
internal static class LogDetailView {
    private const float RowHeight = 20f;
    private const float LabelWidth = 86f;
    private const float Pad = 6f;
    private const float ScrollbarWidth = 18f;
    private const float CopyButtonWidth = 92f;

    public static void Draw(Rect rect, LogEntry? entry, ref Vector2 scroll, bool combined) {
        Widgets.DrawBoxSolid(rect, new Color(1f, 1f, 1f, 0.02f));

        if (entry == null) {
            DrawEmpty(rect);
            return;
        }

        Rect inner = rect.ContractedBy(Pad);
        float contentWidth = inner.width - ScrollbarWidth;
        string trace = EntryText.Trace(entry);

        Rect view = new Rect(0f, 0f, contentWidth, MeasureHeight(entry, trace, combined, contentWidth));
        Widgets.BeginScrollView(inner, ref scroll, view);

        float y = 0f;
        DrawRow(ref y, contentWidth, "CRL_LogViewer_Detail_Level", entry.Level.ToString().ToUpperInvariant(), LevelColors.For(entry.Level));
        DrawRow(ref y, contentWidth, "CRL_LogViewer_Detail_Channel", entry.Channel, LevelColors.ForChannel(entry.Channel));
        if (!string.IsNullOrEmpty(entry.Mod)) {
            DrawRow(ref y, contentWidth, "CRL_LogViewer_Detail_Mod", entry.Mod!, LevelColors.ForChannel(entry.Mod!));
        }
        DrawRow(ref y, contentWidth, "CRL_LogViewer_Detail_Source", SourceText(entry), Color.gray);

        if (entry.Context != null) {
            foreach (KeyValuePair<string, object?> pair in entry.Context) {
                DrawRow(ref y, contentWidth, null, pair.Value?.ToString() ?? "null", Color.gray, pair.Key.ToUpperInvariant());
            }
        }

        if (combined) {
            string both = string.IsNullOrEmpty(trace) ? entry.RenderedMessage : entry.RenderedMessage + "\n\n" + trace;
            DrawBlock(ref y, contentWidth, "CRL_LogViewer_Detail_MessageAndStack", both);
        }
        else {
            DrawBlock(ref y, contentWidth, "CRL_LogViewer_Detail_Message", entry.RenderedMessage);
            DrawBlock(ref y, contentWidth, "CRL_LogViewer_Detail_Stack",
                string.IsNullOrEmpty(trace) ? (string)"CRL_LogViewer_Detail_NoStack".Translate() : trace);
        }

        Widgets.EndScrollView();
        DrawCopyButton(rect, entry);
    }

    private static void DrawCopyButton(Rect rect, LogEntry? entry) {
        // allocated even with nothing selected, so the control id count does not move with the
        // selection. IMGUI derives ids from allocation order.
        Rect copy = entry == null
            ? new Rect(-4000f, -4000f, CopyButtonWidth, 22f)
            : new Rect(rect.xMax - Pad - ScrollbarWidth - Pad - CopyButtonWidth, rect.y + Pad, CopyButtonWidth, 22f);

        Text.Font = GameFont.Tiny;
        if (Widgets.ButtonText(copy, "CRL_LogViewer_Detail_CopyAll".Translate()) && entry != null) {
            GUIUtility.systemCopyBuffer = EntryText.Full(entry);
            Messages.Message("CRL_LogViewer_Copy".Translate(), MessageTypeDefOf.TaskCompletion, false);
        }
        Text.Font = GameFont.Small;
    }

    private static float MeasureHeight(LogEntry entry, string trace, bool combined, float width) {
        int rows = 3 + (string.IsNullOrEmpty(entry.Mod) ? 0 : 1) + (entry.Context?.Count ?? 0);
        float h = rows * RowHeight + 6f;

        Text.Font = GameFont.Small;
        if (combined) {
            string both = string.IsNullOrEmpty(trace) ? entry.RenderedMessage : entry.RenderedMessage + "\n\n" + trace;
            h += RowHeight + BlockHeight(both, width) + 6f;
        }
        else {
            h += RowHeight + BlockHeight(entry.RenderedMessage, width) + 6f;
            h += RowHeight + BlockHeight(string.IsNullOrEmpty(trace) ? " " : trace, width) + 6f;
        }
        return h;
    }

    private static void DrawRow(ref float y, float width, string? labelKey, string value, Color valueColor, string? rawLabel = null) {
        Rect row = new Rect(0f, y, width, RowHeight);

        Text.Font = GameFont.Tiny;
        GUI.color = new Color(0.54f, 0.56f, 0.58f);
        Widgets.Label(new Rect(row.x, row.y, LabelWidth, RowHeight), rawLabel ?? (string)labelKey!.Translate());

        Text.Font = GameFont.Small;
        GUI.color = valueColor;
        Widgets.Label(new Rect(row.x + LabelWidth, row.y - 1f, width - LabelWidth, RowHeight), value);

        GUI.color = Color.white;
        y += RowHeight;
    }

    private static void DrawBlock(ref float y, float width, string labelKey, string body) {
        y += 6f;

        Text.Font = GameFont.Tiny;
        GUI.color = new Color(0.54f, 0.56f, 0.58f);
        Widgets.Label(new Rect(0f, y, width, RowHeight), ((string)labelKey.Translate()).ToUpperInvariant());
        y += RowHeight;

        Text.Font = GameFont.Small;
        GUI.color = new Color(0.94f, 0.94f, 0.91f);
        float h = BlockHeight(body, width);
        // read-only TextArea rather than Label so the text can be selected and copied
        Widgets.TextArea(new Rect(0f, y, width, h), body, readOnly: true);
        GUI.color = Color.white;

        y += h;
    }

    // measured with the TextArea style, not Text.CalcHeight: the field's padding wraps text
    // narrower than a Label, so measuring with the wrong style clips the last few lines
    private static float BlockHeight(string body, float width) {
        return Text.CurTextAreaReadOnlyStyle.CalcHeight(new GUIContent(body), width);
    }

    private static string SourceText(LogEntry entry) {
        return entry.Source.IsCallerProvided
            ? entry.Source.File + ":" + entry.Source.Line
            : (string)"CRL_LogViewer_Detail_NoSource".Translate();
    }

    private static void DrawEmpty(Rect rect) {
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        GUI.color = new Color(0.54f, 0.56f, 0.58f);
        Widgets.Label(rect, "CRL_LogViewer_NoSelection".Translate());
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
    }
}
