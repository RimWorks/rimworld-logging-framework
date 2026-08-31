namespace RimWorks.RimLogging.Capture;

/// <summary>
/// Pure mapping from Unity's <c>LogType</c> ordinal to the framework's <see cref="LogLevel"/>.
/// Takes the raw ordinal int so the helper is testable without referencing <c>UnityEngine</c>.
/// </summary>
internal static class UnityLevelMapping
{
    /// <summary>
    /// Maps a UnityEngine.LogType ordinal to a framework LogLevel.
    /// 0=Error, 1=Assert, 2=Warning, 3=Log, 4=Exception.
    /// </summary>
    internal static LogLevel FromUnityLogTypeId(int unityLogTypeId)
    {
        switch (unityLogTypeId)
        {
            case 0: return LogLevel.Error;   // Error
            case 1: return LogLevel.Error;   // Assert
            case 2: return LogLevel.Warn;    // Warning
            case 4: return LogLevel.Fatal;   // Exception
            default: return LogLevel.Info;   // Log (3) and anything else
        }
    }
}
