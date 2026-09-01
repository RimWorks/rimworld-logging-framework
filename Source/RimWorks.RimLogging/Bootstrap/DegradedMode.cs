using System;

namespace RimWorks.RimLogging.Bootstrap;

internal static class DegradedMode
{
    internal const string HijackClaimKey = "RimWorks.RimLogging.HijackInstalled";

    private static volatile bool _detected;

    internal static bool IsPresent => _detected;

    // Claims the process-wide Verse.Log hijack for this copy. Independent RimLogging copies are byte-loaded
    // into separate Assembly identities but share one AppDomain, so a value in AppDomain data is visible to
    // all of them. The first copy to claim installs the hijack; later copies see the claim and run degraded.
    internal static bool AnotherCopyPresent()
    {
        try
        {
            if (AppDomain.CurrentDomain.GetData(HijackClaimKey) is true)
            {
                _detected = true;
                return true;
            }
        }
        catch
        {
            // AppDomain data access failed; assume no conflicting copy and run normally.
        }
        return false;
    }

    internal static void ClaimHijack()
    {
        AppDomain.CurrentDomain.SetData(HijackClaimKey, true);
    }

    internal static void ReleaseHijackForTests()
    {
        AppDomain.CurrentDomain.SetData(HijackClaimKey, null);
        _detected = false;
    }
}
