using System;
using RimWorks.RimLogging.Viewer;
using Xunit;

namespace RimWorks.RimLogging.Tests.Viewer;

public class FilterSuggestTests
{
    private static readonly string[] Channels = { "Cosmere.Roshar", "Cosmere.Scadrial", "Vanilla", "Unity" };

    private static string[] Items(string source)
    {
        Suggestions s = FilterSuggest.For(source, Channels);
        string[] copy = new string[s.Items.Count];
        for (int i = 0; i < copy.Length; i++) copy[i] = s.Items[i];
        return copy;
    }

    [Fact]
    public void For_EmptyInput_OffersTheThingsThatCanOpenATerm()
    {
        Assert.Equal(["level", "channel", "text", "mod", "ctx.", "NOT", "("], Items(""));
    }

    [Fact]
    public void For_AfterLevelKeyword_OffersOnlyComparisonOperators()
    {
        Assert.Equal(["=", "!=", "<", "<=", ">", ">="], Items("level "));
    }

    [Fact]
    public void For_AfterChannelKeyword_OffersOnlyEqualityOperators()
    {
        Assert.Equal(["=", "!="], Items("channel "));
    }

    [Fact]
    public void For_AfterALevelOperator_OffersLevelNames()
    {
        Assert.Equal(["Trace", "Debug", "Info", "Warn", "Error", "Fatal"], Items("level >= "));
    }

    [Fact]
    public void For_PartialLevelName_NarrowsToMatchesCaseInsensitively()
    {
        Assert.Equal(["Warn"], Items("level >= w"));
    }

    [Fact]
    public void For_PartialOperator_NarrowsToOperatorsStartingWithIt()
    {
        Assert.Equal([">", ">="], Items("level >"));
    }

    [Fact]
    public void For_AfterChannelEquals_OffersLiveChannelNamesQuoted()
    {
        Assert.Equal(["\"Cosmere.Roshar\"", "\"Cosmere.Scadrial\"", "\"Unity\"", "\"Vanilla\""], Items("channel = "));
    }

    [Fact]
    public void For_InsideAnUnterminatedQuote_NarrowsChannelsWithoutRetypingTheQuote()
    {
        Assert.Equal(["\"Cosmere.Roshar\"", "\"Cosmere.Scadrial\""], Items("channel = \"Cos"));
    }

    [Fact]
    public void For_AfterACompleteTerm_OffersConnectors()
    {
        Assert.Equal(["AND", "OR"], Items("level >= Warn "));
    }

    [Fact]
    public void For_AfterAConnector_GoesBackToTermStarts()
    {
        Assert.Equal(["level", "channel", "text", "mod", "ctx.", "NOT", "("], Items("level >= Warn OR "));
    }

    [Fact]
    public void For_AfterNot_GoesBackToTermStarts()
    {
        Assert.Equal(["level", "channel", "text", "mod", "ctx.", "NOT", "("], Items("NOT "));
    }

    [Fact]
    public void For_AfterAClosedString_OffersConnectors()
    {
        Assert.Equal(["AND", "OR"], Items("channel = \"Vanilla\" "));
    }

    [Fact]
    public void For_UnlexableInput_OffersNothingRatherThanThrowing()
    {
        Assert.Empty(Items("level >= Warn @@@ "));
    }

    [Fact]
    public void For_PartialWithNoMatch_OffersNothing()
    {
        Assert.Empty(Items("level >= zzz"));
    }

    [Fact]
    public void Apply_ReplacesThePartialWordAndAddsASeparator()
    {
        Suggestions s = FilterSuggest.For("level >= w", Channels);

        Assert.Equal("level >= Warn ", s.Apply("level >= w", "Warn"));
    }

    [Fact]
    public void Apply_ReplacesAnUnterminatedQuoteWithTheFullQuotedChannel()
    {
        const string source = "channel = \"Cos";
        Suggestions s = FilterSuggest.For(source, Channels);

        Assert.Equal("channel = \"Cosmere.Roshar\" ", s.Apply(source, "\"Cosmere.Roshar\""));
    }

    [Fact]
    public void Apply_OpenParen_DoesNotAddASeparatorAfterIt()
    {
        Suggestions s = FilterSuggest.For("", Channels);

        Assert.Equal("(", s.Apply("", "("));
    }

    [Fact]
    public void Apply_RoundTripsIntoSomethingTheParserAccepts()
    {
        const string source = "level >= w";
        Suggestions s = FilterSuggest.For(source, Channels);
        string completed = s.Apply(source, "Warn").Trim();

        Assert.True(RimLogging.Filtering.FilterExpression.TryParse(completed, out _, out string? error), error);
    }

    [Fact]
    public void ForChannelFilter_MatchesAnywhereInTheIdNotJustThePrefix()
    {
        Suggestions s = FilterSuggest.ForChannelFilter("roshar", Channels);

        Assert.Equal(["Cosmere.Roshar"], [s.Items[0]]);
        Assert.Single(s.Items);
    }

    [Fact]
    public void ForChannelFilter_ReplacesTheWholeFieldWhenAccepted()
    {
        Suggestions s = FilterSuggest.ForChannelFilter("van", Channels);

        Assert.Equal("Vanilla ", s.Apply("van", "Vanilla"));
    }

    [Fact]
    public void For_AfterACtxKeyword_OffersTheStringOperators()
    {
        Assert.Equal(["=", "!="], Items("ctx.pawn "));
    }

    [Theory]
    [InlineData("text ")]
    [InlineData("mod ")]
    public void For_AfterAStringKeyword_OffersTheStringOperators(string source)
    {
        // these used to fall through to the default arm and offer nothing at all
        Assert.Equal(["=", "!="], Items(source));
    }

    [Theory]
    [InlineData("text = ")]
    [InlineData("mod = ")]
    [InlineData("ctx.pawn = ")]
    public void For_AfterAFreeFormOperator_OffersNothingRatherThanLevels(string source)
    {
        // the operand pool used to default to level literals for every non-channel keyword
        Assert.Empty(Items(source));
    }

    [Fact]
    public void Apply_CtxPrefix_DoesNotAddASeparatorAfterIt()
    {
        Suggestions s = FilterSuggest.For("", Channels);

        Assert.Equal("ctx.", s.Apply("", "ctx."));
    }
}
