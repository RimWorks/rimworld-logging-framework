namespace RimWorks.RimLogging.Filtering;

/// <summary>
/// The kinds of lexical tokens recognized in a filter expression.
/// </summary>
public enum TokenKind
{
    /// <summary>The <c>level</c> keyword introducing a level comparison.</summary>
    LevelIdent,
    /// <summary>The <c>channel</c> keyword introducing a channel match.</summary>
    ChannelIdent,
    /// <summary>The <c>text</c> keyword introducing a message substring match.</summary>
    TextIdent,
    /// <summary>The <c>mod</c> keyword introducing a mod match.</summary>
    ModIdent,
    /// <summary>The <c>and</c> logical conjunction keyword.</summary>
    And,
    /// <summary>The <c>or</c> logical disjunction keyword.</summary>
    Or,
    /// <summary>The <c>not</c> logical negation keyword.</summary>
    Not,
    /// <summary>An opening parenthesis <c>(</c>.</summary>
    LParen,
    /// <summary>A closing parenthesis <c>)</c>.</summary>
    RParen,
    /// <summary>The equality operator <c>=</c>.</summary>
    OpEq,
    /// <summary>The inequality operator <c>!=</c>.</summary>
    OpNeq,
    /// <summary>The less-than operator <c>&lt;</c>.</summary>
    OpLt,
    /// <summary>The less-than-or-equal operator <c>&lt;=</c>.</summary>
    OpLte,
    /// <summary>The greater-than operator <c>&gt;</c>.</summary>
    OpGt,
    /// <summary>The greater-than-or-equal operator <c>&gt;=</c>.</summary>
    OpGte,
    /// <summary>A log level literal such as <c>trace</c>, <c>info</c>, or <c>error</c>.</summary>
    LevelLiteral,
    /// <summary>A double-quoted string literal, typically a channel pattern.</summary>
    StringLiteral,
    /// <summary>The end-of-input sentinel token.</summary>
    End,
}
