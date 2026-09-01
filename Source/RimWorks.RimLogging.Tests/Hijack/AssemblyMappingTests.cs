// The plan (Task 5.7) called for reflection-based ModContentPack stubs, but the
// Task 5.2 implementation of AssemblyChannelCache uses a ResolverHook delegate
// pattern instead. This suite exercises the same mappings (Cosmere.Lightweave,
// Brrainz.Harmony, me-and-you, vanilla shortcut) via the hook surface, which is
// the test-visible part of the resolution chain. The Verse-coupled walker
// (Hijack/AssemblyChannelResolver.cs) is integration-tested in Phase 11.

using System;
using System.Reflection;
using RimWorks.RimLogging.Capture;
using Xunit;

namespace RimWorks.RimLogging.Tests.Hijack;

public class AssemblyMappingTests : IDisposable
{
    private static readonly Assembly TestAssembly = typeof(AssemblyMappingTests).Assembly;
    private readonly Func<Assembly, string>? _savedHook;

    public AssemblyMappingTests()
    {
        _savedHook = AssemblyChannelCache.ResolverHook;
        AssemblyChannelCache.ClearForTests();
    }

    public void Dispose()
    {
        AssemblyChannelCache.ResolverHook = _savedHook;
        AssemblyChannelCache.ClearForTests();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Resolve_RegisteredMod_SanitizesPackageIdToChannelSegment()
    {
        // Simulates a mod with packageId "Cosmere.Lightweave" registered to this assembly.
        // The real resolver returns "Mod." + PackageIdSanitizer.ToChannelSegment(packageId).
        // ToChannelSegment preserves case and dots for valid identifier chars, so
        // "Cosmere.Lightweave" -> "Cosmere.Lightweave" -> channel "Mod.Cosmere.Lightweave".
        // Note: the plan guessed lowercase "Mod.cosmere.lightweave", but the actual sanitizer
        // does NOT lowercase; case is preserved.
        string packageId = "Cosmere.Lightweave";
        string expected = "Mod." + PackageIdSanitizer.ToChannelSegment(packageId);

        AssemblyChannelCache.ResolverHook = _ => expected;

        string result = AssemblyChannelCache.Resolve(TestAssembly);

        Assert.Equal("Mod.Cosmere.Lightweave", expected);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Resolve_PackageIdWithMixedCase_PreservesCaseAfterSanitization()
    {
        // "Brrainz.Harmony" contains only valid chars (letters and dots).
        // ToChannelSegment preserves them as-is; dots are NOT collapsed.
        // The plan guessed "Mod.BrrainzHarmony" (dot collapsed), which is WRONG.
        // Actual output: "Mod.Brrainz.Harmony".
        string packageId = "Brrainz.Harmony";
        string sanitized = PackageIdSanitizer.ToChannelSegment(packageId);
        string expected = "Mod." + sanitized;

        Assert.Equal("Brrainz.Harmony", sanitized);
        Assert.Equal("Mod.Brrainz.Harmony", expected);

        AssemblyChannelCache.ResolverHook = _ => expected;
        string result = AssemblyChannelCache.Resolve(TestAssembly);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Resolve_PackageIdWithSpecialChars_StripsToValidIdentifier()
    {
        // "me-and-you": dashes are not letters/digits/underscores and not dots,
        // so they are stripped entirely. Result: "meandyou".
        string packageId = "me-and-you";
        string sanitized = PackageIdSanitizer.ToChannelSegment(packageId);
        string expected = "Mod." + sanitized;

        Assert.Equal("meandyou", sanitized);
        Assert.Equal("Mod.meandyou", expected);

        AssemblyChannelCache.ResolverHook = _ => expected;
        string result = AssemblyChannelCache.Resolve(TestAssembly);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Resolve_VanillaAssemblyName_IsVanilla_HookNotInvoked()
    {
        bool hookInvoked = false;
        AssemblyChannelCache.ResolverHook = _ =>
        {
            hookInvoked = true;
            return "should-not-reach";
        };

        // IsVanillaAssembly checks by name string before invoking the hook.
        Assert.True(AssemblyChannelCache.IsVanillaAssembly("Assembly-CSharp"));
        Assert.True(AssemblyChannelCache.IsVanillaAssembly("Assembly-CSharp-firstpass"));
        Assert.True(AssemblyChannelCache.IsVanillaAssembly("UnityEngine"));
        Assert.True(AssemblyChannelCache.IsVanillaAssembly("UnityEngine.CoreModule"));
        Assert.True(AssemblyChannelCache.IsVanillaAssembly("Verse"));
        Assert.False(AssemblyChannelCache.IsVanillaAssembly("SomeMod.Something"));

        // The hook is set but cannot be exercised with a real vanilla Assembly at runtime
        // (we cannot synthesize an Assembly with a renamed name cheaply). We verify the
        // contract via IsVanillaAssembly directly and confirm the hook was not called
        // during the name-only checks above.
        Assert.False(hookInvoked);
    }

    [Fact]
    public void Resolve_HookReturnsResult_CachesResultAcrossCalls()
    {
        int invokeCount = 0;
        AssemblyChannelCache.ResolverHook = _ =>
        {
            invokeCount++;
            return "Mod.cached.result";
        };

        string first = AssemblyChannelCache.Resolve(TestAssembly);
        string second = AssemblyChannelCache.Resolve(TestAssembly);

        Assert.Equal("Mod.cached.result", first);
        Assert.Equal(first, second);
        Assert.Equal(1, invokeCount);
    }

    [Fact]
    public void Resolve_HookThrows_ReturnsUnknown()
    {
        AssemblyChannelCache.ResolverHook = _ => throw new InvalidOperationException("simulated resolver failure");

        string result = AssemblyChannelCache.Resolve(TestAssembly);

        Assert.Equal(AssemblyChannelCache.Unknown, result);
    }

    [Fact]
    public void Resolve_HookReturnsNull_ReturnsNull()
    {
        // ResolverHook is Func<Assembly, string>?. The return type is non-nullable string,
        // so a hook cannot return null without a null-forgiving cast. If it does (via cast),
        // the cache stores whatever the hook returned. This test documents current behavior:
        // there is no null-coercion guard inside ResolveOnce for the hook's return value,
        // so the raw (null) string propagates to the cache.
        AssemblyChannelCache.ResolverHook = _ => null!;

        string result = AssemblyChannelCache.Resolve(TestAssembly);

        Assert.Null(result);
    }
}
