using System;
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

    private const string RimLoggingTypeName = "RimWorks.RimLogging.Log";

    private readonly string hostTypeName;

    private readonly Func<string, VerseLogMethods.Replacement?> classify;

    private readonly DiagnosticDescriptor sharedKeyRule;

    private readonly DiagnosticDescriptor lostChannelRule;

    /// <summary>Initializes a new instance of the <see cref="VerseLogUsageAnalyzer"/> class.</summary>
    public VerseLogUsageAnalyzer()
        : this(HostLog.TypeName)
    {
    }

    internal VerseLogUsageAnalyzer(string hostTypeName)
        : this(hostTypeName, VerseLogMethods.Classify)
    {
    }

    internal VerseLogUsageAnalyzer(string hostTypeName, Func<string, VerseLogMethods.Replacement?> classify)
    {
        this.hostTypeName = hostTypeName;
        this.classify = classify;
        sharedKeyRule = new DiagnosticDescriptor(
            SharedKeyRuleId,
            hostTypeName + " once-key is shared with every other mod",
            hostTypeName + ".{0} dedupes against a game-wide key set; call {1} instead",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description:
            hostTypeName + ".ErrorOnce and WarningOnce record their int key in one static HashSet shared by "
            + "the whole game. Any other mod that picks the same number silently swallows your message. "
            + "RimLogging keys off a string in its own table, and keeps the channel you pass.");
        lostChannelRule = new DiagnosticDescriptor(
            LostChannelRuleId,
            hostTypeName + " write loses its channel and context",
            hostTypeName + ".{0} arrives as a bare string with a guessed channel; call {1} instead",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description:
            "RimLogging's Harmony patch does capture this call, so the entry is not lost. It arrives "
            + "as plain text though: the channel is inferred from the calling assembly rather than "
            + "stated, and there is no message template and no structured context.");
        SupportedDiagnostics = ImmutableArray.Create(sharedKeyRule, lostChannelRule);
    }

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            INamedTypeSymbol? hostLog = start.Compilation.GetTypeByMetadataName(hostTypeName);
            if (hostLog == null) return;
            if (start.Compilation.GetTypeByMetadataName(RimLoggingTypeName) == null) return;
            start.RegisterOperationAction(ctx => Analyze(ctx, hostLog), OperationKind.Invocation);
        });
    }

    private void Analyze(OperationAnalysisContext context, INamedTypeSymbol hostLog)
    {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol target = invocation.TargetMethod;
        if (!SymbolEqualityComparer.Default.Equals(target.ContainingType, hostLog)) return;

        VerseLogMethods.Replacement? replacement = classify(target.Name);
        if (replacement == null) return;

        context.ReportDiagnostic(Diagnostic.Create(
            replacement.Value.SharesVanillaKey ? sharedKeyRule : lostChannelRule,
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
