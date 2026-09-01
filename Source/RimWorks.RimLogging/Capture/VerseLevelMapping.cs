namespace RimWorks.RimLogging.Capture;

internal static class VerseLevelMapping
{
    /// <summary>
    /// Maps a Verse.LogMessageType ordinal to a framework LogLevel.
    /// 0=Message, 1=Warning, 2=Error.
    /// </summary>
    internal static LogLevel FromVerseMessageTypeId(int verseMessageTypeId)
    {
        switch (verseMessageTypeId)
        {
            case 1: return LogLevel.Warn;    // Warning
            case 2: return LogLevel.Error;   // Error
            default: return LogLevel.Info;   // Message (0) and anything else
        }
    }
}
