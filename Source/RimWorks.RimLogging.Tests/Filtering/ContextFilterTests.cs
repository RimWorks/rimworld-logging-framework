using System;
using System.Collections.Generic;
using RimWorks.RimLogging.Filtering;
using Xunit;

namespace RimWorks.RimLogging.Tests.Filtering;

public class ContextFilterTests
{
    private static LogEntry Entry(IReadOnlyDictionary<string, object?>? context)
    {
        return new LogEntry
        {
            Timestamp = new DateTime(2026, 1, 1),
            Level = LogLevel.Info,
            Channel = "Test",
            MessageTemplate = "msg",
            RenderedMessage = "msg",
            Context = context,
        };
    }

    private static LogEntry WithContext(params (string Key, object? Value)[] pairs)
    {
        Dictionary<string, object?> ctx = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach ((string key, object? value) in pairs) ctx[key] = value;
        return Entry(ctx);
    }

    private static bool Matches(string expression, LogEntry entry)
        => FilterExpression.Parse(expression).Match(entry);

    [Fact]
    public void CtxAtom_MatchesTheValueUnderThatKey()
    {
        Assert.True(Matches("ctx.pawn = \"Randy\"", WithContext(("pawn", "Randy"))));
    }

    [Fact]
    public void CtxAtom_KeyLookupIsCaseInsensitive()
    {
        // template placeholders render as {Pawn}, and nobody types the capital
        Assert.True(Matches("ctx.pawn = \"Randy\"", WithContext(("Pawn", "Randy"))));
    }

    [Fact]
    public void CtxAtom_SupportsWildcards()
    {
        Assert.True(Matches("ctx.pawn = \"Ran*\"", WithContext(("pawn", "Randy"))));
        Assert.False(Matches("ctx.pawn = \"Zan*\"", WithContext(("pawn", "Randy"))));
    }

    [Fact]
    public void CtxAtom_NumericValue_MatchesItsInvariantForm()
    {
        Assert.True(Matches("ctx.days_left = \"2\"", WithContext(("days_left", 2))));
    }

    [Fact]
    public void CtxAtom_MissingKey_NeverMatches()
    {
        Assert.False(Matches("ctx.pawn = \"Randy\"", WithContext(("other", "x"))));
    }

    [Fact]
    public void CtxAtom_NoContextAtAll_NeverMatches()
    {
        Assert.False(Matches("ctx.pawn = \"Randy\"", Entry(null)));
    }

    [Fact]
    public void CtxAtom_NegatedOnAMissingKey_Matches()
    {
        // absent is not equal, so != is true. surprising, and documented in filter-dsl.md
        Assert.True(Matches("ctx.pawn != \"Randy\"", Entry(null)));
    }

    [Fact]
    public void CtxAtom_NullValue_MatchesTheEmptyPattern()
    {
        Assert.True(Matches("ctx.pawn = \"\"", WithContext(("pawn", null))));
    }

    [Fact]
    public void CtxAtom_CombinesWithTheOtherAtoms()
    {
        LogEntry entry = WithContext(("pawn", "Randy"));

        Assert.True(Matches("level >= Info AND ctx.pawn = \"Randy\"", entry));
        Assert.False(Matches("level >= Error AND ctx.pawn = \"Randy\"", entry));
    }

    [Theory]
    [InlineData("ctx. = \"x\"")]
    [InlineData("ctx.a.b = \"x\"")]
    public void CtxAtom_MalformedKey_ThrowsWithThePosition(string expression)
    {
        FormatException ex = Assert.Throws<FormatException>(() => FilterExpression.Parse(expression));
        Assert.Contains("at ", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ExistingAtoms_StillParseAndEvaluate()
    {
        LogEntry entry = WithContext(("pawn", "Randy"));

        Assert.True(Matches("channel = \"Test\"", entry));
        Assert.True(Matches("text = \"msg\"", entry));
        Assert.True(Matches("level = Info", entry));
        Assert.True(Matches("NOT channel = \"Other\"", entry));
    }
}
