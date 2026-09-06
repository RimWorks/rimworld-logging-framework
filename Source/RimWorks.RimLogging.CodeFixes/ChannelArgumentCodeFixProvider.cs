using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RimWorks.RimLogging.Analyzers;

namespace RimWorks.RimLogging.CodeFixes;

/// <summary>
/// Retargets a flagged call to the sibling that takes a channel first, so Log.Info(channel,
/// message) becomes Log.InfoTo(channel, message).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ChannelArgumentCodeFixProvider))]
[Shared]
public sealed class ChannelArgumentCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; }
        = ImmutableArray.Create(ChannelArgumentRules.ChannelAsMessageRuleId);

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
            .ConfigureAwait(false);
        if (root == null) return;

        foreach (Diagnostic diagnostic in context.Diagnostics)
        {
            if (root.FindNode(diagnostic.Location.SourceSpan) is not MemberAccessExpressionSyntax member) continue;

            string renamed = member.Name.Identifier.ValueText + ChannelArgumentRules.ChannelSuffix;
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Call Log." + renamed,
                    ct => Retarget(context.Document, root, member, renamed, ct),
                    equivalenceKey: ChannelArgumentRules.ChannelAsMessageRuleId),
                diagnostic);
        }
    }

    private static Task<Document> Retarget(
        Document document,
        SyntaxNode root,
        MemberAccessExpressionSyntax member,
        string renamed,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // keep the trivia so a call split across lines stays formatted the way it was written
        MemberAccessExpressionSyntax replacement = member.WithName(
            SyntaxFactory.IdentifierName(renamed).WithTriviaFrom(member.Name));

        return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(member, replacement)));
    }
}
