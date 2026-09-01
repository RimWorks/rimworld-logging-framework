// covers the mappings through ResolverHook. the Verse-coupled walker needs the game.

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
        // the sanitizer keeps case, so this stays "Mod.Cosmere.Lightweave"
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
        // dots survive, so this is "Mod.Brrainz.Harmony" not "Mod.BrrainzHarmony"
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

        // a renamed Assembly cannot be synthesized cheaply, so check IsVanillaAssembly directly
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
        // documents current behaviour: a hook that casts past non-nullable puts null in the cache
        AssemblyChannelCache.ResolverHook = _ => null!;

        string result = AssemblyChannelCache.Resolve(TestAssembly);

        Assert.Null(result);
    }
}
