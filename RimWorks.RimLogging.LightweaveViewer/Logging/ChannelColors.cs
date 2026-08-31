using Cosmere.Lightweave;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using UnityEngine;

namespace RimWorks.RimLogging.LightweaveViewer;

internal static class ChannelColors {
    public static Color For(string? name) {
        Cosmere.Lightweave.Theme.Theme theme = RenderContext.Current.Theme;
        if (string.IsNullOrEmpty(name)) {
            return theme.GetColor(ThemeSlot.TextSecondary);
        }
        return theme.GetColor(ThemeSlot.SurfaceAccent);
    }
}
