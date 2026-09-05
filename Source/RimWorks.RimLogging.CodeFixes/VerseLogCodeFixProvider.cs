using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;
using RimWorks.RimLogging.Analyzers;

namespace RimWorks.RimLogging.CodeFixes;

/// <summary>
/// Rewrites a flagged <c>Verse.Log</c> call into the matching <c>RimWorks.RimLogging.Log</c>
/// call. See <see cref="VerseLogMethods"/> for the method table both this and the analyzer read.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(VerseLogCodeFixProvider))]
[Shared]
public sealed class VerseLogCodeFixProvider : CodeFixProvider
{
    private const string RimLoggingLogTypeName = "RimWorks.RimLogging.Log";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
        "RIMLOG001",
        "RIMLOG002");

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        Document document = context.Document;
        SyntaxNode? root = await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null) return;

        SemanticModel? semanticModel = await document.GetSemanticModelAsync(context.CancellationToken)
            .ConfigureAwait(false);
        if (semanticModel == null) return;

        foreach (Diagnostic diagnostic in context.Diagnostics)
        {
            CodeAction? fix = BuildFix(root, semanticModel, diagnostic, document);
            if (fix != null) context.RegisterCodeFix(fix, diagnostic);
        }
    }

    private static CodeAction? BuildFix(
        SyntaxNode root,
        SemanticModel semanticModel,
        Diagnostic diagnostic,
        Document document)
    {
        if (root.FindNode(diagnostic.Location.SourceSpan) is not MemberAccessExpressionSyntax memberAccess)
            return null;
        if (memberAccess.Parent is not InvocationExpressionSyntax invocation) return null;

        VerseLogMethods.Replacement? replacement = VerseLogMethods.Classify(memberAccess.Name.Identifier.Text);
        if (replacement == null) return null;

        SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;

        // a named argument (Log.ErrorOnce(text: x, key: 1)) can reorder freely; textual rewriting can't
        if (arguments.Any(a => a.NameColon != null)) return null;

        if (semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol targetMethod) return null;
        if (!MatchesExpectedShape(replacement.Value, targetMethod, arguments)) return null;

        return CodeAction.Create(
            $"Replace with RimWorks.RimLogging.Log.{replacement.Value.FixMethodName}",
            _ => ReplaceInvocationAsync(document, root, invocation, replacement.Value, arguments),
            equivalenceKey: replacement.Value.FixMethodName);
    }

    // catches the Message(object) overload, which has no textual RimLogging equivalent
    private static bool MatchesExpectedShape(
        VerseLogMethods.Replacement replacement,
        IMethodSymbol targetMethod,
        SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        int expectedArgCount = replacement.SwapsArguments ? 2 : 1;
        if (arguments.Count != expectedArgCount || targetMethod.Parameters.Length != expectedArgCount)
            return false;
        if (targetMethod.Parameters[0].Type.SpecialType != SpecialType.System_String) return false;
        if (replacement.SwapsArguments && targetMethod.Parameters[1].Type.SpecialType != SpecialType.System_Int32)
            return false;
        return true;
    }

    private static Task<Document> ReplaceInvocationAsync(
        Document document,
        SyntaxNode root,
        InvocationExpressionSyntax invocation,
        VerseLogMethods.Replacement replacement,
        SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        string textArgumentText = arguments[0].Expression.ToString();
        string argsText = replacement.SwapsArguments
            ? $"{KeyExpressionText(arguments[1].Expression)}, {textArgumentText}"
            : textArgumentText;

        ExpressionSyntax qualifiedMethod = SyntaxFactory
            .ParseExpression($"{RimLoggingLogTypeName}.{replacement.FixMethodName}")
            .WithAdditionalAnnotations(Simplifier.Annotation);
        ArgumentListSyntax newArgumentList = SyntaxFactory.ParseArgumentList($"({argsText})");

        InvocationExpressionSyntax newInvocation = SyntaxFactory
            .InvocationExpression(qualifiedMethod, newArgumentList)
            .WithTriviaFrom(invocation);

        SyntaxNode newRoot = root.ReplaceNode(invocation, newInvocation);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    // an int literal key becomes a string literal; anything else gets .ToString() appended
    private static string KeyExpressionText(ExpressionSyntax key)
    {
        if (key is LiteralExpressionSyntax { Token.Value: int intValue })
            return "\"" + intValue.ToString(CultureInfo.InvariantCulture) + "\"";

        string keyText = key.ToString();
        return NeedsParensForToString(key) ? $"({keyText}).ToString()" : $"{keyText}.ToString()";
    }

    private static bool NeedsParensForToString(ExpressionSyntax expression) => expression is not (
        IdentifierNameSyntax or MemberAccessExpressionSyntax or InvocationExpressionSyntax
        or ElementAccessExpressionSyntax or LiteralExpressionSyntax or ParenthesizedExpressionSyntax
        or ThisExpressionSyntax or BaseExpressionSyntax);
}
