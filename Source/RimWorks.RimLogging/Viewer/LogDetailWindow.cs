using LudeonTK;
using RimWorks.RimLogging.Settings;
using UnityEngine;
using Verse;

namespace RimWorks.RimLogging.Viewer;

/// <summary>Detail pane detached into its own dev window. Reads the selection straight off the viewer's state.</summary>
internal sealed class LogDetailWindow : EditWindow
{
    private readonly LogViewerState state;
    private Vector2 scroll;

    public LogDetailWindow(LogViewerState state)
    {
        this.state = state;
        optionalTitle = "CRL_LogViewer_DetailWindowTitle".Translate();
        onlyOneOfTypeAllowed = true;
    }

    public override Vector2 InitialSize => new Vector2(560f, 360f);

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Small;
        LogDetailView.Draw(inRect, state.Selected, ref scroll, LoggingMod.Settings.logViewerCombinedDetail);
    }
}
