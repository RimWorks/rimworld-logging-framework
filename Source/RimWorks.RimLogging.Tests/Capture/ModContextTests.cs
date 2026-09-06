using System.Collections.Generic;
using RimWorks.RimLogging.Capture;
using Xunit;

namespace RimWorks.RimLogging.Tests.Capture;

/// <summary>
/// Guards the mod_id context key. It is injected at capture so a filter can say
/// ctx.mod_id = "cebarks.*" and so the id survives into the file sinks.
/// </summary>
public class ModContextTests
{
    [Fact]
    public void NoContextYet_MakesOneHoldingTheId()
    {
        IReadOnlyDictionary<string, object?>? result = ModContext.WithModId(null, "cebarks.rimworld.linuxmisc");

        Assert.Equal("cebarks.rimworld.linuxmisc", Assert.Single(result!)!.Value);
    }

    [Fact]
    public void ExistingContext_KeepsEveryKeyAndGainsTheId()
    {
        Dictionary<string, object?> existing = new Dictionary<string, object?> { ["pawn"] = "Randy" };

        IReadOnlyDictionary<string, object?>? result = ModContext.WithModId(existing, "a.b");

        Assert.Equal("Randy", result!["pawn"]);
        Assert.Equal("a.b", result[ModContext.ModIdKey]);
    }

    [Fact]
    public void ExistingContext_IsNotMutated()
    {
        Dictionary<string, object?> existing = new Dictionary<string, object?> { ["pawn"] = "Randy" };

        ModContext.WithModId(existing, "a.b");

        Assert.Single(existing);
    }

    [Fact]
    public void CallerSuppliedModId_Wins()
    {
        Dictionary<string, object?> existing = new Dictionary<string, object?> { ["mod_id"] = "theirs" };

        IReadOnlyDictionary<string, object?>? result = ModContext.WithModId(existing, "ours");

        Assert.Equal("theirs", result![ModContext.ModIdKey]);
    }

    [Fact]
    public void CallerSuppliedModId_WinsWhateverItsCasing()
    {
        // the filter language matches context keys case-insensitively, so MOD_ID would shadow ours
        Dictionary<string, object?> existing = new Dictionary<string, object?> { ["MOD_ID"] = "theirs" };

        IReadOnlyDictionary<string, object?>? result = ModContext.WithModId(existing, "ours");

        Assert.Equal("theirs", Assert.Single(result!)!.Value);
    }

    [Fact]
    public void OurOwnChannel_FallsBackToOurOwnPackageId()
    {
        // the caller walk skips RimLogging frames, so our entries never resolve a packageId
        Assert.Equal("rimworks.rimlogging",
            ModContext.Fallback("RimWorks.RimLogging", "RimWorks.RimLogging", "rimworks.rimlogging"));
    }

    [Fact]
    public void SomeoneElsesChannel_GetsNoFallback()
    {
        // stamping every unresolved entry as ours would mislabel vanilla and Unity rows
        Assert.Null(ModContext.Fallback("Vanilla", "RimWorks.RimLogging", "rimworks.rimlogging"));
        Assert.Null(ModContext.Fallback("Mod.Unknown", "RimWorks.RimLogging", "rimworks.rimlogging"));
    }

    [Fact]
    public void FallbackMatchesCaseSensitively_SinceChannelsAreOrdinal()
    {
        Assert.Null(ModContext.Fallback("rimworks.rimlogging", "RimWorks.RimLogging", "x"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NoSelfChannelConfigured_GivesNoFallback(string? selfChannel)
    {
        Assert.Null(ModContext.Fallback("anything", selfChannel, "x"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void UnknownMod_LeavesTheContextExactlyAsItWas(string? modId)
    {
        Dictionary<string, object?> existing = new Dictionary<string, object?> { ["pawn"] = "Randy" };

        Assert.Same(existing, ModContext.WithModId(existing, modId));
        Assert.Null(ModContext.WithModId(null, modId));
    }
}
