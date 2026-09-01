namespace RimWorks.RimLogging.Hijack;

/// <summary>
/// Routes a fully-rendered colored line into vanilla's <c>Verse.Log</c> buffer via reflection-confirmed
/// method dispatch, wrapped in a <see cref="Pipeline.ReentryGuard"/> scope so Harmony prefix patches
/// (Task 5.4) can detect and short-circuit recursive calls. After each vanilla write,
/// <c>Verse.Log.ResetMessageCount</c> is invoked via reflection to prevent duplicate-suppression from
/// silencing fresh entries.
/// </summary>
internal static class VanillaBufferWriteback
{
    /// <summary>Prevents duplicate-suppression by resetting the vanilla message counter after each write.</summary>
    private static readonly System.Reflection.MethodInfo? _resetMessageCount = typeof(Verse.Log).GetMethod(
        "ResetMessageCount",
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

    /// <summary>
    /// Writes <paramref name="coloredLine"/> into the appropriate vanilla log method based on
    /// <paramref name="level"/>, guarded by <see cref="Pipeline.ReentryGuard"/> to prevent recursion.
    /// </summary>
    /// <param name="level">Severity level; determines which <c>Verse.Log</c> method is called.</param>
    /// <param name="coloredLine">Fully-rendered, color-tagged line to write.</param>
    internal static void Write(LogLevel level, string coloredLine)
    {
        using (Pipeline.ReentryGuard.Enter())
        {
            switch (level)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                case LogLevel.Info:
                    Verse.Log.Message(coloredLine);
                    break;
                case LogLevel.Warn:
                    Verse.Log.Warning(coloredLine);
                    break;
                case LogLevel.Error:
                case LogLevel.Fatal:
                    Verse.Log.Error(coloredLine);
                    break;
            }

            _resetMessageCount?.Invoke(null, null);
        }
    }
}
