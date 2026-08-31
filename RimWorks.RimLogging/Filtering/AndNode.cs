namespace RimWorks.RimLogging.Filtering;

/// <summary>
/// AST node representing a logical AND of two sub-expressions; both operands must match.
/// </summary>
internal sealed class AndNode : AstNode
{
    public readonly AstNode Left;
    public readonly AstNode Right;

    public AndNode(AstNode left, AstNode right)
    {
        Left = left;
        Right = right;
    }
}
