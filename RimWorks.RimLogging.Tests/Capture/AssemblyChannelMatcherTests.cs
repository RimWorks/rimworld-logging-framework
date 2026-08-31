// Regression coverage for the assembly->channel resolution that previously
// returned "Mod.Unknown" for every modded Verse.Log call: the Verse-bound
// resolver was never wired into AssemblyChannelCache.ResolverHook, and its
// matching loop lived in Hijack/ (excluded from this test project, hence
// untested). The loop is now AssemblyChannelMatcher.Match in Capture/, and the
// Verse shell (Hijack/AssemblyChannelResolver) only projects RunningMods into
// it. The wiring itself (HijackBootstrap.Install) stays integration-only because
// it depends on Harmony + Verse, but the resolution logic is covered here.

using System.Collections.Generic;
using System.Reflection;
using RimWorks.RimLogging.Capture;
using Xunit;

namespace RimWorks.RimLogging.Tests.Capture;

public class AssemblyChannelMatcherTests
{
    private static readonly Assembly TargetAssembly = typeof(AssemblyChannelMatcherTests).Assembly;
    private static readonly Assembly OtherAssembly = typeof(Xunit.FactAttribute).Assembly;

    [Fact]
    public void Match_AssemblyOwnedByMod_ReturnsSanitizedModChannel()
    {
        List<(string, IReadOnlyList<Assembly>)> mods =
        [
            ("Cosmere.Lightweave", new[] { TargetAssembly }),
        ];

        string result = AssemblyChannelMatcher.Match(TargetAssembly, mods);

        Assert.Equal("Mod.Cosmere.Lightweave", result);
    }

    [Fact]
    public void Match_AssemblyNotOwnedByAnyMod_ReturnsUnknown()
    {
        List<(string, IReadOnlyList<Assembly>)> mods =
        [
            ("Cosmere.Lightweave", new[] { OtherAssembly }),
        ];

        string result = AssemblyChannelMatcher.Match(TargetAssembly, mods);

        Assert.Equal(AssemblyChannelCache.Unknown, result);
    }

    [Fact]
    public void Match_EmptyModList_ReturnsUnknown()
    {
        string result = AssemblyChannelMatcher.Match(TargetAssembly, []);

        Assert.Equal(AssemblyChannelCache.Unknown, result);
    }

    [Fact]
    public void Match_PackageIdWithInvalidChars_IsSanitized()
    {
        List<(string, IReadOnlyList<Assembly>)> mods =
        [
            ("me-and-you", new[] { TargetAssembly }),
        ];

        string result = AssemblyChannelMatcher.Match(TargetAssembly, mods);

        Assert.Equal("Mod.meandyou", result);
    }
}
