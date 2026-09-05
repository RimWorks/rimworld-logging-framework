namespace RimWorks.RimLogging.Filtering;

/// <summary>Which entry field a <see cref="FieldMatchNode"/> tests.</summary>
internal enum MatchField
{
    /// <summary>The entry's channel, matched as a wildcard pattern.</summary>
    Channel,

    /// <summary>The entry's rendered message, matched as a case-insensitive substring.</summary>
    Text,

    /// <summary>The owning mod, matched as a wildcard pattern.</summary>
    Mod,

    /// <summary>A structured-context value, found by key and matched as a wildcard pattern.</summary>
    Context,
}

/// <summary>
/// AST node that matches one of a log entry's string fields against a pattern, optionally negated.
/// </summary>
internal sealed class FieldMatchNode : AstNode
{
    public readonly MatchField Field;
    public readonly bool Negated;
    public readonly string Pattern;

    /// <summary>The context key, for <see cref="MatchField.Context"/> only. Null for every other field.</summary>
    public readonly string? Key;

    public FieldMatchNode(MatchField field, string pattern, bool negated, string? key = null)
    {
        Field = field;
        Pattern = pattern;
        Negated = negated;
        Key = key;
    }
}
