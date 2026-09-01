// regression: every modded Verse.Log call resolved to "Mod.Unknown"

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
