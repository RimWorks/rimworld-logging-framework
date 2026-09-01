using System;
using RimWorks.RimLogging.Filtering;
using Xunit;

namespace RimWorks.RimLogging.Tests.Filtering;

public class ParserTests
{
    // 1. level >= Warn parses to LevelCompareNode { Op=OpGte, RightValue=Warn }
    [Fact]
    public void LevelGteWarn_ParsesTo_LevelCompareNode()
    {
        AstNode node = Parser.Parse("level >= Warn");

        LevelCompareNode cmp = Assert.IsType<LevelCompareNode>(node);
        Assert.Equal(TokenKind.OpGte, cmp.Op);
        Assert.Equal(LogLevel.Warn, cmp.RightValue);
    }

    // 2. level >= Warn OR channel = "Cosmere.*" parses to OrNode { Left=LevelCompare, Right=ChannelMatch }
    [Fact]
    public void LevelOrChannel_ParsesTo_OrNode()
    {
        AstNode node = Parser.Parse("level >= Warn OR channel = \"Cosmere.*\"");

        OrNode or = Assert.IsType<OrNode>(node);
        LevelCompareNode left = Assert.IsType<LevelCompareNode>(or.Left);
        Assert.Equal(TokenKind.OpGte, left.Op);
        Assert.Equal(LogLevel.Warn, left.RightValue);
        ChannelMatchNode right = Assert.IsType<ChannelMatchNode>(or.Right);
        Assert.False(right.Negated);
        Assert.Equal("Cosmere.*", right.Pattern);
    }

    // 3a. channel = "Cosmere.Roshar.*" AND level >= Debug: AND at top level
    [Fact]
    public void ChannelAndLevel_ParsesTo_AndNode()
    {
        AstNode node = Parser.Parse("channel = \"Cosmere.Roshar.*\" AND level >= Debug");

        AndNode and = Assert.IsType<AndNode>(node);
        ChannelMatchNode left = Assert.IsType<ChannelMatchNode>(and.Left);
        Assert.False(left.Negated);
        Assert.Equal("Cosmere.Roshar.*", left.Pattern);
        LevelCompareNode right = Assert.IsType<LevelCompareNode>(and.Right);
        Assert.Equal(TokenKind.OpGte, right.Op);
        Assert.Equal(LogLevel.Debug, right.RightValue);
    }

    // 3b. Precedence: A OR B AND C should parse as OR(A, AND(B, C)): AND binds tighter
    [Fact]
    public void Precedence_AndBindsTighterThanOr()
    {
        AstNode node = Parser.Parse("channel = \"A\" OR channel = \"B\" AND level >= Info");

        OrNode or = Assert.IsType<OrNode>(node);
        ChannelMatchNode left = Assert.IsType<ChannelMatchNode>(or.Left);
        Assert.Equal("A", left.Pattern);
        AndNode right = Assert.IsType<AndNode>(or.Right);
        ChannelMatchNode rightLeft = Assert.IsType<ChannelMatchNode>(right.Left);
        Assert.Equal("B", rightLeft.Pattern);
        LevelCompareNode rightRight = Assert.IsType<LevelCompareNode>(right.Right);
        Assert.Equal(TokenKind.OpGte, rightRight.Op);
        Assert.Equal(LogLevel.Info, rightRight.RightValue);
    }

    // 4. (level = Info OR level = Warn) AND channel = "Foo" honors parentheses
    [Fact]
    public void Parentheses_OverridePrecedence()
    {
        AstNode node = Parser.Parse("(level = Info OR level = Warn) AND channel = \"Foo\"");

        AndNode and = Assert.IsType<AndNode>(node);
        OrNode left = Assert.IsType<OrNode>(and.Left);
        LevelCompareNode leftLeft = Assert.IsType<LevelCompareNode>(left.Left);
        Assert.Equal(TokenKind.OpEq, leftLeft.Op);
        Assert.Equal(LogLevel.Info, leftLeft.RightValue);
        LevelCompareNode leftRight = Assert.IsType<LevelCompareNode>(left.Right);
        Assert.Equal(TokenKind.OpEq, leftRight.Op);
        Assert.Equal(LogLevel.Warn, leftRight.RightValue);
        ChannelMatchNode right = Assert.IsType<ChannelMatchNode>(and.Right);
        Assert.Equal("Foo", right.Pattern);
    }

    // 5. NOT (channel = "Unity") produces NotNode wrapping ChannelMatch
    [Fact]
    public void Not_WrapsChannelMatch()
    {
        AstNode node = Parser.Parse("NOT (channel = \"Unity\")");

        NotNode not = Assert.IsType<NotNode>(node);
        ChannelMatchNode inner = Assert.IsType<ChannelMatchNode>(not.Operand);
        Assert.False(inner.Negated);
        Assert.Equal("Unity", inner.Pattern);
    }

    // 6. NOT NOT channel = "Foo" produces NotNode wrapping NotNode wrapping ChannelMatch
    [Fact]
    public void NotNot_RightAssociative()
    {
        AstNode node = Parser.Parse("NOT NOT channel = \"Foo\"");

        NotNode outer = Assert.IsType<NotNode>(node);
        NotNode inner = Assert.IsType<NotNode>(outer.Operand);
        ChannelMatchNode ch = Assert.IsType<ChannelMatchNode>(inner.Operand);
        Assert.Equal("Foo", ch.Pattern);
    }

    // 7. Bad inputs throw FormatException with positional info

    [Theory]
    [InlineData("level >= ")]
    [InlineData("level >= \t")]
    public void MissingRightOperand_ThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => Parser.Parse(input));
    }

    [Fact]
    public void UnbalancedParen_ThrowsFormatExceptionMentioningExpectedRParen()
    {
        FormatException ex = Assert.Throws<FormatException>(() => Parser.Parse("(level = Info"));

        Assert.Contains("')'", ex.Message);
    }

    [Fact]
    public void MissingOperator_ThrowsFormatException()
    {
        // "level Info": lexer accepts both as valid tokens but parser sees no operator between them
        Assert.Throws<FormatException>(() => Parser.Parse("level Info"));
    }

    [Fact]
    public void TrailingAtom_ThrowsUnexpectedToken()
    {
        // two atoms with no AND/OR: parser consumes first atom then hits 'channel' at End check
        FormatException ex = Assert.Throws<FormatException>(() => Parser.Parse("level >= Info channel = \"x\""));

        Assert.Contains("Unexpected token", ex.Message);
    }

    [Fact]
    public void ChannelWithLevelLiteralRhs_ThrowsExpectedStringLiteral()
    {
        // channel = level: LevelIdent is not a StringLiteral
        FormatException ex = Assert.Throws<FormatException>(() => Parser.Parse("channel = level"));

        Assert.Contains("Expected string literal", ex.Message);
    }

    [Fact]
    public void ChannelWithGteOperator_ThrowsExpectedEqOrNeq()
    {
        // channel >= "x": only = and != are valid for channel
        FormatException ex = Assert.Throws<FormatException>(() => Parser.Parse("channel >= \"x\""));

        Assert.Contains("'='", ex.Message);
        Assert.Contains("'!='", ex.Message);
    }
}
