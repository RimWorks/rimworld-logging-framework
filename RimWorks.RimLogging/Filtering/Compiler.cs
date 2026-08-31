using System;

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
                    TokenKind.OpEq  => e => e.Level == rv,
                    TokenKind.OpNeq => e => e.Level != rv,
                    TokenKind.OpLt  => e => e.Level <  rv,
                    TokenKind.OpLte => e => e.Level <= rv,
                    TokenKind.OpGt  => e => e.Level >  rv,
                    TokenKind.OpGte => e => e.Level >= rv,
                    _ => throw new InvalidOperationException("Unknown level comparison operator: " + lc.Op),
                };
            case ChannelMatchNode cm:
                string pat = cm.Pattern;
                bool neg = cm.Negated;
                return e =>
                {
                    bool match = WildcardMatcher.Match(pat, e.Channel);
                    return neg ? !match : match;
                };
            default:
                throw new ArgumentException("Unknown AST node type: " + node.GetType().Name);
        }
    }
}
