namespace RimWorks.RimLogging.Filtering;

/// <summary>
/// AST node that compares a log entry's level against a fixed level using a comparison operator.
/// </summary>
internal sealed class LevelCompareNode : AstNode
{
    public readonly TokenKind Op;
    public readonly LogLevel RightValue;

    public LevelCompareNode(TokenKind op, LogLevel rightValue)
    {
        Op = op;
        RightValue = rightValue;
    }
}
