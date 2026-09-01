using System;
using System.Collections.Generic;

namespace RimWorks.RimLogging.Filtering;

/// <summary>
/// Converts filter expression source text into a list of <see cref="Token"/> values.
/// </summary>
internal static class Lexer
{
    /// <summary>
    /// Tokenizes the input, recognizing parentheses, operators, double-quoted strings, and
    /// keyword/level identifiers, and appends a trailing <see cref="TokenKind.End"/> token.
    /// </summary>
    /// <param name="input">The filter expression source text.</param>
    /// <returns>The list of tokens, ending with an <see cref="TokenKind.End"/> token.</returns>
    /// <exception cref="FormatException">
    /// Thrown for an unterminated string, an unexpected character, or an unknown identifier.
    /// </exception>
    public static List<Token> Tokenize(string input)
    {
        List<Token> tokens = new();
        int i = 0;
        while (i < input.Length)
        {
            char c = input[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }
            i = ScanToken(input, i, tokens);
        }
        tokens.Add(new Token(TokenKind.End, "", input.Length));
        return tokens;
    }

    private static int ScanToken(string input, int i, List<Token> tokens)
    {
        char c = input[i];
        switch (c)
        {
            case '(': tokens.Add(new Token(TokenKind.LParen, "(", i)); return i + 1;
            case ')': tokens.Add(new Token(TokenKind.RParen, ")", i)); return i + 1;
            case '"': return ScanString(input, i, tokens);
        }
        if (c is '=' or '!' or '<' or '>') return ScanOperator(input, i, tokens);
        if (char.IsLetter(c)) return ScanIdentifier(input, i, tokens);
        throw new FormatException($"Unexpected character '{c}' at {i}");
    }

    private static int ScanString(string input, int i, List<Token> tokens)
    {
        int end = input.IndexOf('"', i + 1);
        if (end < 0) throw new FormatException($"Unterminated string at {i}");
        tokens.Add(new Token(TokenKind.StringLiteral, input.Substring(i + 1, end - i - 1), i));
        return end + 1;
    }

    private static int ScanOperator(string input, int i, List<Token> tokens)
    {
        char c = input[i];
        char? next = i + 1 < input.Length ? input[i + 1] : null;
        switch (c)
        {
            case '=': tokens.Add(new Token(TokenKind.OpEq, "=", i)); return i + 1;
            case '!' when next == '=': tokens.Add(new Token(TokenKind.OpNeq, "!=", i)); return i + 2;
            case '<' when next == '=': tokens.Add(new Token(TokenKind.OpLte, "<=", i)); return i + 2;
            case '>' when next == '=': tokens.Add(new Token(TokenKind.OpGte, ">=", i)); return i + 2;
            case '<': tokens.Add(new Token(TokenKind.OpLt, "<", i)); return i + 1;
            case '>': tokens.Add(new Token(TokenKind.OpGt, ">", i)); return i + 1;
            default: throw new FormatException($"Unexpected character '{c}' at {i}");
        }
    }

    private static int ScanIdentifier(string input, int i, List<Token> tokens)
    {
        int start = i;
        while (i < input.Length && (char.IsLetterOrDigit(input[i]) || input[i] == '_')) i++;
        tokens.Add(ClassifyIdent(input.Substring(start, i - start), start));
        return i;
    }

    private static Token ClassifyIdent(string word, int pos)
    {
        string lower = word.ToLowerInvariant();
        return lower switch
        {
            "and" => new Token(TokenKind.And, word, pos),
            "or"  => new Token(TokenKind.Or, word, pos),
            "not" => new Token(TokenKind.Not, word, pos),
            "level"   => new Token(TokenKind.LevelIdent, word, pos),
            "channel" => new Token(TokenKind.ChannelIdent, word, pos),
            "trace" or "debug" or "info" or "warn" or "error" or "fatal"
                  => new Token(TokenKind.LevelLiteral, word, pos),
            _ => throw new FormatException($"Unknown identifier '{word}' at {pos}"),
        };
    }
}
