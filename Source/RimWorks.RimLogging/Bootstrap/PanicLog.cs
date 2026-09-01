namespace RimWorks.RimLogging.Bootstrap;

/// <summary>
/// Reports framework startup failures. Goes through Unity because the Verse.Log hijack
/// cancels Verse.Log, which would let a broken bootstrap hide its own error.
/// </summary>
internal static class PanicLog
{
    internal static void Write(string message)
    {
        UnityEngine.Debug.LogError(message);
    }
}
