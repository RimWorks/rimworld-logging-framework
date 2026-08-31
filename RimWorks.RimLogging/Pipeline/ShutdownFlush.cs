using System;

namespace RimWorks.RimLogging.Pipeline;

internal static class ShutdownFlush
{
    internal static void Install(BackgroundDrain drain)
    {
        if (ShutdownFlushLogic.Installed) return;
        ShutdownFlushLogic.Installed = true;
        UnityEngine.Application.quitting += () => ShutdownFlushLogic.Flush(drain);
        AppDomain.CurrentDomain.ProcessExit += (_, __) => ShutdownFlushLogic.Flush(drain);
    }
}
