namespace RimWorks.RimLogging.Filtering;

/// <summary>
/// AST node representing a logical NOT that negates the result of its operand.
/// </summary>
internal sealed class NotNode : AstNode
{
    public readonly AstNode Operand;

    public NotNode(AstNode operand)
    {
        Operand = operand;
    }
}
