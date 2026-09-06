using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimWorks.RimLogging.Viewer;

/// <summary>Draws the detail body for one entry. Shared by the inline pane and the popout window.</summary>
internal static class LogDetailView
{
    private const float RowHeight = 20f;
    private const float LabelWidth = 86f;
    private const float Pad = 6f;
    private const float ScrollbarWidth = 18f;
    private const float CopyButtonWidth = 92f;

    private static readonly Color LinkColor = new Color(0.44f, 0.68f, 0.90f);
    private static readonly Color LinkHover = new Color(0.62f, 0.81f, 1f);

    private static Vector2 dragMouse;
    private static int dragControl;
    private static Vector2 linkDown;

    public static void Draw(Rect rect, LogEntry? entry, ref Vector2 scroll)
    {
        Widgets.DrawBoxSolid(rect, new Color(1f, 1f, 1f, 0.02f));

        if (entry == null)
        {
            DrawEmpty(rect);
            return;
        }

        Rect inner = rect.ContractedBy(Pad);

        // the button gets its own strip. rows are text areas, and one drawn under the button
        // would take the mouse down and mark it used before the button ever saw it
        Rect header = new Rect(inner.x, inner.y, inner.width, DetailPaneLayout.ButtonHeight);
        Rect body = new Rect(inner.x, DetailPaneLayout.BodyTop(inner.y), inner.width,
            DetailPaneLayout.BodyHeight(inner.height));

        float contentWidth = body.width - ScrollbarWidth;
        string trace = EntryText.Trace(entry);

        Rect view = new Rect(0f, 0f, contentWidth, MeasureHeight(entry, trace, contentWidth));
        Widgets.BeginScrollView(body, ref scroll, view);

        float y = 0f;
        DrawRow(ref y, contentWidth, "CRL_LogViewer_Detail_Level", entry.Level.ToString().ToUpperInvariant(), LevelColors.For(entry.Level));
        DrawRow(ref y, contentWidth, "CRL_LogViewer_Detail_Channel", entry.Channel, LevelColors.ForChannel(entry.Channel));
        if (!string.IsNullOrEmpty(entry.Mod))
        {
            DrawRow(ref y, contentWidth, "CRL_LogViewer_Detail_Mod", entry.Mod!, LevelColors.ForChannel(entry.Mod!));
        }
        if (entry.PatchedBy == null)
        {
            DrawRow(ref y, contentWidth, "CRL_LogViewer_Detail_PatchedBy",
                "CRL_LogViewer_Detail_PatchedByUnavailable".Translate(), new Color(0.45f, 0.45f, 0.45f));
        }
        else if (entry.PatchedBy.Count > 0)
        {
            DrawRow(ref y, contentWidth, "CRL_LogViewer_Detail_PatchedBy", string.Join(", ", entry.PatchedBy), Color.gray);
        }
        DrawRow(ref y, contentWidth, "CRL_LogViewer_Detail_Source", SourceText(entry), Color.gray);
        DrawLinks(ref y, contentWidth, entry, trace);

        if (entry.Context != null)
        {
            foreach (KeyValuePair<string, object?> pair in entry.Context)
            {
                DrawRow(ref y, contentWidth, null, pair.Value?.ToString() ?? "null", Color.gray, pair.Key.ToUpperInvariant());
            }
        }

        DrawBlock(ref y, contentWidth, "CRL_LogViewer_Detail_MessageAndStack", MessageAndStack(entry, trace));

        Widgets.EndScrollView();
        DrawCopyButton(header, entry);
    }

    private static void DrawCopyButton(Rect header, LogEntry entry)
    {
        Rect copy = new Rect(header.xMax - ScrollbarWidth - CopyButtonWidth, header.y,
            CopyButtonWidth, DetailPaneLayout.ButtonHeight);

        Text.Font = GameFont.Tiny;
        if (Widgets.ButtonText(copy, "CRL_LogViewer_Detail_CopyAll".Translate()))
        {
            GUIUtility.systemCopyBuffer = EntryText.Full(entry);
            Messages.Message("CRL_LogViewer_Copy".Translate(), MessageTypeDefOf.TaskCompletion, false);
        }
        Text.Font = GameFont.Small;
    }

    private static float MeasureHeight(LogEntry entry, string trace, float width)
    {
        int rows = 3 + (string.IsNullOrEmpty(entry.Mod) ? 0 : 1) + (entry.PatchedBy is null or { Count: > 0 } ? 1 : 0)
            + (entry.Context?.Count ?? 0) + UrlScanner.ForEntry(entry, trace).Count;
        float h = rows * RowHeight + 6f;

        Text.Font = GameFont.Small;
        return h + RowHeight + BlockHeight(MessageAndStack(entry, trace), width) + 6f;
    }

    private static string MessageAndStack(LogEntry entry, string trace)
    {
        return string.IsNullOrEmpty(trace) ? entry.RenderedMessage : entry.RenderedMessage + "\n\n" + trace;
    }

    private static void DrawRow(ref float y, float width, string? labelKey, string value, Color valueColor, string? rawLabel = null)
    {
        Rect row = new Rect(0f, y, width, RowHeight);

        Text.Font = GameFont.Tiny;
        GUI.color = new Color(0.54f, 0.56f, 0.58f);
        Widgets.Label(new Rect(row.x, row.y, LabelWidth, RowHeight), rawLabel ?? (string)labelKey!.Translate());

        Text.Font = GameFont.Small;
        GUI.color = valueColor;
        DrawSelectable(FieldRect(ValueRect(row)), value);

        GUI.color = Color.white;
        y += RowHeight;
    }

    /// <summary>One row per link. The url selects like any other row, and a click that never drags copies it.</summary>
    private static void DrawLinks(ref float y, float width, LogEntry entry, string trace)
    {
        foreach (string url in UrlScanner.ForEntry(entry, trace))
        {
            Rect row = new Rect(0f, y, width, RowHeight);

            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.54f, 0.56f, 0.58f);
            Widgets.Label(new Rect(row.x, row.y, LabelWidth, RowHeight), "CRL_LogViewer_Detail_Link".Translate());

            Rect link = ValueRect(row);

            // the text area takes the mouse event and marks it used, so read it before drawing
            EventType type = Event.current.type;
            bool left = Event.current.button == 0;
            Vector2 mouse = Event.current.mousePosition;

            Text.Font = GameFont.Small;
            GUI.color = Mouse.IsOver(link) ? LinkHover : LinkColor;
            DrawSelectable(FieldRect(link), url);
            GUI.color = Color.white;

            // no ButtonInvisible: it would allocate a control id, and the link count varies per
            // entry, which would shift the text area ids underneath and break their selection
            if (left && type == EventType.MouseDown)
            {
                linkDown = mouse;
            }
            else if (left && type == EventType.MouseUp && link.Contains(mouse) && link.Contains(linkDown)
                && LinkClickPolicy.IsClick(mouse.x - linkDown.x, mouse.y - linkDown.y))
            {
                GUIUtility.systemCopyBuffer = url;
                Messages.Message("CRL_LogViewer_Detail_LinkCopied".Translate(url.Named("URL")),
                    MessageTypeDefOf.TaskCompletion, false);
            }

            y += RowHeight;
        }
    }

    private static Rect ValueRect(Rect row)
    {
        return new Rect(row.x + LabelWidth, row.y - 1f, row.width - LabelWidth, RowHeight);
    }

    // a text area insets its own content, so grow the rect by that padding to leave the glyphs
    // where the label used to draw them
    private static Rect FieldRect(Rect content)
    {
        RectOffset pad = Text.CurTextAreaReadOnlyStyle.padding;
        return new Rect(content.x - pad.left, content.y - pad.top,
            content.width + pad.horizontal, content.height + pad.vertical);
    }

    // read-only TextArea rather than Label so the text can be selected and copied
    private static void DrawSelectable(Rect rect, string text)
    {
        Widgets.TextArea(rect, text, readOnly: true);
        ExtendSlowDrag(rect);
    }

    private static void DrawBlock(ref float y, float width, string labelKey, string body)
    {
        y += 6f;

        Text.Font = GameFont.Tiny;
        GUI.color = new Color(0.54f, 0.56f, 0.58f);
        Widgets.Label(new Rect(0f, y, width, RowHeight), ((string)labelKey.Translate()).ToUpperInvariant());
        y += RowHeight;

        Text.Font = GameFont.Small;
        GUI.color = new Color(0.94f, 0.94f, 0.91f);
        float h = BlockHeight(body, width);
        DrawSelectable(new Rect(0f, y, width, h), body);
        GUI.color = Color.white;

        y += h;
    }

    /// <summary>
    /// Keeps a slow drag selecting. Unity grows the selection only on <c>MouseDrag</c>, and below
    /// about a pixel of travel per frame it emits none at all, so the selection stops part way.
    /// Measured at 233fps: a 13 second drag across 218px produced zero drag events.
    /// </summary>
    private static void ExtendSlowDrag(Rect rect)
    {
        int hot = GUIUtility.hotControl;
        int keyboard = GUIUtility.keyboardControl;
        bool repaint = Event.current.type == EventType.Repaint;

        // GetStateObject allocates an editor for whatever id it is handed, so screen out the
        // controls that cannot be a focused text field before asking for one
        if (!repaint || hot == 0 || hot != keyboard) return;
        if (GUIUtility.GetStateObject(typeof(TextEditor), hot) is not TextEditor editor) return;

        Vector2 mouse = Event.current.mousePosition;
        if (hot != dragControl)
        {
            dragControl = hot;
            dragMouse = mouse;
            return;
        }

        if (!DragSelectPolicy.ShouldExtend(repaint, hot, keyboard, editor.position == rect,
            mouse.x - dragMouse.x, mouse.y - dragMouse.y))
        {
            return;
        }

        dragMouse = mouse;
        editor.SelectToPosition(mouse);
    }

    // measured with the TextArea style, not Text.CalcHeight: the field's padding wraps text
    // narrower than a Label, so measuring with the wrong style clips the last few lines
    private static float BlockHeight(string body, float width)
    {
        return Text.CurTextAreaReadOnlyStyle.CalcHeight(new GUIContent(body), width);
    }

    private static string SourceText(LogEntry entry)
    {
        return entry.Source.IsCallerProvided
            ? entry.Source.File + ":" + entry.Source.Line
            : (string)"CRL_LogViewer_Detail_NoSource".Translate();
    }

    private static void DrawEmpty(Rect rect)
    {
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        GUI.color = new Color(0.54f, 0.56f, 0.58f);
        Widgets.Label(rect, "CRL_LogViewer_NoSelection".Translate());
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
    }
}
