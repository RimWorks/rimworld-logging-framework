using RimWorks.RimLogging.Capture;
using Xunit;

namespace RimWorks.RimLogging.Tests.Capture;

public class PackageIdSanitizerTests
{
    [Fact]
    public void DottedId_Unchanged()
    {
        Assert.Equal("cosmere.lightweave", PackageIdSanitizer.ToChannelSegment("cosmere.lightweave"));
    }

    [Fact]
    public void EmbeddedDashes_Stripped()
    {
        Assert.Equal("meandyou", PackageIdSanitizer.ToChannelSegment("me-and-you"));
    }

    [Fact]
    public void Spaces_Stripped()
    {
        Assert.Equal("AuthorThatHasSpaces", PackageIdSanitizer.ToChannelSegment("Author That Has Spaces"));
    }

    [Fact]
    public void DoubleDots_Collapsed()
    {
        Assert.Equal("a.b", PackageIdSanitizer.ToChannelSegment("a..b"));
    }

    [Fact]
    public void LeadingDot_Stripped()
    {
        Assert.Equal("foo", PackageIdSanitizer.ToChannelSegment(".foo."));
    }

    [Fact]
    public void MultipleLeadingDots_Stripped()
    {
        Assert.Equal("foo", PackageIdSanitizer.ToChannelSegment("..foo"));
    }

    [Fact]
    public void MultipleTrailingDots_Stripped()
    {
        Assert.Equal("foo", PackageIdSanitizer.ToChannelSegment("foo..."));
    }

    [Fact]
    public void EmptyInput_ReturnsUnknown()
    {
        Assert.Equal("Unknown", PackageIdSanitizer.ToChannelSegment(""));
    }

    [Fact]
    public void NullInput_ReturnsUnknown()
    {
        Assert.Equal("Unknown", PackageIdSanitizer.ToChannelSegment(null!));
    }

    [Fact]
    public void AllInvalidChars_ReturnsUnknown()
    {
        Assert.Equal("Unknown", PackageIdSanitizer.ToChannelSegment("!@#"));
    }

    [Fact]
    public void Underscores_Preserved()
    {
        Assert.Equal("foo_bar", PackageIdSanitizer.ToChannelSegment("foo_bar"));
    }

    [Fact]
    public void Digits_Preserved()
    {
        Assert.Equal("mod123", PackageIdSanitizer.ToChannelSegment("mod123"));
    }

    [Fact]
    public void RealWorldId_Unchanged()
    {
        Assert.Equal("com.cryptiklemur.rimobs", PackageIdSanitizer.ToChannelSegment("com.cryptiklemur.rimobs"));
    }
}
