using RimWorks.RimLogging.Viewer;
using Xunit;

namespace RimWorks.RimLogging.Tests.Viewer;

public class ChannelClassifierTests
{
    private static void UseMods(params (string PackageId, string Facing)[] mods)
    {
        ChannelClassifier.Reset();
        ChannelClassifier.UseModTable(() =>
        {
            foreach ((string packageId, string facing) in mods) ChannelClassifier.AddMod(packageId, facing);
        });
        ChannelClassifier.EnsureBuilt();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("(root)")]
    [InlineData("Vanilla")]
    public void PathFor_RootLikeChannel_MapsToVanillaGroup(string? channel)
    {
        UseMods();

        Assert.Equal(["Vanilla"], ChannelClassifier.PathFor(channel));
    }

    [Fact]
    public void PathFor_ModPrefixedChannelWithNoRegisteredMod_SplitsOnDotsUnderModGroup()
    {
        UseMods();

        Assert.Equal(["Mod", "some", "thing"], ChannelClassifier.PathFor("Mod.some.thing"));
    }

    [Fact]
    public void PathFor_ChannelMatchingAModsFacingId_ExpandsToThatModsPath()
    {
        UseMods(("rimworks.rimlogging", "RimWorks.RimLogging"));

        Assert.Equal(["Mod", "RimWorks", "RimLogging", "Boot"], ChannelClassifier.PathFor("RimWorks.RimLogging.Boot"));
    }

    [Fact]
    public void PathFor_ChannelMatchingOnlyTheModuleName_StillExpandsToTheFullModPath()
    {
        UseMods(("rimworks.rimlogging", "RimWorks.RimLogging"));

        Assert.Equal(["Mod", "RimWorks", "RimLogging", "Pipeline"], ChannelClassifier.PathFor("RimLogging.Pipeline"));
    }

    [Fact]
    public void PathFor_UnknownDottedChannel_FallsBackToModGroupWithSegmentsIntact()
    {
        UseMods();

        Assert.Equal(["Mod", "Cosmere", "Roshar"], ChannelClassifier.PathFor("Cosmere.Roshar"));
    }

    [Fact]
    public void JoinPath_UsesSlashSeparatorSoIdsNestUnambiguously()
    {
        Assert.Equal("Mod/Cosmere/Roshar", ChannelClassifier.JoinPath(["Mod", "Cosmere", "Roshar"]));
    }

    [Fact]
    public void EnsureBuilt_CalledTwice_OnlyLoadsTheModTableOnce()
    {
        int loads = 0;
        ChannelClassifier.Reset();
        ChannelClassifier.UseModTable(() => loads++);

        ChannelClassifier.EnsureBuilt();
        ChannelClassifier.EnsureBuilt();

        Assert.Equal(1, loads);
    }

    private static void UseRootedMods(params (string PackageId, string[] Path, string[] Roots)[] mods)
    {
        ChannelClassifier.Reset();
        ChannelClassifier.UseModTable(() =>
        {
            foreach ((string packageId, string[] path, string[] roots) in mods) ChannelClassifier.AddMod(packageId, path, roots);
        });
        ChannelClassifier.EnsureBuilt();
    }

    [Fact]
    public void PathFor_ChannelUnderARegisteredRoot_ResolvesUnderThatModsPath()
    {
        UseRootedMods(("acme.tools", ["Acme Tools"], ["Acme.Tools"]));

        Assert.Equal(["Mod", "Acme Tools", "Net"], ChannelClassifier.PathFor("Acme.Tools.Net"));
        Assert.Equal(["Mod", "Acme Tools"], ChannelClassifier.PathFor("Acme.Tools"));
    }

    [Fact]
    public void PathFor_RootMatchingIgnoresCase()
    {
        UseRootedMods(("acme.tools", ["Acme Tools"], ["Acme.Tools"]));

        Assert.Equal(["Mod", "Acme Tools", "Net"], ChannelClassifier.PathFor("acme.TOOLS.Net"));
    }

    [Fact]
    public void PathFor_ChannelSharingOnlyPartOfALastRootSegment_DoesNotMatch()
    {
        UseRootedMods(("acme.tools", ["Acme Tools"], ["Acme.Tools"]));

        Assert.Equal(["Mod", "Acme", "Toolsmith"], ChannelClassifier.PathFor("Acme.Toolsmith"));
    }

    [Fact]
    public void PathFor_TwoRootsMatch_TheLongerRootDecides()
    {
        UseRootedMods(
            ("acme", ["Acme"], ["Acme"]),
            ("acme.tools", ["Acme Tools"], ["Acme.Tools"]));

        Assert.Equal(["Mod", "Acme Tools", "Net"], ChannelClassifier.PathFor("Acme.Tools.Net"));
        Assert.Equal(["Mod", "Acme", "Other"], ChannelClassifier.PathFor("Acme.Other"));
    }

    [Fact]
    public void PathFor_ModPathWithADotInItsName_StaysOneNode()
    {
        UseRootedMods(("dr.acme", ["Dr. Acme"], ["Acme"]));

        Assert.Equal(["Mod", "Dr. Acme", "Net"], ChannelClassifier.PathFor("Acme.Net"));
    }

    [Fact]
    public void PathFor_AfterReset_RegisteredRootsNoLongerMatch()
    {
        UseRootedMods(("acme.tools", ["Acme Tools"], ["Acme.Tools"]));
        ChannelClassifier.Reset();
        UseMods();

        Assert.Equal(["Mod", "Acme", "Tools", "Net"], ChannelClassifier.PathFor("Acme.Tools.Net"));
    }
}
