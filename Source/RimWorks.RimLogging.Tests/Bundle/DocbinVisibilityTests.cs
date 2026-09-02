using RimWorks.RimLogging.Bundle;
using Xunit;

namespace RimWorks.RimLogging.Tests.Bundle;

public class DocbinVisibilityTests
{
    [Theory]
    [InlineData("unlisted")]
    [InlineData("public")]
    [InlineData("")]
    [InlineData(null)]
    public void Effective_WithoutAKey_IsAlwaysPublic(string? chosen)
    {
        // anonymous pastes have no account to file them under, so the choice cannot be honoured
        Assert.Equal("public", DocbinVisibility.Effective(false, chosen));
    }

    [Fact]
    public void Effective_WithAKey_HonoursAnExplicitPublic()
    {
        Assert.Equal("public", DocbinVisibility.Effective(true, "public"));
    }

    [Theory]
    [InlineData("unlisted")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("nonsense")]
    public void Effective_WithAKey_DefaultsToUnlisted(string? chosen)
    {
        Assert.Equal("unlisted", DocbinVisibility.Effective(true, chosen));
    }

    [Fact]
    public void Effective_IsCaseInsensitive()
    {
        Assert.Equal("public", DocbinVisibility.Effective(true, "PUBLIC"));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("dk_live_abc", true)]
    public void CanChoose_TracksWhetherAKeyIsSet(string? apiKey, bool expected)
    {
        Assert.Equal(expected, DocbinVisibility.CanChoose(apiKey));
    }
}
