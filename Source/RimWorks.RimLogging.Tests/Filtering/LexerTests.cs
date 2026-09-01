using System;
using System.Collections.Generic;
using RimWorks.RimLogging.Filtering;
using Xunit;

namespace RimWorks.RimLogging.Tests.Filtering;

public class LexerTests
{
    // 1. Each operator emits the correct TokenKind
    [Theory]
    [InlineData("=",  TokenKind.OpEq)]
    [InlineData("!=", TokenKind.OpNeq)]
    [InlineData("<",  TokenKind.OpLt)]
    [InlineData("<=", TokenKind.OpLte)]
    [InlineData(">",  TokenKind.OpGt)]
    [InlineData(">=", TokenKind.OpGte)]
    public void Operator_EmitsCorrectKind(string input, TokenKind expected)
    {
        List<Token> tokens = Lexer.Tokenize(input);

        Assert.Equal(expected, tokens[0].Kind);
        Assert.Equal(input, tokens[0].Text);
        Assert.Equal(0, tokens[0].Pos);
    }

    // 2. String literal extraction: quotes stripped, pos is opening-quote index
    [Fact]
    public void StringLiteral_StripQuotesAndCorrectPos()
    {
        List<Token> tokens = Lexer.Tokenize("\"foo bar\"");

        Assert.Equal(TokenKind.StringLiteral, tokens[0].Kind);
        Assert.Equal("foo bar", tokens[0].Text);
        Assert.Equal(0, tokens[0].Pos);
    }

    [Fact]
    public void StringLiteral_PosIsOpeningQuoteIndex_WhenPrecededByWhitespace()
    {
        List<Token> tokens = Lexer.Tokenize("   \"hello\"");

        Assert.Equal(TokenKind.StringLiteral, tokens[0].Kind);
        Assert.Equal("hello", tokens[0].Text);
        Assert.Equal(3, tokens[0].Pos);
    }

    // 3. Keyword case-insensitivity: AND, OR, NOT, level, channel
    [Theory]
    [InlineData("AND")]
    [InlineData("and")]
    [InlineData("And")]
    [InlineData("aNd")]
    public void And_CaseInsensitive(string input)
    {
        List<Token> tokens = Lexer.Tokenize(input);

        Assert.Equal(TokenKind.And, tokens[0].Kind);
        Assert.Equal(input, tokens[0].Text);
    }

    [Theory]
    [InlineData("OR")]
    [InlineData("or")]
    [InlineData("Or")]
    public void Or_CaseInsensitive(string input)
    {
        List<Token> tokens = Lexer.Tokenize(input);

        Assert.Equal(TokenKind.Or, tokens[0].Kind);
        Assert.Equal(input, tokens[0].Text);
    }

    [Theory]
    [InlineData("NOT")]
    [InlineData("not")]
    [InlineData("Not")]
    public void Not_CaseInsensitive(string input)
    {
        List<Token> tokens = Lexer.Tokenize(input);

        Assert.Equal(TokenKind.Not, tokens[0].Kind);
        Assert.Equal(input, tokens[0].Text);
    }

    [Theory]
    [InlineData("level")]
    [InlineData("LEVEL")]
    [InlineData("Level")]
    public void LevelIdent_CaseInsensitive(string input)
    {
        List<Token> tokens = Lexer.Tokenize(input);

        Assert.Equal(TokenKind.LevelIdent, tokens[0].Kind);
    }

    [Theory]
    [InlineData("channel")]
    [InlineData("CHANNEL")]
    [InlineData("Channel")]
    public void ChannelIdent_CaseInsensitive(string input)
    {
        List<Token> tokens = Lexer.Tokenize(input);

        Assert.Equal(TokenKind.ChannelIdent, tokens[0].Kind);
    }

    // 4. Level literals (any case) produce LevelLiteral with original Text
    [Theory]
    [InlineData("Trace")]
    [InlineData("TRACE")]
    [InlineData("trace")]
    [InlineData("Debug")]
    [InlineData("DEBUG")]
    [InlineData("debug")]
    [InlineData("Info")]
    [InlineData("INFO")]
    [InlineData("info")]
    [InlineData("Warn")]
    [InlineData("WARN")]
    [InlineData("warn")]
    [InlineData("Error")]
    [InlineData("ERROR")]
    [InlineData("error")]
    [InlineData("Fatal")]
    [InlineData("FATAL")]
    [InlineData("fatal")]
    public void LevelLiteral_AnyCase_ProducesCorrectKindAndPreservesText(string input)
    {
        List<Token> tokens = Lexer.Tokenize(input);

        Assert.Equal(TokenKind.LevelLiteral, tokens[0].Kind);
        Assert.Equal(input, tokens[0].Text);
    }

    // 5. Parentheses emit LParen / RParen
    [Fact]
    public void LParen_EmitsLParenKind()
    {
        List<Token> tokens = Lexer.Tokenize("(");

        Assert.Equal(TokenKind.LParen, tokens[0].Kind);
        Assert.Equal("(", tokens[0].Text);
        Assert.Equal(0, tokens[0].Pos);
    }

    [Fact]
    public void RParen_EmitsRParenKind()
    {
        List<Token> tokens = Lexer.Tokenize(")");

        Assert.Equal(TokenKind.RParen, tokens[0].Kind);
        Assert.Equal(")", tokens[0].Text);
        Assert.Equal(0, tokens[0].Pos);
    }

    // 6. Whitespace is skipped: no token emitted for spaces/tabs
    [Fact]
    public void Whitespace_IsSkipped()
    {
        List<Token> tokens = Lexer.Tokenize("   \t   ");

        Assert.Single(tokens);
        Assert.Equal(TokenKind.End, tokens[0].Kind);
    }

    // 7. End token always appended with Pos == input.Length
    [Theory]
    [InlineData("")]
    [InlineData("=")]
    [InlineData("level >= Info")]
    public void End_AlwaysAppendedWithPosEqualToInputLength(string input)
    {
        List<Token> tokens = Lexer.Tokenize(input);

        Token end = tokens[tokens.Count - 1];
        Assert.Equal(TokenKind.End, end.Kind);
        Assert.Equal(input.Length, end.Pos);
    }

    // 8. Unterminated string throws FormatException with position info
    [Fact]
    public void UnterminatedString_ThrowsFormatException()
    {
        FormatException ex = Assert.Throws<FormatException>(() => Lexer.Tokenize("\"unterminated"));

        Assert.Contains("0", ex.Message);
    }

    [Fact]
    public void UnterminatedString_AtNonZeroPos_ThrowsWithCorrectPosition()
    {
        FormatException ex = Assert.Throws<FormatException>(() => Lexer.Tokenize("level \"unterminated"));

        Assert.Contains("6", ex.Message);
    }

    // 9. Unknown identifier throws FormatException mentioning word and position
    [Fact]
    public void UnknownIdentifier_ThrowsFormatException()
    {
        FormatException ex = Assert.Throws<FormatException>(() => Lexer.Tokenize("foo"));

        Assert.Contains("foo", ex.Message);
        Assert.Contains("0", ex.Message);
    }

    [Fact]
    public void UnknownIdentifier_AtOffset_ThrowsWithPosition()
    {
        FormatException ex = Assert.Throws<FormatException>(() => Lexer.Tokenize("level xyz"));

        Assert.Contains("xyz", ex.Message);
        Assert.Contains("6", ex.Message);
    }

    // 10. Unexpected character throws FormatException with position info
    [Theory]
    [InlineData("@", 0)]
    [InlineData("#", 0)]
    [InlineData("level @", 6)]
    public void UnexpectedCharacter_ThrowsFormatException(string input, int expectedPos)
    {
        FormatException ex = Assert.Throws<FormatException>(() => Lexer.Tokenize(input));

        Assert.Contains(expectedPos.ToString(), ex.Message);
    }

    // 11. Compound expression produces correct token sequence
    [Fact]
    public void CompoundExpression_ProducesCorrectTokenSequence()
    {
        List<Token> tokens = Lexer.Tokenize("level >= Info AND channel = \"Cosmere.*\"");

        Assert.Equal(8, tokens.Count);
        Assert.Equal(TokenKind.LevelIdent,    tokens[0].Kind);
        Assert.Equal(TokenKind.OpGte,         tokens[1].Kind);
        Assert.Equal(TokenKind.LevelLiteral,  tokens[2].Kind);
        Assert.Equal(TokenKind.And,           tokens[3].Kind);
        Assert.Equal(TokenKind.ChannelIdent,  tokens[4].Kind);
        Assert.Equal(TokenKind.OpEq,          tokens[5].Kind);
        Assert.Equal(TokenKind.StringLiteral, tokens[6].Kind);
        Assert.Equal(TokenKind.End,           tokens[7].Kind);

        Assert.Equal("level",    tokens[0].Text);
        Assert.Equal(">=",       tokens[1].Text);
        Assert.Equal("Info",     tokens[2].Text);
        Assert.Equal("AND",      tokens[3].Text);
        Assert.Equal("channel",  tokens[4].Text);
        Assert.Equal("=",        tokens[5].Text);
        Assert.Equal("Cosmere.*", tokens[6].Text);
    }

    // 12. Position field tracking
    [Fact]
    public void PositionTracking_OpGteHasCorrectPos()
    {
        // "level   >=   Info": '>' is at index 8
        string input = "level   >=   Info";
        List<Token> tokens = Lexer.Tokenize(input);

        Assert.Equal(TokenKind.OpGte, tokens[1].Kind);
        Assert.Equal(8, tokens[1].Pos);
    }
}
