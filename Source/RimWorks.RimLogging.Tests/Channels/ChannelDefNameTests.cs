using RimWorks.RimLogging.Channels;
using Xunit;

namespace RimWorks.RimLogging.Tests.Channels;

public class ChannelDefNameTests
{
    [Theory]
    [InlineData("MPF")]
    [InlineData("MPF.Worldgen")]
    [InlineData("Cosmere.Roshar.Surgebinding")]
    [InlineData("RimWorks.RimLogging.Boot")]
    [InlineData("mod_name.sub-part.Leaf2")]
    public void Validate_DottedChannelNames_AreAccepted(string defName)
    {
        Assert.Null(ChannelDefName.Validate(defName));
    }

    [Theory]
    [InlineData(".Leading")]
    [InlineData("Trailing.")]
    [InlineData("Double..Dot")]
    public void Validate_MalformedDots_AreRejected(string defName)
    {
        Assert.Contains("empty segment", ChannelDefName.Validate(defName));
    }

    [Theory]
    [InlineData("Has Space")]
    [InlineData("Has/Slash")]
    [InlineData("Has:Colon")]
    public void Validate_CharactersVanillaWouldRejectAnyway_AreStillRejected(string defName)
    {
        Assert.Contains("should only contain", ChannelDefName.Validate(defName));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingDefName_IsRejected(string? defName)
    {
        Assert.Contains("lacks a defName", ChannelDefName.Validate(defName));
    }
}
