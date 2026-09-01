namespace RimWorks.RimLogging.Filtering;

/// <summary>
/// AST node representing a logical OR of two sub-expressions; either operand may match.
/// </summary>
internal sealed class OrNode : AstNode
{
    public readonly AstNode Left;
    public readonly AstNode Right;

    public OrNode(AstNode left, AstNode right)
    {
        Left = left;
        Right = right;
    }
}
