using RimWorks.RimLogging.Format;
using Xunit;

namespace RimWorks.RimLogging.Tests.Format;

public class RichTextTests
{
    [Theory]
    [InlineData("hello <color=#ff0000>world</color>", "hello world")]
    [InlineData("<b>bold</b> and <i>italic</i>", "bold and italic")]
    [InlineData("a<size=12>b</size>c", "abc")]
    [InlineData("no tags here", "no tags here")]
    [InlineData("foo <unknown>bar</unknown>", "foo <unknown>bar</unknown>")]
    [InlineData("foo <color=", "foo <color=")]
    public void Strip_KnownAndUnknownTags(string input, string expected)
    {
        Assert.Equal(expected, RichText.Strip(input));
    }

    [Fact]
    public void Strip_Null_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, RichText.Strip(null));
    }

    [Fact]
    public void Strip_Empty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, RichText.Strip(string.Empty));
    }
}
