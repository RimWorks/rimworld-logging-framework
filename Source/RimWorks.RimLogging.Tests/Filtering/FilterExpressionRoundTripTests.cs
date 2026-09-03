using System;
using RimWorks.RimLogging.Capture;
using RimWorks.RimLogging.Filtering;
using Xunit;

namespace RimWorks.RimLogging.Tests.Filtering;

public class FilterExpressionRoundTripTests
{
    private static LogEntry MakeEntry(LogLevel lvl, string ch) => new LogEntry
    {
        Timestamp = DateTime.UtcNow,
        Level = lvl,
        Channel = ch,
        MessageTemplate = "",
        RenderedMessage = "",
        Context = null,
        Source = SourceLocation.Empty,
        StackTrace = null,
        Exception = null,
    };

    // 1. Parse("level >= Warn").Match: info=false, error=true
    [Fact]
    public void LevelGteWarn_MatchInfoFalse_MatchErrorTrue()
    {
        FilterExpression fe = FilterExpression.Parse("level >= Warn");

        Assert.False(fe.Match(MakeEntry(LogLevel.Info, "default")));
        Assert.True(fe.Match(MakeEntry(LogLevel.Error, "default")));
    }

    // 2. channel = "Cosmere.*" matches sub-channel, not Unity
    [Fact]
    public void ChannelPattern_MatchSubChannel_NotUnity()
    {
        FilterExpression fe = FilterExpression.Parse("channel = \"Cosmere.*\"");

        Assert.True(fe.Match(MakeEntry(LogLevel.Info, "Cosmere.Roshar")));
        Assert.False(fe.Match(MakeEntry(LogLevel.Info, "Unity")));
    }

    // 3. Source property returns the original input string verbatim
    [Fact]
    public void Source_ReturnsOriginalInput()
    {
        string input = "level >= Warn";
        FilterExpression fe = FilterExpression.Parse(input);

        Assert.Equal(input, fe.Source);
    }

    // 4. ToString returns canonical form
    [Fact]
    public void ToString_LevelCompare_NoExtraParens()
    {
        FilterExpression fe = FilterExpression.Parse("level >= Warn");

        Assert.Equal("level >= Warn", fe.ToString());
    }

    [Fact]
    public void ToString_OrExpression_HasOuterParens()
    {
        FilterExpression fe = FilterExpression.Parse("level >= Warn OR channel = \"X\"");

        Assert.Equal("(level >= Warn OR channel = \"X\")", fe.ToString());
    }

    // 5. Round-trip: Parse(ToString()) evaluates identically
    [Fact]
    public void RoundTrip_ParseStringifyParse_IdenticalEvaluation()
    {
        FilterExpression original = FilterExpression.Parse("level >= Warn OR channel = \"X\"");
        FilterExpression reparsed = FilterExpression.Parse(original.ToString());

        LogEntry[] entries =
        [
            MakeEntry(LogLevel.Trace, "X"),
            MakeEntry(LogLevel.Info,  "X"),
            MakeEntry(LogLevel.Warn,  "default"),
            MakeEntry(LogLevel.Error, "other"),
            MakeEntry(LogLevel.Info,  "other"),
        ];

        foreach (LogEntry entry in entries)
            Assert.Equal(original.Match(entry), reparsed.Match(entry));
    }

    // evaluation matrix over levels Trace..Error and a few representative channels

    // Expression: "level >= Warn"
    [Theory]
    [InlineData(LogLevel.Trace, "default", false)]
    [InlineData(LogLevel.Trace, "Cosmere.Roshar", false)]
    [InlineData(LogLevel.Trace, "Cosmere.Roshar.Surgebinding", false)]
    [InlineData(LogLevel.Trace, "Unity", false)]
    [InlineData(LogLevel.Trace, "Mod.foo", false)]
    [InlineData(LogLevel.Debug, "default", false)]
    [InlineData(LogLevel.Debug, "Unity", false)]
    [InlineData(LogLevel.Info, "default", false)]
    [InlineData(LogLevel.Info, "Cosmere.Roshar", false)]
    [InlineData(LogLevel.Warn, "default", true)]
    [InlineData(LogLevel.Warn, "Cosmere.Roshar", true)]
    [InlineData(LogLevel.Warn, "Unity", true)]
    [InlineData(LogLevel.Error, "default", true)]
    [InlineData(LogLevel.Error, "Cosmere.Roshar.Surgebinding", true)]
    [InlineData(LogLevel.Error, "Unity", true)]
    public void Spec_LevelGteWarn(LogLevel lvl, string ch, bool expected)
    {
        FilterExpression fe = FilterExpression.Parse("level >= Warn");
        Assert.Equal(expected, fe.Match(MakeEntry(lvl, ch)));
    }

    // Expression: "level >= Warn OR channel = "Cosmere.*""
    [Theory]
    [InlineData(LogLevel.Trace, "default", false)]
    [InlineData(LogLevel.Trace, "Cosmere.Roshar", true)]   // channel matches
    [InlineData(LogLevel.Trace, "Cosmere.Roshar.Surgebinding", true)]   // channel matches
    [InlineData(LogLevel.Trace, "Unity", false)]
    [InlineData(LogLevel.Trace, "Mod.foo", false)]
    [InlineData(LogLevel.Debug, "default", false)]
    [InlineData(LogLevel.Debug, "Cosmere.Roshar", true)]   // channel matches
    [InlineData(LogLevel.Info, "Unity", false)]
    [InlineData(LogLevel.Warn, "default", true)]   // level matches
    [InlineData(LogLevel.Warn, "Cosmere.Roshar", true)]   // both match
    [InlineData(LogLevel.Warn, "Unity", true)]   // level matches
    [InlineData(LogLevel.Error, "default", true)]   // level matches
    [InlineData(LogLevel.Error, "Unity", true)]   // level matches
    [InlineData(LogLevel.Error, "Cosmere.Roshar.Surgebinding", true)]   // both match
    public void Spec_LevelGteWarnOrCosmereChannel(LogLevel lvl, string ch, bool expected)
    {
        FilterExpression fe = FilterExpression.Parse("level >= Warn OR channel = \"Cosmere.*\"");
        Assert.Equal(expected, fe.Match(MakeEntry(lvl, ch)));
    }

    // Expression: "channel = "Cosmere.Roshar.*" AND level >= Debug"
    // Cosmere.Roshar.* matches "Cosmere.Roshar" (exact prefix) and "Cosmere.Roshar.Surgebinding"
    [Theory]
    [InlineData(LogLevel.Trace, "default", false)]
    [InlineData(LogLevel.Trace, "Cosmere.Roshar", false)]  // level fails (Trace < Debug)
    [InlineData(LogLevel.Trace, "Cosmere.Roshar.Surgebinding", false)]  // level fails
    [InlineData(LogLevel.Debug, "default", false)]  // channel fails
    [InlineData(LogLevel.Debug, "Cosmere.Roshar", true)]   // both pass
    [InlineData(LogLevel.Debug, "Cosmere.Roshar.Surgebinding", true)]   // both pass
    [InlineData(LogLevel.Debug, "Unity", false)]  // channel fails
    [InlineData(LogLevel.Info, "Cosmere.Roshar", true)]
    [InlineData(LogLevel.Info, "Cosmere.Roshar.Surgebinding", true)]
    [InlineData(LogLevel.Info, "Mod.foo", false)]
    [InlineData(LogLevel.Warn, "Cosmere.Roshar", true)]
    [InlineData(LogLevel.Warn, "Unity", false)]
    [InlineData(LogLevel.Error, "Cosmere.Roshar", true)]
    [InlineData(LogLevel.Error, "Cosmere.Roshar.Surgebinding", true)]
    [InlineData(LogLevel.Error, "Unity", false)]
    public void Spec_CosmereRosharChannelAndLevelGteDebug(LogLevel lvl, string ch, bool expected)
    {
        FilterExpression fe = FilterExpression.Parse("channel = \"Cosmere.Roshar.*\" AND level >= Debug");
        Assert.Equal(expected, fe.Match(MakeEntry(lvl, ch)));
    }

    // Expression: "NOT (channel = "Unity")"
    [Theory]
    [InlineData(LogLevel.Trace, "default", true)]
    [InlineData(LogLevel.Trace, "Cosmere.Roshar", true)]
    [InlineData(LogLevel.Trace, "Cosmere.Roshar.Surgebinding", true)]
    [InlineData(LogLevel.Trace, "Unity", false)]
    [InlineData(LogLevel.Trace, "Mod.foo", true)]
    [InlineData(LogLevel.Debug, "Unity", false)]
    [InlineData(LogLevel.Info, "Unity", false)]
    [InlineData(LogLevel.Warn, "Unity", false)]
    [InlineData(LogLevel.Error, "Unity", false)]
    [InlineData(LogLevel.Error, "default", true)]
    [InlineData(LogLevel.Error, "Cosmere.Roshar", true)]
    public void Spec_NotUnityChannel(LogLevel lvl, string ch, bool expected)
    {
        FilterExpression fe = FilterExpression.Parse("NOT (channel = \"Unity\")");
        Assert.Equal(expected, fe.Match(MakeEntry(lvl, ch)));
    }

    // Task 6.8: TryParse error-handling

    // 1. Valid input: returns true, non-null result, null error
    [Fact]
    public void TryParse_ValidInput_ReturnsTrueWithResult()
    {
        bool ok = FilterExpression.TryParse("level >= Warn", out FilterExpression? fe, out string? err);

        Assert.True(ok);
        Assert.NotNull(fe);
        Assert.Null(err);
    }

    // 2. Truncated input "level >= ": missing level literal
    [Fact]
    public void TryParse_TruncatedLevelExpression_ReturnsFalseWithPositionInfo()
    {
        bool ok = FilterExpression.TryParse("level >= ", out FilterExpression? fe, out string? err);

        Assert.False(ok);
        Assert.Null(fe);
        Assert.NotNull(err);
        Assert.Contains("9", err);  // End token Pos = input.Length = 9
    }

    // 3. Empty input: parser throws on the End token
    [Fact]
    public void TryParse_EmptyInput_ReturnsFalseWithPositionInfo()
    {
        bool ok = FilterExpression.TryParse("", out FilterExpression? fe, out string? err);

        Assert.False(ok);
        Assert.Null(fe);
        Assert.NotNull(err);
        Assert.Contains("0", err);  // End token Pos = 0 for empty input
    }
}
