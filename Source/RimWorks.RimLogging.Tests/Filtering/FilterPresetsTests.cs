using System.Collections.Generic;
using RimWorks.RimLogging.Filtering;
using Xunit;

namespace RimWorks.RimLogging.Tests.Filtering;

public class FilterPresetsTests
{
    private static (List<string> Names, List<string> Expressions) Pair()
        => (new List<string> { "errors" }, new List<string> { "level >= Error" });

    [Fact]
    public void Save_AddsANewPreset()
    {
        (List<string> names, List<string> expressions) = Pair();

        FilterPresets.Save(names, expressions, "harmony", "mod = \"Harmony\"");

        Assert.Equal("mod = \"Harmony\"", FilterPresets.Expression(names, expressions, "harmony"));
        Assert.Equal(2, names.Count);
    }

    [Fact]
    public void Save_ReplacesAnExistingNameRatherThanDuplicating()
    {
        (List<string> names, List<string> expressions) = Pair();

        FilterPresets.Save(names, expressions, "errors", "level >= Fatal");

        Assert.Single(names);
        Assert.Equal("level >= Fatal", FilterPresets.Expression(names, expressions, "errors"));
    }

    [Fact]
    public void Save_TrimsTheName()
    {
        (List<string> names, List<string> expressions) = Pair();

        FilterPresets.Save(names, expressions, "  spaced  ", "level = Info");

        Assert.Contains("spaced", names);
    }

    [Theory]
    [InlineData(null, "level = Info")]
    [InlineData("name", null)]
    [InlineData("", "level = Info")]
    [InlineData("  ", "level = Info")]
    public void Save_IncompleteInput_IsIgnored(string? name, string? expression)
    {
        (List<string> names, List<string> expressions) = Pair();

        FilterPresets.Save(names, expressions, name, expression);

        Assert.Single(names);
    }

    [Fact]
    public void Remove_DropsBothHalvesTogether()
    {
        (List<string> names, List<string> expressions) = Pair();

        Assert.True(FilterPresets.Remove(names, expressions, "errors"));
        Assert.Empty(names);
        Assert.Empty(expressions);
    }

    [Fact]
    public void Remove_UnknownName_ChangesNothing()
    {
        (List<string> names, List<string> expressions) = Pair();

        Assert.False(FilterPresets.Remove(names, expressions, "nope"));
        Assert.Single(names);
    }

    [Fact]
    public void Lookup_IsCaseInsensitive()
    {
        (List<string> names, List<string> expressions) = Pair();

        Assert.Equal("level >= Error", FilterPresets.Expression(names, expressions, "ERRORS"));
    }

    [Fact]
    public void DriftedLists_NeverPairANameWithSomeoneElsesExpression()
    {
        // a half-written scribe could leave the two lists different lengths
        List<string> names = new List<string> { "a", "b", "c" };
        List<string> expressions = new List<string> { "level = Info" };

        Assert.Equal("level = Info", FilterPresets.Expression(names, expressions, "a"));
        Assert.Null(FilterPresets.Expression(names, expressions, "b"));
        Assert.Equal(["a"], FilterPresets.Usable(names, expressions));
    }

    [Fact]
    public void Save_OnDriftedLists_RealignsBeforeAppending()
    {
        List<string> names = new List<string> { "a", "b", "c" };
        List<string> expressions = new List<string> { "level = Info" };

        FilterPresets.Save(names, expressions, "new", "level = Warn");

        Assert.Equal(names.Count, expressions.Count);
        Assert.Equal("level = Warn", FilterPresets.Expression(names, expressions, "new"));
    }
}
