using RimWorks.RimLogging.Bootstrap;
using RimWorks.RimLogging.Capture;
using RimWorks.RimLogging.Patching;

namespace RimWorks.RimLogging.Hijack;

internal static class HijackBootstrap
{
    private static volatile bool _installed;

    internal static bool Install()
    {
        if (_installed) return true;
        if (DegradedMode.AnotherCopyPresent()) return false;

        AssemblyChannelCache.ResolverHook = AssemblyChannelResolver.Resolve;
        AssemblyChannelCache.OnResolverError = (asm, ex) =>
            Verse.Log.Warning($"[RimLogging] channel resolver failed for '{asm.GetName().Name}': {ex.GetType().Name}: {ex.Message}");
        ModNameCache.Provider = ModNameMapProvider.Build;
        ModNameCache.FolderProvider = ModNameMapProvider.BuildFolders;
        ModNameCache.OnProviderError = ex =>
            Verse.Log.Warning($"[RimLogging] mod-name provider failed: {ex.GetType().Name}: {ex.Message}");
        Sinks.VerseLogSink.VanillaWriter = VanillaBufferWriteback.Write;
        VerseLogBackfill.Drain();
        PatchBackends.ApplyBest();
        UnityLogBridge.Install();
        DegradedMode.ClaimHijack();
        _installed = true;
        return true;
    }
}
