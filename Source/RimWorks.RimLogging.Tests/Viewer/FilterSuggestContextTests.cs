using System.Collections.Generic;
using RimWorks.RimLogging.Viewer;
using Xunit;

namespace RimWorks.RimLogging.Tests.Viewer;

/// <summary>
/// Guards ctx. completion in the filter box, and the bug where every opened quote offered
/// channel names, so ctx.mod_id = " listed channels instead of mod ids.
/// </summary>
public class FilterSuggestContextTests
{
    private static readonly string[] Channels = { "Cosmere.Roshar", "Vanilla" };

    private static ContextIndex Index()
    {
        ContextIndex index = new ContextIndex();
        index.Add(new Dictionary<string, object?>
        {
            ["mod_id"] = "cebarks.rimworld.linuxmisc",
            ["pawn"] = "Randy",
        });
        index.Add(new Dictionary<string, object?> { ["mod_id"] = "brrainz.harmony" });
        return index;
    }

    private static string[] Items(string source)
    {
        Suggestions s = FilterSuggest.For(source, Channels, Index());
        return new List<string>(s.Items).ToArray();
    }

    [Fact]
    public void TypingCtxDot_OffersTheKeysSeenSoFar()
    {
        Assert.Equal(new[] { "ctx.mod_id", "ctx.pawn" }, Items("ctx."));
    }

    [Fact]
    public void TypingPartOfAKey_NarrowsToIt()
    {
        Assert.Equal(new[] { "ctx.mod_id" }, Items("ctx.mo"));
    }

    [Fact]
    public void OpeningAQuoteOnACtxKey_OffersThatKeysValues_NotChannels()
    {
        // the reported bug: this used to list "Vanilla" and the Mod.* channel names
        Assert.Equal(
            new[] { "\"brrainz.harmony\"", "\"cebarks.rimworld.linuxmisc\"" },
            Items("ctx.mod_id = \""));
    }

    [Fact]
    public void OpeningAQuoteOnADifferentKey_OffersThatKeysValues()
    {
        Assert.Equal(new[] { "\"Randy\"" }, Items("ctx.pawn = \""));
    }

    [Fact]
    public void OpeningAQuoteOnChannel_StillOffersChannels()
    {
        Assert.Equal(new[] { "\"Cosmere.Roshar\"", "\"Vanilla\"" }, Items("channel = \""));
    }

    [Fact]
    public void OpeningAQuoteOnText_OffersNothing_BecauseItIsFreeForm()
    {
        Assert.Empty(Items("text = \""));
    }

    [Fact]
    public void OpeningAQuoteOnMod_OffersNothing_BecauseItIsFreeForm()
    {
        Assert.Empty(Items("mod = \""));
    }

    [Fact]
    public void AnUnseenCtxKey_OffersNoValues()
    {
        Assert.Empty(Items("ctx.nope = \""));
    }

    [Fact]
    public void PartialValueInsideTheQuote_NarrowsTheList()
    {
        Assert.Equal(new[] { "\"cebarks.rimworld.linuxmisc\"" }, Items("ctx.mod_id = \"ce"));
    }

    [Fact]
    public void WithNoIndex_CtxFallsBackToTheTermStart()
    {
        // the popout window has no index to hand, so ctx. stays offered as a bare term start
        Suggestions s = FilterSuggest.For("ctx.", Channels);

        Assert.Equal(new[] { "ctx." }, s.Items);
    }

    [Fact]
    public void ApplyingAKeySuggestion_LeavesTheCaretOnTheSameTerm()
    {
        Suggestions s = FilterSuggest.For("ctx.mo", Channels, Index());

        Assert.Equal("ctx.mod_id ", s.Apply("ctx.mo", "ctx.mod_id"));
    }
}
