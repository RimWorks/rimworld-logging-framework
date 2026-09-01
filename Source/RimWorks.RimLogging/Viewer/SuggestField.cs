using UnityEngine;
using Verse;

namespace RimWorks.RimLogging.Viewer;

/// <summary>A text field with placeholder text and a completion dropdown.</summary>
/// <remarks>Draw the field, draw the panes, then call <see cref="DrawOverlay"/> so the list paints on top.</remarks>
internal sealed class SuggestField {
    private const float RowHeight = 20f;
    private const int MaxRows = 8;

    private readonly string controlName;

    private Rect fieldRect;
    private Suggestions suggestions = Suggestions.None;
    private string current = string.Empty;
    private int highlight;
    private bool dismissed;
    private int pendingAccept = -1;

    public SuggestField(string controlName) {
        this.controlName = controlName;
    }

    private bool Focused => GUI.GetNameOfFocusedControl() == controlName;

    private bool IsOpen => Focused && !dismissed && suggestions.Any;

    private int VisibleRows => Mathf.Min(suggestions.Items.Count, MaxRows);

    private Rect DropdownRect() {
        return new Rect(fieldRect.x, fieldRect.yMax + 1f, fieldRect.width, VisibleRows * RowHeight);
    }

    /// <summary>Claims mouse events over the dropdown before the panes below can consume them.</summary>
    public void ReserveClicks() {
        if (!IsOpen || !Mouse.IsOver(DropdownRect())) {
            return;
        }

        EventType type = Event.current.type;
        if (type != EventType.MouseDown && type != EventType.MouseUp && type != EventType.MouseDrag) {
            return;
        }

        if (type == EventType.MouseUp) {
            Rect box = DropdownRect();
            int index = Mathf.FloorToInt((Event.current.mousePosition.y - box.y) / RowHeight);
            if (index >= 0 && index < VisibleRows) {
                pendingAccept = index;
            }
        }

        Event.current.Use();
    }


    public string Draw(Rect rect, string value, string placeholderKey, Suggestions suggest) {
        fieldRect = rect;
        suggestions = suggest;

        if (highlight >= suggest.Items.Count) {
            highlight = 0;
        }

        // A key handler used to return before the TextField below. That skipped a control id, and
        // IMGUI ids are positional, so it shifted the id of every scroll view drawn after this.
        string afterKeys = HandleKeys(value);
        bool keysChanged = afterKeys != value;

        GUI.SetNextControlName(controlName);
        string next = Widgets.TextField(rect, keysChanged ? afterKeys : value);
        if (!keysChanged && next != value) {
            dismissed = false;
            highlight = 0;
        }

        string result = keysChanged ? afterKeys : next;
        DrawPlaceholder(rect, result, placeholderKey);
        current = result;
        return result;
    }

    /// <summary>Paints the dropdown and returns an accepted value, or <c>null</c> when nothing was picked.</summary>
    public string? DrawOverlay() {
        if (!IsOpen) {
            return null;
        }

        int rows = VisibleRows;
        Rect box = DropdownRect();

        Widgets.DrawBoxSolid(box, new Color(0.08f, 0.09f, 0.11f, 0.98f));
        Widgets.DrawBox(box);

        Text.Font = GameFont.Small;

        for (int i = 0; i < rows; i++) {
            Rect row = new Rect(box.x, box.y + i * RowHeight, box.width, RowHeight);
            if (i == highlight) {
                Widgets.DrawHighlightSelected(row);
            }
            else if (Mouse.IsOver(row)) {
                Widgets.DrawHighlight(row);
            }

            Widgets.Label(new Rect(row.x + 5f, row.y - 2f, row.width - 10f, RowHeight), suggestions.Items[i]);
        }

        string? accepted = null;
        if (pendingAccept >= 0 && pendingAccept < suggestions.Items.Count) {
            accepted = Accept(pendingAccept);
        }
        pendingAccept = -1;

        if (suggestions.Items.Count > rows) {
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.42f, 0.44f, 0.46f);
            Widgets.Label(new Rect(box.x + 5f, box.yMax, box.width, 16f), "+" + (suggestions.Items.Count - rows));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        return accepted;
    }

    private string HandleKeys(string value) {
        if (!IsOpen || Event.current.type != EventType.KeyDown) {
            return value;
        }

        switch (Event.current.keyCode) {
            case KeyCode.Tab:
                Event.current.Use();
                return Accept(highlight);
            case KeyCode.DownArrow:
                highlight = (highlight + 1) % suggestions.Items.Count;
                Event.current.Use();
                return value;
            case KeyCode.UpArrow:
                highlight = (highlight - 1 + suggestions.Items.Count) % suggestions.Items.Count;
                Event.current.Use();
                return value;
            case KeyCode.Escape:
                dismissed = true;
                Event.current.Use();
                return value;
            default:
                return value;
        }
    }

    private string Accept(int index) {
        string next = suggestions.Apply(current, suggestions.Items[index]);
        dismissed = true;
        highlight = 0;
        current = next;
        SyncEditorCaret(next);
        return next;
    }

    /// <summary>The live TextEditor caches its own copy, so a programmatic edit has to be pushed into it.</summary>
    private static void SyncEditorCaret(string next) {
        if (GUIUtility.keyboardControl == 0) {
            return;
        }

        if (GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) is not TextEditor editor) {
            return;
        }

        editor.text = next;
        editor.SelectNone();
        editor.MoveTextEnd();
    }

    private static void DrawPlaceholder(Rect rect, string value, string placeholderKey) {
        if (!string.IsNullOrEmpty(value)) {
            return;
        }

        Text.Font = GameFont.Tiny;
        GUI.color = new Color(0.42f, 0.44f, 0.46f);
        Widgets.Label(new Rect(rect.x + 5f, rect.y + 3f, rect.width - 10f, rect.height), placeholderKey.Translate());
        GUI.color = Color.white;
        Text.Font = GameFont.Small;
    }
}
