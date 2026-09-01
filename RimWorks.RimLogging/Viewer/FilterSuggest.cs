using System;
using System.Collections.Generic;
using RimWorks.RimLogging.Filtering;

namespace RimWorks.RimLogging.Viewer;

/// <summary>Completion candidates for the text typed so far, plus the span they replace.</summary>
internal readonly struct Suggestions {
    public static readonly Suggestions None = new Suggestions(Array.Empty<string>(), 0, 0);

    public readonly IReadOnlyList<string> Items;
    public readonly int ReplaceStart;
    public readonly int ReplaceLength;

    public Suggestions(IReadOnlyList<string> items, int replaceStart, int replaceLength) {
        Items = items;
        ReplaceStart = replaceStart;
        ReplaceLength = replaceLength;
    }

    public bool Any => Items.Count > 0;

    /// <summary>Splices <paramref name="item"/> into <paramref name="source"/> over the span this covers.</summary>
    public string Apply(string source, string item) {
        string head = source.Substring(0, ReplaceStart);
        string tail = source.Substring(ReplaceStart + ReplaceLength);
        string joiner = item == "(" || tail.StartsWith(" ", StringComparison.Ordinal) ? "" : " ";
        return head + item + joiner + tail;
    }
}

/// <summary>Works out what may legally follow a partly typed filter expression.</summary>
/// <remarks>The grammar is small, so this reads the last complete token instead of parsing incrementally.</remarks>
internal static class FilterSuggest {
    private static readonly string[] Levels = { "Trace", "Debug", "Info", "Warn", "Error", "Fatal" };
    private static readonly string[] TermStarts = { "level", "channel", "NOT", "(" };
    private static readonly string[] LevelOps = { "=", "!=", "<", "<=", ">", ">=" };
    private static readonly string[] StringOps = { "=", "!=" };
    private static readonly string[] Connectors = { "AND", "OR" };

    private const string WordChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_.*!=<>()";

    /// <summary>Candidates for the DSL box, assuming the caret sits at the end of <paramref name="source"/>.</summary>
    public static Suggestions For(string source, IReadOnlyCollection<string> channels) {
        source ??= string.Empty;

        int quote = UnclosedQuoteIndex(source);
        if (quote >= 0) {
            return Filtered(QuotedChannels(channels), source.Substring(quote), quote, source.Length - quote);
        }

        int partialStart = PartialWordStart(source);
        string partial = source.Substring(partialStart);

        List<Token>? tokens = TryTokenize(source.Substring(0, partialStart));
        if (tokens == null) {
            return Suggestions.None;
        }

        return Filtered(PoolAfter(tokens, channels), partial, partialStart, partial.Length);
    }

    /// <summary>Candidates for the plain-substring channel box, which replaces the whole field.</summary>
    public static Suggestions ForChannelFilter(string source, IReadOnlyCollection<string> channelIds) {
        source ??= string.Empty;

        List<string> hits = new List<string>();
        foreach (string id in channelIds) {
            if (source.Length == 0 || id.IndexOf(source, StringComparison.OrdinalIgnoreCase) >= 0) {
                hits.Add(id);
            }
        }

        hits.Sort(StringComparer.OrdinalIgnoreCase);
        return hits.Count == 0 ? Suggestions.None : new Suggestions(hits, 0, source.Length);
    }

    private static IReadOnlyList<string> PoolAfter(List<Token> tokens, IReadOnlyCollection<string> channels) {
        // Tokenize always appends End, so the last meaningful token sits one before it.
        int last = tokens.Count - 2;
        if (last < 0) {
            return TermStarts;
        }

        switch (tokens[last].Kind) {
            case TokenKind.And:
            case TokenKind.Or:
            case TokenKind.Not:
            case TokenKind.LParen:
                return TermStarts;

            case TokenKind.LevelIdent:
                return LevelOps;

            case TokenKind.ChannelIdent:
                return StringOps;

            case TokenKind.LevelLiteral:
            case TokenKind.StringLiteral:
            case TokenKind.RParen:
                return Connectors;

            default:
                return IsOperator(tokens[last].Kind) ? OperandsFor(tokens, last, channels) : Array.Empty<string>();
        }
    }

    /// <summary>An operator's operand depends on which keyword opened the term.</summary>
    private static IReadOnlyList<string> OperandsFor(List<Token> tokens, int opIndex, IReadOnlyCollection<string> channels) {
        if (opIndex == 0) {
            return Array.Empty<string>();
        }
        return tokens[opIndex - 1].Kind == TokenKind.ChannelIdent ? QuotedChannels(channels) : Levels;
    }

    private static bool IsOperator(TokenKind kind) {
        return kind == TokenKind.OpEq || kind == TokenKind.OpNeq || kind == TokenKind.OpLt
            || kind == TokenKind.OpLte || kind == TokenKind.OpGt || kind == TokenKind.OpGte;
    }

    private static List<string> QuotedChannels(IReadOnlyCollection<string> channels) {
        List<string> quoted = new List<string>(channels.Count);
        foreach (string channel in channels) {
            quoted.Add("\"" + channel + "\"");
        }
        quoted.Sort(StringComparer.OrdinalIgnoreCase);
        return quoted;
    }

    private static Suggestions Filtered(IReadOnlyList<string> pool, string typed, int start, int length) {
        string probe = typed.TrimStart('"');

        List<string> hits = new List<string>();
        for (int i = 0; i < pool.Count; i++) {
            string candidate = pool[i];
            string bare = candidate.StartsWith("\"", StringComparison.Ordinal) ? candidate.Substring(1) : candidate;
            if (probe.Length == 0 || bare.StartsWith(probe, StringComparison.OrdinalIgnoreCase)) {
                hits.Add(candidate);
            }
        }

        return hits.Count == 0 ? Suggestions.None : new Suggestions(hits, start, length);
    }

    private static List<Token>? TryTokenize(string committed) {
        try {
            return Lexer.Tokenize(committed);
        }
        catch (FormatException) {
            return null;
        }
    }

    /// <summary>Index of a quote with no partner, or -1 when every quote is paired.</summary>
    private static int UnclosedQuoteIndex(string source) {
        int index = -1;
        for (int i = 0; i < source.Length; i++) {
            if (source[i] == '"') {
                index = index < 0 ? i : -1;
            }
        }
        return index;
    }

    private static int PartialWordStart(string source) {
        int i = source.Length;
        while (i > 0 && WordChars.IndexOf(source[i - 1]) >= 0) {
            i--;
        }
        return i;
    }
}
