namespace RimWorks.RimLogging.Filtering;

/// <summary>
/// AST node that matches a log entry's channel against a wildcard pattern, optionally negated.
/// </summary>
internal sealed class ChannelMatchNode : AstNode
{
    public readonly bool Negated;
    public readonly string Pattern;

    public ChannelMatchNode(string pattern, bool negated)
    {
        Pattern = pattern;
        Negated = negated;
    }
}
