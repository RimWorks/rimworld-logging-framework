using RimWorks.RimLogging.Filtering;
using Xunit;

namespace RimWorks.RimLogging.Tests.Filtering;

public class WildcardMatcherTests
{
    // 1. "Cosmere.*" matches the prefix alone (no trailing dot)
    [Fact]
    public void PrefixPattern_MatchesPrefixAlone()
    {
        Assert.True(WildcardMatcher.Match("Cosmere.*", "Cosmere"));
    }

    // 2. "Cosmere.*" matches sub-channels
    [Theory]
    [InlineData("Cosmere.Roshar")]
    [InlineData("Cosmere.Roshar.Surgebinding")]
    public void PrefixPattern_MatchesSubChannels(string input)
    {
        Assert.True(WildcardMatcher.Match("Cosmere.*", input));
    }

    // 3. "Cosmere.*" does NOT match "CosmereExtra"
    [Fact]
    public void PrefixPattern_DoesNotMatchAdjacentPrefix()
    {
        Assert.False(WildcardMatcher.Match("Cosmere.*", "CosmereExtra"));
    }

    // 4. Literal pattern matches exactly, not as prefix or suffix
    [Theory]
    [InlineData("Foo", true)]
    [InlineData("FooBar", false)]
    [InlineData("xFoo", false)]
    public void LiteralPattern_ExactMatchOnly(string input, bool expected)
    {
        Assert.Equal(expected, WildcardMatcher.Match("Foo", input));
    }

    // 5. Generic glob "Foo.*.Bar": dots must be present; * is middle segment
    [Theory]
    [InlineData("Foo.x.Bar", true)]
    [InlineData("Foo..Bar", true)]   // zero-length middle is ok: .* matches empty
    [InlineData("Foo.Bar", false)]   // no dots between: the literal dots must exist
    public void GenericGlob_MiddleWildcard(string input, bool expected)
    {
        Assert.Equal(expected, WildcardMatcher.Match("Foo.*.Bar", input));
    }

    // 6. Single "*" matches anything including empty string
    [Theory]
    [InlineData("")]
    [InlineData("anything")]
    [InlineData("a.b.c.d")]
    public void SingleStar_MatchesAnything(string input)
    {
        Assert.True(WildcardMatcher.Match("*", input));
    }

    // 7. Case-sensitive: "Cosmere.*" does NOT match "cosmere.roshar"
    [Fact]
    public void PrefixPattern_CaseSensitive()
    {
        Assert.False(WildcardMatcher.Match("Cosmere.*", "cosmere.roshar"));
    }
}
