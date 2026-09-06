using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace RimWorks.RimLogging.Analyzers;

/// <summary>
/// Reports a channel handed to a default-channel overload. The channel becomes the message, the
/// real message is dropped as an unused format argument, and the whole thing compiles clean.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ChannelArgumentAnalyzer : DiagnosticAnalyzer
{
    /// <summary>A channel was passed where the overload expects a message template.</summary>
    public const string ChannelAsMessageRuleId = ChannelArgumentRules.ChannelAsMessageRuleId;

    private const string Category = "RimLogging.Usage";

    private const string RimLoggingTypeName = "RimWorks.RimLogging.Log";

    private static readonly DiagnosticDescriptor ChannelAsMessageRule = new DiagnosticDescriptor(
        ChannelAsMessageRuleId,
        "Channel passed to an overload that has no channel parameter",
        "Log.{0}'s first argument is the message, not the channel; call Log.{0}To instead",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
        "Log.Info(channel, message) binds to Log.Info(string template, params object?[] args). The "
        + "channel becomes the message, the real message becomes a format argument that nothing "
        + "consumes, and the entry lands on the default channel. Log.InfoTo takes the channel first.");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
        = ImmutableArray.Create(ChannelAsMessageRule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            INamedTypeSymbol? log = start.Compilation.GetTypeByMetadataName(RimLoggingTypeName);
            if (log == null) return;
            start.RegisterOperationAction(ctx => Analyze(ctx, log), OperationKind.Invocation);
        });
    }

    private static void Analyze(OperationAnalysisContext context, INamedTypeSymbol log)
    {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol target = invocation.TargetMethod;
        if (!SymbolEqualityComparer.Default.Equals(target.ContainingType, log)) return;
        if (!ChannelArgumentRules.HasChannelSibling(target.Name)) return;

        // only the (string template, params object?[] args) shape can swallow a channel
        if (target.Parameters.Length != 2 || !target.Parameters[1].IsParams) return;
        if (log.GetMembers(target.Name + ChannelArgumentRules.ChannelSuffix).IsEmpty) return;

        // a params argument collapses every value into one array, so count the elements
        if (invocation.Arguments.Length != 2) return;
        if (invocation.Arguments[1].Value is not IArrayCreationOperation { Initializer: { } passed }) return;
        if (passed.ElementValues.Length != 1) return;

        // each element carries an implicit string-to-object conversion, so look through it
        IOperation message = passed.ElementValues[0];
        if (message is IConversionOperation conversion) message = conversion.Operand;
        if (message.Type?.SpecialType != SpecialType.System_String) return;

        // an argument named at the call site was chosen on purpose, not by accident
        if (invocation.Arguments.Any(a => a.Syntax is ArgumentSyntax { NameColon: not null })) return;

        if (invocation.Arguments[0].Value.ConstantValue is not { HasValue: true, Value: string first }) return;
        if (!ChannelArgumentRules.LooksLikeChannel(first)) return;

        context.ReportDiagnostic(Diagnostic.Create(
            ChannelAsMessageRule, CallSiteOf(invocation), target.Name));
    }

    // squiggle "Log.Info" rather than the whole call, which often spans several lines
    private static Location CallSiteOf(IInvocationOperation invocation)
        => invocation.Syntax is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member }
            ? member.GetLocation()
            : invocation.Syntax.GetLocation();
}
