// RimWorks.RimLogging.Tests/Format/MessageTemplateTests.cs
using System.Collections.Generic;
using System.Globalization;
using RimWorks.RimLogging.Format;
using Xunit;

namespace RimWorks.RimLogging.Tests.Format;

public class MessageTemplateTests
{
    private static readonly string[] NameHpHoles = ["Name", "Hp"];
    private static readonly string[] DeathSegments = ["player ", " died at ", "hp"];
    private static readonly string[] EmptySegment = [""];

    [Fact]
    public void Construct_StoresRawTemplate()
    {
        MessageTemplate t = new MessageTemplate(
            raw: "player {Name} died at {Hp}hp",
            holes: NameHpHoles,
            segments: DeathSegments);

        Assert.Equal("player {Name} died at {Hp}hp", t.Raw);
        Assert.Equal(NameHpHoles, t.Holes);
        Assert.Equal(DeathSegments, t.Segments);
    }

    [Fact]
    public void Construct_AllowsEmptyTemplate()
    {
        MessageTemplate t = new MessageTemplate("", Array.Empty<string>(), EmptySegment);
        Assert.Equal("", t.Raw);
        Assert.Empty(t.Holes);
    }

    [Theory]
    [InlineData("hello", new string[0], new[] { "hello" })]
    [InlineData("hi {Name}", new[] { "Name" }, new[] { "hi ", "" })]
    [InlineData("a {X} b {Y} c", new[] { "X", "Y" }, new[] { "a ", " b ", " c" })]
    [InlineData("{Only}", new[] { "Only" }, new[] { "", "" })]
    [InlineData("", new string[0], new[] { "" })]
    public void Parse_ExtractsHolesAndSegments(string raw, string[] holes, string[] segments)
    {
        MessageTemplate t = MessageTemplate.Parse(raw);
        Assert.Equal(raw, t.Raw);
        Assert.Equal(holes, t.Holes);
        Assert.Equal(segments, t.Segments);
    }

    [Fact]
    public void Parse_EscapedBracesAreLiteral()
    {
        MessageTemplate t = MessageTemplate.Parse("literal {{not-a-hole}} here");
        Assert.Empty(t.Holes);
        Assert.Single(t.Segments);
        Assert.Equal("literal {not-a-hole} here", t.Segments[0]);
    }

    [Fact]
    public void Parse_UnclosedHoleIsLiteral()
    {
        MessageTemplate t = MessageTemplate.Parse("oops {Name");
        Assert.Empty(t.Holes);
        Assert.Single(t.Segments);
        Assert.Equal("oops {Name", t.Segments[0]);
    }

    [Fact]
    public void Render_TemplateWithOneHole_FillsArgAndCaptures()
    {
        (string rendered, IReadOnlyDictionary<string, object?>? context) = MessageTemplate.Parse("hi {Name}").Render(["Bob"]);
        Assert.Equal("hi Bob", rendered);
        Assert.NotNull(context);
        Assert.Equal("Bob", context["Name"]);
        Assert.Single(context);
    }

    [Fact]
    public void Render_MoreArgsThanHoles_DropsExtras()
    {
        (string rendered, IReadOnlyDictionary<string, object?>? context) = MessageTemplate.Parse("hi {Name}").Render(["Bob", "extra"]);
        Assert.Equal("hi Bob", rendered);
        Assert.NotNull(context);
        Assert.Single(context!);
    }

    [Fact]
    public void Render_FewerArgsThanHoles_LeavesHoleNameVisible()
    {
        (string rendered, IReadOnlyDictionary<string, object?>? context) = MessageTemplate.Parse("{A} {B}").Render([1]);
        Assert.Equal("1 {B}", rendered);
        Assert.NotNull(context);
        Assert.True(context!.ContainsKey("A"));
        Assert.False(context.ContainsKey("B"));
    }

    [Fact]
    public void Render_NullArg_RendersAsEmptyString()
    {
        (string rendered, IReadOnlyDictionary<string, object?>? context) = MessageTemplate.Parse("x={X}y").Render([null]);
        Assert.Equal("x=y", rendered);
        Assert.NotNull(context);
        Assert.Null(context!["X"]);
    }

    [Fact]
    public void Render_IFormattable_UsesInvariantCulture()
    {
        CultureInfo saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            (string rendered, IReadOnlyDictionary<string, object?>? _) = MessageTemplate.Parse("{V}").Render([(object?)1234.5]);
            Assert.Contains("1234.5", rendered);
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

    [Fact]
    public void Render_TemplateWithNoHoles_ReturnsRawAndNullContext()
    {
        (string rendered, IReadOnlyDictionary<string, object?>? context) = MessageTemplate.Parse("hello").Render(System.Array.Empty<object?>());
        Assert.Equal("hello", rendered);
        Assert.Null(context);
    }
}
