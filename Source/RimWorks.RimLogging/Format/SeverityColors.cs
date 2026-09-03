namespace RimWorks.RimLogging.Format;

/// <summary>Maps log severity levels to muted hex color strings for Unity rich-text tags.</summary>
public static class SeverityColors
{
    /// <summary>Returns a hex color string (no <c>#</c> prefix) for the given <paramref name="level"/>.</summary>
    public static string GetHex(LogLevel level) => level switch
    {
        LogLevel.Trace => "808080",   // muted gray
        LogLevel.Debug => "8AA9C8",   // muted blue
        LogLevel.Info => "A5C2A5",   // muted green
        LogLevel.Warn => "D8C36C",   // muted yellow
        LogLevel.Error => "C97373",   // muted red
        LogLevel.Fatal => "9F4FBF",   // muted magenta
        _ => "C7C7C7",
    };
}
