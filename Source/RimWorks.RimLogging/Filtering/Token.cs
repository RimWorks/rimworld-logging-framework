namespace RimWorks.RimLogging.Filtering;

/// <summary>
/// A single lexical token produced by the filter expression <see cref="Lexer"/>.
/// </summary>
internal readonly struct Token
{
    /// <summary>The category of the token.</summary>
    public readonly TokenKind Kind;
    /// <summary>The raw source text of the token (for string literals, the unquoted contents).</summary>
    public readonly string Text;
    /// <summary>The zero-based character offset of the token within the source input.</summary>
    public readonly int Pos;

    /// <summary>Creates a token with the given kind, text, and source position.</summary>
    /// <param name="k">The token kind.</param>
    /// <param name="t">The token text.</param>
    /// <param name="p">The zero-based source position.</param>
    public Token(TokenKind k, string t, int p)
    {
        Kind = k;
        Text = t;
        Pos = p;
    }
}
