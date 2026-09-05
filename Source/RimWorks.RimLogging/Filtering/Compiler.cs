using System;
using System.Collections.Generic;
using System.Globalization;

namespace RimWorks.RimLogging.Filtering;

/// <summary>
/// Compiles a filter expression AST into an executable predicate over log entries.
/// </summary>
internal static class Compiler
{
    /// <summary>
    /// Recursively compiles the given AST node into a predicate that returns whether a
    /// <see cref="LogEntry"/> satisfies the expression.
    /// </summary>
    /// <param name="node">The root AST node to compile.</param>
    /// <returns>A predicate that evaluates the expression against a log entry.</returns>
    /// <exception cref="ArgumentException">Thrown when the node is an unrecognized AST type.</exception>
    /// <exception cref="InvalidOperationException">Thrown for an unknown level comparison operator.</exception>
    public static Func<LogEntry, bool> Compile(AstNode node)
    {
        switch (node)
        {
            case AndNode a:
                Func<LogEntry, bool> al = Compile(a.Left);
                Func<LogEntry, bool> ar = Compile(a.Right);
                return e => al(e) && ar(e);
            case OrNode o:
                Func<LogEntry, bool> ol = Compile(o.Left);
                Func<LogEntry, bool> or = Compile(o.Right);
                return e => ol(e) || or(e);
            case NotNode n:
                Func<LogEntry, bool> nv = Compile(n.Operand);
                return e => !nv(e);
            case LevelCompareNode lc:
                LogLevel rv = lc.RightValue;
                return lc.Op switch
                {
                    TokenKind.OpEq => e => e.Level == rv,
                    TokenKind.OpNeq => e => e.Level != rv,
                    TokenKind.OpLt => e => e.Level < rv,
                    TokenKind.OpLte => e => e.Level <= rv,
                    TokenKind.OpGt => e => e.Level > rv,
                    TokenKind.OpGte => e => e.Level >= rv,
                    _ => throw new InvalidOperationException("Unknown level comparison operator: " + lc.Op),
                };
            case FieldMatchNode fm:
                string pat = fm.Pattern;
                bool neg = fm.Negated;
                MatchField field = fm.Field;
                string? key = fm.Key;
                return e =>
                {
                    bool match = Matches(field, pat, key, e);
                    return neg ? !match : match;
                };
            default:
                throw new ArgumentException("Unknown AST node type: " + node.GetType().Name);
        }
    }

    private static bool Matches(MatchField field, string pattern, string? key, LogEntry e)
    {
        switch (field)
        {
            case MatchField.Text:
                // substring, not wildcard: nobody wants to type text = "*exception*"
                return e.RenderedMessage.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;
            case MatchField.Mod:
                return e.Mod != null && WildcardMatcher.Match(pattern, e.Mod);
            case MatchField.Context:
                return MatchesContext(e.Context, key, pattern);
            default:
                return WildcardMatcher.Match(pattern, e.Channel);
        }
    }

    /// <summary>Finds the key case-insensitively; a missing key never matches.</summary>
    private static bool MatchesContext(IReadOnlyDictionary<string, object?>? context, string? key, string pattern)
    {
        if (context == null || key == null) return false;
        foreach (KeyValuePair<string, object?> pair in context)
        {
            // keys come from template placeholders and property names, so the author rarely matches casing
            if (!string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase)) continue;
            string text = Convert.ToString(pair.Value, CultureInfo.InvariantCulture) ?? string.Empty;
            return WildcardMatcher.Match(pattern, text);
        }
        return false;
    }
}
