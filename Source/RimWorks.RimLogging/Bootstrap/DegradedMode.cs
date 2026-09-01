using System;

namespace RimWorks.RimLogging.Bootstrap;

internal static class DegradedMode
{
    internal const string HijackClaimKey = "RimWorks.RimLogging.HijackInstalled";

    private static volatile bool _detected;

    internal static bool IsPresent => _detected;

    // Separate copies load as separate assemblies but share one AppDomain, so a value there is
    // visible to all of them. First copy to claim installs the hijack, the rest run degraded.
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
