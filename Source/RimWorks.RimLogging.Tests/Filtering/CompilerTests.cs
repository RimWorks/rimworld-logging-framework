using System;
using RimWorks.RimLogging.Capture;
using RimWorks.RimLogging.Filtering;
using Xunit;

namespace RimWorks.RimLogging.Tests.Filtering;

public class CompilerTests
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

    // 1. LevelCompareNode Gte: returns true for Error/Warn/Fatal, false for Info/Debug/Trace
    [Theory]
    [InlineData(LogLevel.Error, true)]
    [InlineData(LogLevel.Warn, true)]
    [InlineData(LogLevel.Fatal, true)]
    [InlineData(LogLevel.Info, false)]
    [InlineData(LogLevel.Debug, false)]
    [InlineData(LogLevel.Trace, false)]
    public void LevelGte_Warn_CorrectResult(LogLevel lvl, bool expected)
    {
        Func<LogEntry, bool> pred = Compiler.Compile(
            new LevelCompareNode(TokenKind.OpGte, LogLevel.Warn));

        Assert.Equal(expected, pred(MakeEntry(lvl, "default")));
    }

    // 2. Each comparison op yields correct predicate
    [Theory]
    [InlineData(TokenKind.OpEq,  LogLevel.Warn, LogLevel.Warn,  true)]
    [InlineData(TokenKind.OpEq,  LogLevel.Warn, LogLevel.Error, false)]
    [InlineData(TokenKind.OpNeq, LogLevel.Warn, LogLevel.Warn,  false)]
    [InlineData(TokenKind.OpNeq, LogLevel.Warn, LogLevel.Error, true)]
    [InlineData(TokenKind.OpLt,  LogLevel.Warn, LogLevel.Info,  true)]   // Info < Warn = 2 < 3 = true
    [InlineData(TokenKind.OpLt,  LogLevel.Info, LogLevel.Warn,  false)]  // Warn < Info = 3 < 2 = false
    [InlineData(TokenKind.OpLte, LogLevel.Warn, LogLevel.Warn,  true)]   // Warn <= Warn = true
    [InlineData(TokenKind.OpLte, LogLevel.Warn, LogLevel.Info,  true)]   // Info <= Warn = 2 <= 3 = true
    [InlineData(TokenKind.OpLte, LogLevel.Info, LogLevel.Warn,  false)]  // Warn <= Info = 3 <= 2 = false
    [InlineData(TokenKind.OpGt,  LogLevel.Info, LogLevel.Warn,  true)]   // Warn > Info = 3 > 2 = true
    [InlineData(TokenKind.OpGt,  LogLevel.Warn, LogLevel.Info,  false)]  // Info > Warn = 2 > 3 = false
    [InlineData(TokenKind.OpGte, LogLevel.Warn, LogLevel.Warn,  true)]   // Warn >= Warn = true
    [InlineData(TokenKind.OpGte, LogLevel.Info, LogLevel.Warn,  true)]   // Warn >= Info = 3 >= 2 = true
    [InlineData(TokenKind.OpGte, LogLevel.Warn, LogLevel.Info,  false)]  // Info >= Warn = 2 >= 3 = false
    public void AllComparisonOps_CorrectResult(TokenKind op, LogLevel rightValue, LogLevel entryLevel, bool expected)
    {
        Func<LogEntry, bool> pred = Compiler.Compile(
            new LevelCompareNode(op, rightValue));

        Assert.Equal(expected, pred(MakeEntry(entryLevel, "default")));
    }

    // 3. ChannelMatchNode: matches correct channel, respects Negated
    [Theory]
    [InlineData("Cosmere.Roshar", false, true)]
    [InlineData("Unity", false, false)]
    [InlineData("Cosmere.Roshar", true, false)]
    [InlineData("Unity", true, true)]
    public void ChannelMatch_PatternAndNegated(string channel, bool negated, bool expected)
    {
        Func<LogEntry, bool> pred = Compiler.Compile(
            new ChannelMatchNode("Cosmere.*", negated));

        Assert.Equal(expected, pred(MakeEntry(LogLevel.Info, channel)));
    }

    // 4. AndNode: both conditions must hold
    [Theory]
    [InlineData(LogLevel.Warn,  "Cosmere.X", true)]
    [InlineData(LogLevel.Warn,  "Unity",     false)]
    [InlineData(LogLevel.Info,  "Cosmere.X", false)]
    [InlineData(LogLevel.Debug, "Unity",     false)]
    public void AndNode_BothMustHold(LogLevel lvl, string channel, bool expected)
    {
        Func<LogEntry, bool> pred = Compiler.Compile(new AndNode(
            new LevelCompareNode(TokenKind.OpGte, LogLevel.Warn),
            new ChannelMatchNode("Cosmere.*", false)));

        Assert.Equal(expected, pred(MakeEntry(lvl, channel)));
    }

    // 5. OrNode: either condition suffices
    [Theory]
    [InlineData(LogLevel.Error, "anything",   true)]
    [InlineData(LogLevel.Fatal, "other",      true)]
    [InlineData(LogLevel.Info,  "Unity",      true)]
    [InlineData(LogLevel.Debug, "Unity",      true)]
    [InlineData(LogLevel.Info,  "Cosmere.X",  false)]
    [InlineData(LogLevel.Debug, "default",    false)]
    public void OrNode_EitherSuffices(LogLevel lvl, string channel, bool expected)
    {
        Func<LogEntry, bool> pred = Compiler.Compile(new OrNode(
            new LevelCompareNode(TokenKind.OpGte, LogLevel.Error),
            new ChannelMatchNode("Unity", false)));

        Assert.Equal(expected, pred(MakeEntry(lvl, channel)));
    }

    // 6. NotNode: inverts child predicate
    [Theory]
    [InlineData(LogLevel.Info,  true)]
    [InlineData(LogLevel.Debug, true)]
    [InlineData(LogLevel.Trace, true)]
    [InlineData(LogLevel.Warn,  false)]
    [InlineData(LogLevel.Error, false)]
    [InlineData(LogLevel.Fatal, false)]
    public void NotNode_InvertsChild(LogLevel lvl, bool expected)
    {
        Func<LogEntry, bool> pred = Compiler.Compile(new NotNode(
            new LevelCompareNode(TokenKind.OpGte, LogLevel.Warn)));

        Assert.Equal(expected, pred(MakeEntry(lvl, "default")));
    }
}
