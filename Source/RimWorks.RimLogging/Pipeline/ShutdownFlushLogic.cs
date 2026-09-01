namespace RimWorks.RimLogging.Pipeline;

internal static class ShutdownFlushLogic
{
    internal const int DrainTimeoutMs = 500;
    internal static volatile bool Installed;

    internal static void Flush(BackgroundDrain drain)
    {
        drain.WaitForDrain(DrainTimeoutMs);
        Sinks.SinkRegistry.FlushAll();
    }

    internal static void ResetInstalledForTests()
    {
        Installed = false;
    }
}
