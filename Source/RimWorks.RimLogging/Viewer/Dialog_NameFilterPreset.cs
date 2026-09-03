using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimWorks.RimLogging.Viewer;

/// <summary>
/// Asks for a name to save the current filter under. Vanilla's Dialog_Rename needs an
/// IRenameable and force-pauses the game, neither of which suits a dev window.
/// </summary>
internal sealed class Dialog_NameFilterPreset : Window
{
    private const int MaxNameLength = 40;
    private readonly Action<string> onAccepted;
    private bool focused;
    private string name = string.Empty;

    internal Dialog_NameFilterPreset(Action<string> onAccepted)
    {
        this.onAccepted = onAccepted;
        doCloseX = true;
        closeOnAccept = false;
        closeOnClickedOutside = true;
        absorbInputAroundWindow = true;
    }

    public override Vector2 InitialSize => new Vector2(320f, 150f);

    public override void DoWindowContents(Rect inRect)
    {
        bool submitted = Event.current.type == EventType.KeyDown
            && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);
        if (submitted)
        {
            Event.current.Use();
        }

        Text.Font = GameFont.Small;
        Widgets.Label(new Rect(0f, 0f, inRect.width, 30f), "CRL_LogViewer_PresetNamePrompt".Translate());

        GUI.SetNextControlName("PresetName");
        string typed = Widgets.TextField(new Rect(0f, 32f, inRect.width, 32f), name);
        if (typed.Length <= MaxNameLength)
        {
            name = typed;
        }
        if (!focused)
        {
            UI.FocusControl("PresetName", this);
            focused = true;
        }

        if (!Widgets.ButtonText(new Rect(0f, inRect.height - 36f, inRect.width, 32f), "OK".Translate()) && !submitted)
        {
            return;
        }
        if (name.Trim().Length == 0)
        {
            Messages.Message("CRL_LogViewer_PresetNameEmpty".Translate(), MessageTypeDefOf.RejectInput, false);
            return;
        }
        onAccepted(name.Trim());
        Find.WindowStack.TryRemove(this);
    }
}
