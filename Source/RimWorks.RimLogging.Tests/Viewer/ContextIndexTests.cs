using System;
using System.Collections.Generic;
using RimWorks.RimLogging.Viewer;
using Xunit;

namespace RimWorks.RimLogging.Tests.Viewer;

/// <summary>
/// Guards the context key index behind ctx. completion in the filter box. Values are stringified
/// the way the filter compiler does, or a suggestion would not match what it filters.
/// </summary>
public class ContextIndexTests
{
    private static LogEntry Entry(params (string Key, object? Value)[] context)
    {
        Dictionary<string, object?> ctx = new Dictionary<string, object?>();
        foreach ((string key, object? value) in context) ctx[key] = value;

        return new LogEntry
        {
            Timestamp = new DateTime(2026, 1, 1),
            Level = LogLevel.Info,
            Channel = "Test",
            MessageTemplate = "m",
            RenderedMessage = "m",
            Context = ctx,
        };
    }

    [Fact]
    public void KeysComeBackSortedAndDeduped()
    {
        ContextIndex index = new ContextIndex();
        index.Add(Entry(("pawn", "Randy")).Context);
        index.Add(Entry(("mod_id", "a.b"), ("pawn", "Tynan")).Context);

        Assert.Equal(new[] { "mod_id", "pawn" }, index.Keys());
    }

    [Fact]
    public void ValuesAreDedupedPerKey()
    {
        ContextIndex index = new ContextIndex();
        index.Add(Entry(("pawn", "Randy")).Context);
        index.Add(Entry(("pawn", "Randy")).Context);
        index.Add(Entry(("pawn", "Tynan")).Context);

        Assert.Equal(new[] { "Randy", "Tynan" }, index.ValuesFor("pawn"));
    }

    [Fact]
    public void ValuesUseInvariantCulture_SoASuggestionMatchesWhatItFilters()
    {
        ContextIndex index = new ContextIndex();
        index.Add(Entry(("days_left", 2)).Context);

        Assert.Equal(new[] { "2" }, index.ValuesFor("days_left"));
    }

    [Fact]
    public void ANullValue_BecomesEmptyRatherThanCrashing()
    {
        ContextIndex index = new ContextIndex();
        index.Add(Entry(("pawn", null)).Context);

        Assert.Equal(new[] { string.Empty }, index.ValuesFor("pawn"));
    }

    [Fact]
    public void AnUnseenKey_HasNoValues()
    {
        Assert.Empty(new ContextIndex().ValuesFor("nope"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ANullOrEmptyKeyLookup_IsEmptyRatherThanThrowing(string? key)
    {
        Assert.Empty(new ContextIndex().ValuesFor(key));
    }

    [Fact]
    public void NoContextAtAll_IsIgnored()
    {
        ContextIndex index = new ContextIndex();
        index.Add(null);

        Assert.Empty(index.Keys());
    }

    [Fact]
    public void KeyCountIsCapped_SoAWildKeySpaceCannotGrowForever()
    {
        ContextIndex index = new ContextIndex();
        for (int i = 0; i < ContextIndex.MaxKeys + 20; i++)
        {
            index.Add(Entry(("key" + i, "v")).Context);
        }

        Assert.Equal(ContextIndex.MaxKeys, index.Keys().Count);
    }

    [Fact]
    public void ValueCountIsCappedPerKey_SoATickValuedKeyCannotGrowForever()
    {
        ContextIndex index = new ContextIndex();
        for (int i = 0; i < ContextIndex.MaxValuesPerKey + 20; i++)
        {
            index.Add(Entry(("tick", i)).Context);
        }

        Assert.Equal(ContextIndex.MaxValuesPerKey, index.ValuesFor("tick").Count);
    }

    [Fact]
    public void Clear_DropsEverything()
    {
        ContextIndex index = new ContextIndex();
        index.Add(Entry(("pawn", "Randy")).Context);
        index.Clear();

        Assert.Empty(index.Keys());
    }

    [Fact]
    public void FromSnapshot_WalksAWholeLoadedFile()
    {
        ContextIndex index = ContextIndex.FromSnapshot(new[]
        {
            Entry(("mod_id", "a.b")),
            Entry(("mod_id", "c.d")),
        });

        Assert.Equal(new[] { "a.b", "c.d" }, index.ValuesFor("mod_id"));
    }

    [Fact]
    public void KeyLookupIgnoresCase_MatchingTheFilterLanguage()
    {
        ContextIndex index = new ContextIndex();
        index.Add(Entry(("Pawn", "Randy")).Context);

        Assert.Equal(new[] { "Randy" }, index.ValuesFor("pawn"));
    }
}
