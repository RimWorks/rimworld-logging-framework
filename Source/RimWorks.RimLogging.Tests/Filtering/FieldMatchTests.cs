using System;
using RimWorks.RimLogging.Capture;
using RimWorks.RimLogging.Filtering;
using Xunit;

namespace RimWorks.RimLogging.Tests.Filtering;

public class FieldMatchTests
{
    private static LogEntry Entry(string message, string? mod = null, string channel = "default") => new LogEntry
    {
        Timestamp = DateTime.UtcNow,
        Level = LogLevel.Info,
        Channel = channel,
        MessageTemplate = "",
        RenderedMessage = message,
        Context = null,
        Source = SourceLocation.Empty,
        StackTrace = null,
        Exception = null,
        Mod = mod,
    };

    private static bool Eval(string expression, LogEntry e)
        => Compiler.Compile(Parser.Parse(expression))(e);

    [Fact]
    public void Text_MatchesASubstringWithoutWildcards()
    {
        Assert.True(Eval("text = \"exception\"", Entry("Unhandled exception ticking pawn")));
    }

    [Fact]
    public void Text_IsCaseInsensitive()
    {
        Assert.True(Eval("text = \"EXCEPTION\"", Entry("unhandled exception")));
    }

    [Fact]
    public void Text_NonMatchIsFalse()
    {
        Assert.False(Eval("text = \"exception\"", Entry("all fine here")));
    }

    [Fact]
    public void Text_CanBeNegated()
    {
        Assert.True(Eval("text != \"exception\"", Entry("all fine here")));
    }

    [Fact]
    public void Mod_MatchesAsAWildcard()
    {
        Assert.True(Eval("mod = \"Harm*\"", Entry("msg", mod: "Harmony")));
    }

    [Fact]
    public void Mod_NullModNeverMatches()
    {
        // vanilla entries carry no mod, and must not blow up or match a pattern
        Assert.False(Eval("mod = \"*\"", Entry("msg", mod: null)));
    }

    [Fact]
    public void Mod_NegatedNullModMatches()
    {
        Assert.True(Eval("mod != \"Harmony\"", Entry("msg", mod: null)));
    }

    [Fact]
    public void Channel_StillBehavesAsBefore()
    {
        Assert.True(Eval("channel = \"mod.*\"", Entry("msg", channel: "mod.cebarks")));
        Assert.False(Eval("channel = \"mod.*\"", Entry("msg", channel: "default")));
    }

    [Fact]
    public void Fields_CombineWithBooleans()
    {
        LogEntry e = Entry("Unhandled exception", mod: "Harmony");

        Assert.True(Eval("mod = \"Harmony\" AND text = \"exception\"", e));
        Assert.False(Eval("mod = \"Nope\" AND text = \"exception\"", e));
    }

    [Theory]
    [InlineData("text = \"a\"", "text = \"a\"")]
    [InlineData("mod != \"b\"", "mod != \"b\"")]
    [InlineData("channel = \"c\"", "channel = \"c\"")]
    public void RoundTrips_ThroughToString(string input, string expected)
    {
        Assert.Equal(expected, FilterExpression.Parse(input).ToString());
    }
}
