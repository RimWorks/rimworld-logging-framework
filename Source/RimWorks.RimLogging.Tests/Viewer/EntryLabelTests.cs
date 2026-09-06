using RimWorks.RimLogging.Viewer;
using Xunit;

namespace RimWorks.RimLogging.Tests.Viewer;

/// <summary>
/// Guards the entry list's channel column. "Mod.cebarks.rimworld.linuxmisc" truncates to
/// something unreadable, so a mod entry shows the mod's own name instead.
/// </summary>
public class EntryLabelTests
{
    [Fact]
    public void AModChannel_ShowsTheModName()
    {
        Assert.Equal("Mod: Linux Performance Optimizations",
            EntryLabel.Channel("Mod.cebarks.rimworld.linuxmisc", "Linux Performance Optimizations"));
    }

    [Fact]
    public void ANonModChannel_IsLeftAlone()
    {
        // Vanilla and Unity entries carry a mod name too, and their channel is already readable
        Assert.Equal("Vanilla", EntryLabel.Channel("Vanilla", "Some Mod"));
        Assert.Equal("default", EntryLabel.Channel("default", "Some Mod"));
    }

    [Fact]
    public void AChannelMerelyStartingWithMod_IsNotAModChannel()
    {
        Assert.Equal("Modular.Thing", EntryLabel.Channel("Modular.Thing", "Some Mod"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void UnknownModName_FallsBackToTheChannel(string? modName)
    {
        // losing the packageId too would leave the row with no identifier at all
        Assert.Equal("Mod.cebarks.rimworld.linuxmisc",
            EntryLabel.Channel("Mod.cebarks.rimworld.linuxmisc", modName));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NoChannel_GivesEmptyRatherThanNull(string? channel)
    {
        Assert.Equal(string.Empty, EntryLabel.Channel(channel, "Some Mod"));
    }
}
