using RimWorks.RimLogging.Bootstrap;
using RimWorks.RimLogging.Capture;
using RimWorks.RimLogging.Patching;

namespace RimWorks.RimLogging.Hijack;

internal static class HijackBootstrap
{
    private static volatile bool _installed;

    // the two provider hooks below report through Unity, not our own pipeline. they fire during
    // channel and mod resolution, which an emit performs, so logging normally would recurse.
    internal static bool Install()
    {
        if (_installed) return true;
        if (DegradedMode.AnotherCopyPresent()) return false;

        AssemblyChannelCache.ResolverHook = AssemblyChannelResolver.Resolve;
        AssemblyChannelCache.OnResolverError = (asm, ex) =>
            PanicLog.Warn($"[RimLogging] channel resolver failed for '{asm.GetName().Name}': {ex.GetType().Name}: {ex.Message}");
        ModNameCache.Provider = ModNameMapProvider.Build;
        ModNameCache.FolderProvider = ModNameMapProvider.BuildFolders;
        ModNameCache.PackageIdProvider = ModNameMapProvider.BuildPackageIds;
        ModNameCache.OnProviderError = ex =>
            PanicLog.Warn($"[RimLogging] mod-name provider failed: {ex.GetType().Name}: {ex.Message}");
        Sinks.VerseLogSink.VanillaWriter = VanillaBufferWriteback.Write;
        VerseLogBackfill.Drain();
        PatchBackends.ApplyBest();
        UnityLogBridge.Install();
        DegradedMode.ClaimHijack();
        _installed = true;
        return true;
    }
}
