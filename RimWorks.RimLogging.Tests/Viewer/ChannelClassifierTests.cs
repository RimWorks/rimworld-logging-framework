using RimWorks.RimLogging.Viewer;
using Xunit;

namespace RimWorks.RimLogging.Tests.Viewer;

public class ChannelClassifierTests
{
    private static void UseMods(params (string PackageId, string Facing)[] mods)
    {
        ChannelClassifier.Reset();
        ChannelClassifier.ModTableLoader = () =>
        {
            foreach ((string packageId, string facing) in mods) ChannelClassifier.AddMod(packageId, facing);
        };
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

        Assert.Equal(new[] { "Vanilla" }, ChannelClassifier.PathFor(channel));
    }

    [Fact]
    public void PathFor_ModPrefixedChannelWithNoRegisteredMod_SplitsOnDotsUnderModGroup()
    {
        UseMods();

        Assert.Equal(new[] { "Mod", "some", "thing" }, ChannelClassifier.PathFor("Mod.some.thing"));
    }

    [Fact]
    public void PathFor_ChannelMatchingAModsFacingId_ExpandsToThatModsPath()
    {
        UseMods(("rimworks.rimlogging", "RimWorks.RimLogging"));

        Assert.Equal(new[] { "Mod", "RimWorks", "RimLogging", "Boot" }, ChannelClassifier.PathFor("RimWorks.RimLogging.Boot"));
    }

    [Fact]
    public void PathFor_ChannelMatchingOnlyTheModuleName_StillExpandsToTheFullModPath()
    {
        UseMods(("rimworks.rimlogging", "RimWorks.RimLogging"));

        Assert.Equal(new[] { "Mod", "RimWorks", "RimLogging", "Pipeline" }, ChannelClassifier.PathFor("RimLogging.Pipeline"));
    }

    [Fact]
    public void PathFor_UnknownDottedChannel_FallsBackToModGroupWithSegmentsIntact()
    {
        UseMods();

        Assert.Equal(new[] { "Mod", "Cosmere", "Roshar" }, ChannelClassifier.PathFor("Cosmere.Roshar"));
    }

    [Fact]
    public void JoinPath_UsesSlashSeparatorSoIdsNestUnambiguously()
    {
        Assert.Equal("Mod/Cosmere/Roshar", ChannelClassifier.JoinPath(new[] { "Mod", "Cosmere", "Roshar" }));
    }

    [Fact]
    public void EnsureBuilt_CalledTwice_OnlyLoadsTheModTableOnce()
    {
        int loads = 0;
        ChannelClassifier.Reset();
        ChannelClassifier.ModTableLoader = () => loads++;

        ChannelClassifier.EnsureBuilt();
        ChannelClassifier.EnsureBuilt();

        Assert.Equal(1, loads);
    }
}
