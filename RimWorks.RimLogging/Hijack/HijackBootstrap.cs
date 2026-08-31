using Concord;
using RimWorks.RimLogging.Bootstrap;
using RimWorks.RimLogging.Capture;

namespace RimWorks.RimLogging.Hijack;

internal static class HijackBootstrap
{
    private static volatile bool _installed;
    private static IPatchHandle? _patches;

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
        _patches = Patcher.Apply(typeof(HijackBootstrap).Assembly);
        UnityLogBridge.Install();
        DegradedMode.ClaimHijack();
        _installed = true;
        return true;
    }

    internal static void UninstallForTests()
    {
        UnityLogBridge.Uninstall();
        _patches?.Dispose();
        _patches = null;
        Sinks.VerseLogSink.VanillaWriter = null;
        AssemblyChannelCache.OnResolverError = null;
        ModNameCache.OnProviderError = null;
        DegradedMode.ReleaseHijackForTests();
        _installed = false;
    }
}
