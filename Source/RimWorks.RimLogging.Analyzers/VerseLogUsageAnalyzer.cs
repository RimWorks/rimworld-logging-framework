using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace RimWorks.RimLogging.Analyzers;

/// <summary>
/// Reports <c>Verse.Log</c> writes in a project that already references RimLogging. Stays
/// silent when the compilation cannot see <c>RimWorks.RimLogging.Log</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class VerseLogUsageAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Vanilla's <c>ErrorOnce</c> and <c>WarningOnce</c> share one global key set.</summary>
    public const string SharedKeyRuleId = "RIMLOG001";

    /// <summary>A captured <c>Verse.Log</c> write arrives without its channel or context.</summary>
    public const string LostChannelRuleId = "RIMLOG002";

    private const string Category = "RimLogging.Usage";

    private const string VerseLogTypeName = "Verse.Log";

    private const string RimLoggingTypeName = "RimWorks.RimLogging.Log";

    private static readonly DiagnosticDescriptor SharedKeyRule = new DiagnosticDescriptor(
        SharedKeyRuleId,
        "Verse.Log once-key is shared with every other mod",
        "Verse.Log.{0} dedupes against a game-wide key set; call {1} instead",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
        "Verse.Log.ErrorOnce and WarningOnce record their int key in one static HashSet shared by "
        + "the whole game. Any other mod that picks the same number silently swallows your message. "
        + "RimLogging keys off a string in its own table, and keeps the channel you pass.");

    private static readonly DiagnosticDescriptor LostChannelRule = new DiagnosticDescriptor(
        LostChannelRuleId,
        "Verse.Log write loses its channel and context",
        "Verse.Log.{0} arrives as a bare string with a guessed channel; call {1} instead",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
        "RimLogging's Harmony patch does capture this call, so the entry is not lost. It arrives "
        + "as plain text though: the channel is inferred from the calling assembly rather than "
        + "stated, and there is no message template and no structured context.");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
        = ImmutableArray.Create(SharedKeyRule, LostChannelRule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            INamedTypeSymbol? verseLog = start.Compilation.GetTypeByMetadataName(VerseLogTypeName);
            if (verseLog == null) return;
            if (start.Compilation.GetTypeByMetadataName(RimLoggingTypeName) == null) return;
            start.RegisterOperationAction(ctx => Analyze(ctx, verseLog), OperationKind.Invocation);
        });
    }

    private static void Analyze(OperationAnalysisContext context, INamedTypeSymbol verseLog)
    {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol target = invocation.TargetMethod;
        if (!SymbolEqualityComparer.Default.Equals(target.ContainingType, verseLog)) return;

        VerseLogMethods.Replacement? replacement = VerseLogMethods.Classify(target.Name);
        if (replacement == null) return;

        context.ReportDiagnostic(Diagnostic.Create(
            replacement.Value.SharesVanillaKey ? SharedKeyRule : LostChannelRule,
            CallSiteOf(invocation),
            target.Name,
            replacement.Value.Signature));
    }

    // squiggle "Log.ErrorOnce" rather than the whole call, which often spans several lines
    private static Location CallSiteOf(IInvocationOperation invocation)
        => invocation.Syntax is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member }
            ? member.GetLocation()
            : invocation.Syntax.GetLocation();
}
