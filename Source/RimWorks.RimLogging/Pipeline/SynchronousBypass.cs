namespace RimWorks.RimLogging.Pipeline;

internal static class SynchronousBypass
{
    internal static bool ShouldBypass(LogLevel level) => level >= LogLevel.Error;
}
