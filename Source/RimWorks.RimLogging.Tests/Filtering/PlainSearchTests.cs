using System;
using System.Collections.Generic;
using RimWorks.RimLogging.Filtering;
using Xunit;

namespace RimWorks.RimLogging.Tests.Filtering;

public class PlainSearchTests
{
    private static LogEntry Entry(string message, string channel = "Test", LogLevel level = LogLevel.Info)
    {
        return new LogEntry
        {
            Timestamp = new DateTime(2026, 1, 1),
            Level = level,
            Channel = channel,
            MessageTemplate = message,
            RenderedMessage = message,
        };
    }

    private static FilterExpression Parse(string input)
    {
        Assert.True(FilterExpression.TryParseOrSearch(input, out FilterExpression? e, out string? error), error);
        return e!;
    }

    [Fact]
    public void BareWord_BecomesAMessageSearchInsteadOfAParseError()
    {
        // "exception" is not a DSL identifier, so this used to throw and filter nothing
        FilterExpression e = Parse("exception");

        Assert.True(e.Match(Entry("threw an exception during tick")));
        Assert.False(e.Match(Entry("all fine")));
    }

    [Fact]
    public void PhraseWithSpaces_SearchesForTheWholeThing()
    {
        FilterExpression e = Parse("save failed");

        Assert.True(e.Match(Entry("the save failed badly")));
        Assert.False(e.Match(Entry("save succeeded")));
    }

    [Fact]
    public void PlainSearch_IsCaseInsensitive()
    {
        Assert.True(Parse("EXCEPTION").Match(Entry("an exception")));
    }

    [Fact]
    public void PlainSearch_DoesNotMatchTheChannelOrMod()
    {
        // it searches the message, the same field the "text" atom uses
        FilterExpression e = Parse("Cosmere");

        Assert.False(e.Match(Entry("nothing to see", channel: "Cosmere.Roshar")));
    }

    [Fact]
    public void WordsThatAreAlsoKeywords_StillSearchAsText()
    {
        Assert.True(Parse("level").Match(Entry("power level rising")));
        Assert.True(Parse("not found").Match(Entry("file not found")));
    }

    [Theory]
    [InlineData("level >= Warn")]
    [InlineData("channel = \"Cosmere.*\"")]
    [InlineData("ctx.pawn = \"Randy\"")]
    [InlineData("NOT (channel = \"Unity\")")]
    public void RealExpressions_StillParseAsTheDsl(string input)
    {
        Assert.True(FilterExpression.LooksStructured(input));
        Assert.True(FilterExpression.TryParseOrSearch(input, out FilterExpression? e, out string? error), error);
        Assert.NotNull(e);
    }

    [Fact]
    public void TypoInsideARealExpression_StillReportsAnError()
    {
        // the whole point of the heuristic: a broken expression must not silently become a text
        // search that matches nothing, because then a typo is indistinguishable from no results
        Assert.False(FilterExpression.TryParseOrSearch("level >= Warnn", out FilterExpression? e, out string? error));

        Assert.Null(e);
        Assert.NotNull(error);
    }

    [Fact]
    public void UnterminatedQuote_StillReportsAnError()
    {
        Assert.False(FilterExpression.TryParseOrSearch("channel = \"Cosmere", out _, out string? error));
        Assert.NotNull(error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void BlankInput_IsNotAFilterAtAll(string? input)
    {
        Assert.False(FilterExpression.TryParseOrSearch(input!, out FilterExpression? e, out string? error));

        Assert.Null(e);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("exception", false)]
    [InlineData("save failed", false)]
    [InlineData("level", false)]
    [InlineData("level >= Warn", true)]
    [InlineData("pawn=Randy", true)]
    [InlineData("a != b", true)]
    [InlineData("(grouped)", true)]
    [InlineData("\"quoted\"", true)]
    public void LooksStructured_KeysOffOperatorsQuotesAndBrackets(string input, bool structured)
    {
        Assert.Equal(structured, FilterExpression.LooksStructured(input));
    }
}
