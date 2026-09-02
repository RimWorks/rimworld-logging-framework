namespace RimWorks.RimLogging.Viewer;

/// <summary>Picks a spinner glyph from a wall-clock reading. Pure so it tests without Unity.</summary>
internal static class Spinner {
    private const float FramesPerSecond = 10f;
    private static readonly char[] Frames = { '|', '/', '-', '\\' };

    /// <summary>Returns the glyph for the given time in seconds, cycling ten frames a second.</summary>
    internal static char Frame(float seconds) {
        if (seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds)) {
            return Frames[0];
        }

        // modulo before the cast keeps a large realtimeSinceStartup from overflowing the int
        return Frames[(int)(seconds * FramesPerSecond % Frames.Length)];
    }
}
