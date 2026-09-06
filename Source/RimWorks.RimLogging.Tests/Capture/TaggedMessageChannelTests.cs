using RimWorks.RimLogging.Capture;
using Xunit;

namespace RimWorks.RimLogging.Tests.Capture;

/// <summary>
/// Guards the Concord shim. Concord logs through Verse.Log, so the caller walk lands on a Verse
/// frame and its entries showed up as Vanilla. The tag it writes is the only reliable signal.
/// </summary>
public class TaggedMessageChannelTests
{
    private const string Root = TaggedMessageChannel.ConcordRoot;

    [Fact]
    public void ASubTagBecomesANestedChannel()
    {
        Assert.Equal("Concord.Coex",
            TaggedMessageChannel.Read("[Concord.Coex] hook-installed 2.4.1.0", Root));
    }

    [Fact]
    public void TheBareRootTagIsAccepted()
    {
        Assert.Equal("Concord", TaggedMessageChannel.Read("[Concord] runtime wired.", Root));
    }

    [Fact]
    public void DeeperTagsKeepEveryLevel()
    {
        Assert.Equal("Concord.RimWorld.Boot",
            TaggedMessageChannel.Read("[Concord.RimWorld.Boot] up", Root));
    }

    [Fact]
    public void AnotherLibrarysTagIsLeftAlone()
    {
        Assert.Null(TaggedMessageChannel.Read("[LinuxMisc] Available CPU cores: 32", Root));
    }

    [Fact]
    public void ATagMerelyStartingWithTheRootIsNotAMatch()
    {
        // "Concordance" is not Concord, and prefix matching alone would claim it
        Assert.Null(TaggedMessageChannel.Read("[Concordance] hello", Root));
    }

    [Fact]
    public void ATagWithASpaceIsProseNotAChannel()
    {
        Assert.Null(TaggedMessageChannel.Read("[Concord is loading] hello", Root));
    }

    [Fact]
    public void ATagMustOpenTheMessage()
    {
        Assert.Null(TaggedMessageChannel.Read("loading [Concord.Coex] now", Root));
    }

    [Fact]
    public void AnUnclosedBracketIsNotATag()
    {
        Assert.Null(TaggedMessageChannel.Read("[Concord.Coex hook-installed", Root));
    }

    [Fact]
    public void AnEmptyTagIsNotATag()
    {
        Assert.Null(TaggedMessageChannel.Read("[] hello", Root));
        Assert.Null(TaggedMessageChannel.Read("[.] hello", Root));
    }

    [Fact]
    public void MatchingIsCaseSensitive_SinceChannelsAre()
    {
        Assert.Null(TaggedMessageChannel.Read("[concord.coex] hello", Root));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NoMessageMeansNoChannel(string? message)
    {
        Assert.Null(TaggedMessageChannel.Read(message, Root));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NoRootConfiguredMeansNoChannel(string? root)
    {
        Assert.Null(TaggedMessageChannel.Read("[Concord.Coex] hello", root));
    }

    [Fact]
    public void ATrailingDotIsRejected()
    {
        Assert.Null(TaggedMessageChannel.Read("[Concord.] hello", Root));
    }
}
