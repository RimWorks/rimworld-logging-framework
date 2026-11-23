using System;
using Verse;

namespace RimLoggingProbe;

/// <summary>Logs the probe lines that <c>scripts/live-proof.sh</c> checks, once the game has loaded its defs.</summary>
[StaticConstructorOnStartup]
public static class ProbeLines
{
    static ProbeLines()
    {
        Verse.Log.Message("[rimlogging-probe] line1-message");
        Verse.Log.Warning("[rimlogging-probe] line2-warning");
        Verse.Log.Error("[rimlogging-probe] line3-error");
        Verse.Log.WarningOnce("[rimlogging-probe] line4-warning-once", 0x5052_4f42);
        RimWorks.RimLogging.Log.Warn(Thrown(), "[rimlogging-probe] line5-warn-exception");

        bool capture = RimWorks.RimLogging.Logging.CaptureStackTraces;
        RimWorks.RimLogging.Logging.CaptureStackTraces = false;
        try
        {
            Verse.Log.Message("[rimlogging-probe] line6-no-capture");
        }
        finally
        {
            RimWorks.RimLogging.Logging.CaptureStackTraces = capture;
        }

        Verse.Log.Message("[rimlogging-probe] done");
    }

    private static InvalidOperationException Thrown()
    {
        try
        {
            throw new InvalidOperationException("[rimlogging-probe] line5-exception-text");
        }
        catch (InvalidOperationException ex)
        {
            return ex;
        }
    }
}
