using System;

namespace RimWorks.RimLogging.Filtering;

/// <summary>
/// A parsed and compiled log filter expression that can be evaluated against log entries.
/// </summary>
public sealed class FilterExpression
{
    private readonly AstNode _ast;
    private readonly Func<LogEntry, bool> _predicate;

    /// <summary>The original source text the expression was parsed from.</summary>
    public string Source { get; }

    private FilterExpression(string source, AstNode ast)
    {
        Source = source;
        _ast = ast;
        _predicate = Compiler.Compile(ast);
    }

    /// <summary>
    /// Parses and compiles the given source text into a <see cref="FilterExpression"/>.
    /// </summary>
    /// <param name="input">The filter expression source text.</param>
    /// <returns>The compiled filter expression.</returns>
    /// <exception cref="FormatException">Thrown when the input is not a valid filter expression.</exception>
    public static FilterExpression Parse(string input)
        => new FilterExpression(input, Parser.Parse(input));

    /// <summary>
    /// Attempts to parse the given source text without throwing on failure.
    /// </summary>
    /// <param name="input">The filter expression source text.</param>
    /// <param name="result">The parsed expression on success; otherwise <c>null</c>.</param>
    /// <param name="error">The parse error message on failure; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise <c>false</c>.</returns>
    public static bool TryParse(string input, out FilterExpression? result, out string? error)
    {
        try
        {
            result = Parse(input);
            error = null;
            return true;
        }
        catch (FormatException ex)
        {
            result = null;
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Evaluates the expression against a log entry.
    /// </summary>
    /// <param name="entry">The log entry to test.</param>
    /// <returns><c>true</c> if the entry satisfies the filter; otherwise <c>false</c>.</returns>
    public bool Match(LogEntry entry) => _predicate(entry);

    /// <summary>
    /// Returns a normalized, parenthesized rendering of the parsed expression tree.
    /// </summary>
    /// <returns>A canonical string form of the expression.</returns>
    public override string ToString() => Stringify(_ast);

    private static string Stringify(AstNode n) => n switch
    {
        AndNode a  => "(" + Stringify(a.Left) + " AND " + Stringify(a.Right) + ")",
        OrNode o   => "(" + Stringify(o.Left) + " OR " + Stringify(o.Right) + ")",
        NotNode nt => "NOT " + Stringify(nt.Operand),
        LevelCompareNode lc => "level " + OpToString(lc.Op) + " " + lc.RightValue,
        ChannelMatchNode cm => "channel " + (cm.Negated ? "!= " : "= ") + "\"" + cm.Pattern + "\"",
        _ => "<unknown>",
    };

    private static string OpToString(TokenKind op) => op switch
    {
        TokenKind.OpEq  => "=",
        TokenKind.OpNeq => "!=",
        TokenKind.OpLt  => "<",
        TokenKind.OpLte => "<=",
        TokenKind.OpGt  => ">",
        TokenKind.OpGte => ">=",
        _ => "?",
    };
}
