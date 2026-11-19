using System;
using RimWorks.RimLogging.Capture;
using Xunit;

namespace RimWorks.RimLogging.Tests.Capture;

public class CallerFrameClassifierTests : IDisposable
{
    public void Dispose()
    {
        CallerFrameClassifier.HostFrameHook = null;
        CallerFrameClassifier.OnHostFrameHookError = null;
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void IsInternalFrame_HarmonyLibNamespace_IsSkipped()
    {
        Assert.True(CallerFrameClassifier.IsInternalFrame("HarmonyLib.PatchProcessor", "0Harmony"));
        Assert.True(CallerFrameClassifier.IsInternalFrame("HarmonyLib.Tools.SomeType", "0Harmony"));
    }

    [Fact]
    public void IsInternalFrame_FrameworkNamespace_IsSkipped()
    {
        Assert.True(CallerFrameClassifier.IsInternalFrame("RimWorks.RimLogging.Hijack.VerseLog_Message_Patch", "RimWorks.RimLogging"));
        Assert.True(CallerFrameClassifier.IsInternalFrame("RimWorks.RimLogging.Log", "RimWorks.RimLogging"));
    }

    [Fact]
    public void IsInternalFrame_HarmonyAssemblyButNonHarmonyLibNamespace_IsSkipped()
    {
        // regression: 0Harmony bundles MonoMod and Cecil, so every patched call blamed brrainz.harmony
        Assert.True(CallerFrameClassifier.IsInternalFrame("MonoMod.Core.Platforms.PlatformTriple", "0Harmony"));
        Assert.True(CallerFrameClassifier.IsInternalFrame("MonoMod.Cil.ILContext", "0Harmony"));
        Assert.True(CallerFrameClassifier.IsInternalFrame("Mono.Cecil.MethodDefinition", "0Harmony"));
        Assert.True(CallerFrameClassifier.IsInternalFrame("Iced.Intel.Decoder", "0Harmony"));
        Assert.True(CallerFrameClassifier.IsInternalFrame("AssemblyInfo", "0Harmony"));
        Assert.True(CallerFrameClassifier.IsInternalFrame(null, "0Harmony"));
    }

    [Fact]
    public void IsInternalFrame_HarmonyGeneratedCollectibleAssemblyType_IsSkipped()
    {
        Assert.True(CallerFrameClassifier.IsInternalFrame("HarmonyGenerated", "DMD<Verse.Log::Message>"));
        Assert.True(CallerFrameClassifier.IsInternalFrame("HarmonyGenerated", null));
    }

    [Fact]
    public void IsInternalFrame_HarmonyGeneratedPrefixCollision_IsNotSkipped()
    {
        Assert.False(CallerFrameClassifier.IsInternalFrame("HarmonyGeneratedThings.Patcher", "SomeMod"));
    }

    [Fact]
    public void IsInternalFrame_NullNamespace_IsSkipped()
    {
        // DynamicMethod-emitted Harmony stubs frequently surface with a null DeclaringType.
        Assert.True(CallerFrameClassifier.IsInternalFrame(null, "SomeMod"));
        Assert.True(CallerFrameClassifier.IsInternalFrame(null, null));
    }

    [Fact]
    public void IsInternalFrame_MonoModNamespaceFromOutsideHarmony_StillSkipped()
    {
        // MonoMod is only ever loaded as a Harmony transitive. Skipping by namespace too
        // guarantees we never attribute MonoMod frames to whatever mod happens to host them.
        Assert.True(CallerFrameClassifier.IsInternalFrame("MonoMod.Utils.Anything", "SomeOtherAsm"));
    }

    [Fact]
    public void IsInternalFrame_ConcordNamespace_IsSkipped()
    {
        // Concord weaves the Verse.Log hijack wrappers, so its frames sit between the real
        // caller and the sink and must be skipped like the legacy Harmony ones.
        Assert.True(CallerFrameClassifier.IsInternalFrame("Concord.Emit.WrapperComposer", "Concord"));
        Assert.True(CallerFrameClassifier.IsInternalFrame("Concord.RimWorld.RimWorldAdapter", "ConcordRimWorld"));
    }

    [Fact]
    public void IsInternalFrame_RegularModFrame_IsNotSkipped()
    {
        Assert.False(CallerFrameClassifier.IsInternalFrame("Verse.Log", "Assembly-CSharp"));
        Assert.False(CallerFrameClassifier.IsInternalFrame("HugsLib.Logs.Logger", "HugsLib"));
        Assert.False(CallerFrameClassifier.IsInternalFrame("RimThemes.PatchClass", "RimThemes"));
        Assert.False(CallerFrameClassifier.IsInternalFrame("UnityEngine.Debug", "UnityEngine.CoreModule"));
    }

    [Fact]
    public void IsInternalFrame_HarmonyLibPrefixCollision_IsNotSkipped()
    {
        // Defensive: a mod that happens to have a type literally named "HarmonyLib"
        // (no dot suffix) should not be skipped: the namespace check requires the dot.
        Assert.False(CallerFrameClassifier.IsInternalFrame("HarmonyLib", "SomeMod"));
        Assert.False(CallerFrameClassifier.IsInternalFrame("HarmonyLibrarian.Tools", "SomeMod"));
    }

    [Fact]
    public void IsInternalFrame_OurOwnPatchBackendFrames_AreSkipped()
    {
        // the backends live in separate assemblies, so without this the mod that logged
        // would be attributed to RimLogging itself
        Assert.True(CallerFrameClassifier.IsInternalFrame(
            "RimWorks.RimLogging.Patches.Concord.VerseLogPatch", "RimWorks.RimLogging.Patches.Concord"));
        Assert.True(CallerFrameClassifier.IsInternalFrame(
            "RimWorks.RimLogging.Patches.Harmony.HarmonyBackend", "RimWorks.RimLogging.Patches.Harmony"));
    }

    [Fact]
    public void IsInternalFrame_HostHookMarksTypeAndAssembly_SkipsOnlyThose()
    {
        CallerFrameClassifier.HostFrameHook = (type, asm) => type == "Acme.Log" || asm == "Acme.Logging";

        Assert.True(CallerFrameClassifier.IsInternalFrame("Acme.Log", "Acme.Game"));
        Assert.True(CallerFrameClassifier.IsInternalFrame("Acme.Other", "Acme.Logging"));
        Assert.False(CallerFrameClassifier.IsInternalFrame("Acme.LogHelper", "Acme.Game"));
        Assert.False(CallerFrameClassifier.IsInternalFrame("Verse.Log", "Assembly-CSharp"));
    }

    [Fact]
    public void IsInternalFrame_HostHookSet_KeepsEveryBuiltInSkip()
    {
        CallerFrameClassifier.HostFrameHook = (_, _) => false;

        Assert.True(CallerFrameClassifier.IsInternalFrame("HarmonyLib.PatchProcessor", "0Harmony"));
        Assert.True(CallerFrameClassifier.IsInternalFrame("RimWorks.RimLogging.Log", "RimWorks.RimLogging"));
        Assert.True(CallerFrameClassifier.IsInternalFrame(null, "SomeMod"));
        Assert.False(CallerFrameClassifier.IsInternalFrame("Verse.Log", "Assembly-CSharp"));
    }

    [Fact]
    public void IsInternalFrame_HostHookUnset_NeverConsultedForNullType()
    {
        bool called = false;
        CallerFrameClassifier.HostFrameHook = (_, _) => called = true;

        Assert.True(CallerFrameClassifier.IsInternalFrame(null, "SomeMod"));
        Assert.False(called);
    }

    [Fact]
    public void IsInternalFrame_HostHookThrows_ReturnsBuiltInAnswerAndReports()
    {
        Exception? reported = null;
        CallerFrameClassifier.HostFrameHook = (_, _) => throw new InvalidOperationException("host hook failed");
        CallerFrameClassifier.OnHostFrameHookError = ex => reported = ex;

        Assert.False(CallerFrameClassifier.IsInternalFrame("Acme.Log", "Acme.Logging"));
        Assert.True(CallerFrameClassifier.IsInternalFrame("HarmonyLib.PatchProcessor", "0Harmony"));
        Assert.IsType<InvalidOperationException>(reported);
    }

    [Fact]
    public void IsInternalFrame_HostHookThrowsWithNoErrorHook_DoesNotThrow()
    {
        CallerFrameClassifier.HostFrameHook = (_, _) => throw new InvalidOperationException("host hook failed");

        Assert.False(CallerFrameClassifier.IsInternalFrame("Acme.Log", "Acme.Logging"));
    }
}
