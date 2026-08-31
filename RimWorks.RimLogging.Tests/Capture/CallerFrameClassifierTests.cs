using RimWorks.RimLogging.Capture;
using Xunit;

namespace RimWorks.RimLogging.Tests.Capture;

public class CallerFrameClassifierTests
{
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
        // Regression: 0Harmony.dll bundles MonoMod / Mono.Cecil / Iced / Microsoft.Cci /
        // System.* polyfill types whose namespaces do NOT start with "HarmonyLib.". Before
        // the assembly-name skip, ResolveCallerChannel picked up these frames during patched
        // dispatch and resolved every patched call to the brrainz.harmony mod (which loads
        // 0Harmony.dll), misattributing logs from every other mod.
        Assert.True(CallerFrameClassifier.IsInternalFrame("MonoMod.Core.Platforms.PlatformTriple", "0Harmony"));
        Assert.True(CallerFrameClassifier.IsInternalFrame("MonoMod.Cil.ILContext", "0Harmony"));
        Assert.True(CallerFrameClassifier.IsInternalFrame("Mono.Cecil.MethodDefinition", "0Harmony"));
        Assert.True(CallerFrameClassifier.IsInternalFrame("Iced.Intel.Decoder", "0Harmony"));
        Assert.True(CallerFrameClassifier.IsInternalFrame("AssemblyInfo", "0Harmony"));
        Assert.True(CallerFrameClassifier.IsInternalFrame(null, "0Harmony"));
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
}
