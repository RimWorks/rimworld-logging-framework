extern alias Analyzers;
extern alias CodeFixes;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using RimWorks.RimLogging.Analyzers;
using RimWorks.RimLogging.CodeFixes;
using Xunit;
using AnalyzerMethods = Analyzers::RimWorks.RimLogging.Analyzers.VerseLogMethods;
using FixMethods = CodeFixes::RimWorks.RimLogging.Analyzers.VerseLogMethods;

namespace RimWorks.RimLogging.Tests.Analyzers;

public class VerseLogCodeFixTests
{
    private static readonly MetadataReference[] FrameworkRefs =
    {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(Path.Combine(
            Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Runtime.dll")),
    };

    private const string VerseStub = @"
namespace Verse
{
    public class Pawn
    {
        public int thingIDNumber;
    }

    public static class Log
    {
        public static void Error(string text) { }
        public static void ErrorOnce(string text, int key) { }
        public static void Warning(string text) { }
        public static void WarningOnce(string text, int key) { }
        public static void Message(string text) { }
        public static void Message(object obj) { }
        public static void Clear() { }
    }
}";

    private const string TrailingOptionalStub = @"
namespace Verse
{
    public static class Log
    {
        public static void Error(string text, bool stackTrace = false) { }
        public static void ErrorOnce(string text, string key) { }
    }
}";

    private const string AcmeStub = @"
namespace Acme
{
    public static class Log
    {
        public static void ErrorOnce(string text, string key) { }
        public static void WarningOnce(string text, long key) { }
        public static void Notice(string text) { }
        public static void Trace(string text) { }
    }
}";

    private const string RimLoggingStub = @"
namespace RimWorks.RimLogging
{
    public static class Log
    {
        public static void Error(string message) { }
        public static void Warn(string message) { }
        public static void Info(string message) { }
        public static void ErrorOnce(string key, string message) { }
        public static void WarnOnce(string key, string message) { }
    }
}";

    [Fact]
    public async Task ErrorOnce_IntLiteralKey_SwapsAndConvertsToStringLiteral()
    {
        string fixedText = await ApplyFixAsync(@"Verse.Log.ErrorOnce(""boom"", 12345);");

        Assert.Contains(@"RimWorks.RimLogging.Log.ErrorOnce(""12345"", ""boom"")", fixedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ErrorOnce_NonLiteralKey_AppendsToString()
    {
        string fixedText = await ApplyFixAsync(
            "Verse.Log.ErrorOnce(msg, pawn.thingIDNumber);",
            "string msg, Verse.Pawn pawn");

        Assert.Contains(
            "RimWorks.RimLogging.Log.ErrorOnce(pawn.thingIDNumber.ToString(), msg)",
            fixedText,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task WarningOnce_SwapsArgumentsAndRenamesToWarnOnce()
    {
        string fixedText = await ApplyFixAsync(@"Verse.Log.WarningOnce(""careful"", 7);");

        Assert.Contains(@"RimWorks.RimLogging.Log.WarnOnce(""7"", ""careful"")", fixedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Error_SingleArg_ReplacesWithQualifiedError()
    {
        string fixedText = await ApplyFixAsync(@"Verse.Log.Error(""boom"");");

        Assert.Contains(@"RimWorks.RimLogging.Log.Error(""boom"")", fixedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Warning_SingleArg_ReplacesWithQualifiedWarn()
    {
        string fixedText = await ApplyFixAsync(@"Verse.Log.Warning(""boom"");");

        Assert.Contains(@"RimWorks.RimLogging.Log.Warn(""boom"")", fixedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Message_StringOverload_ReplacesWithQualifiedInfo()
    {
        string fixedText = await ApplyFixAsync(@"Verse.Log.Message(""boom"");");

        Assert.Contains(@"RimWorks.RimLogging.Log.Info(""boom"")", fixedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Message_ObjectOverload_OffersNoFix()
    {
        ImmutableArray<CodeAction> actions = await RegisterFixesAsync("Verse.Log.Message(new object());");

        Assert.Empty(actions);
    }

    [Fact]
    public async Task NamedArgument_OffersNoFixButDiagnosticStillFires()
    {
        (ImmutableArray<Diagnostic> diagnostics, ImmutableArray<CodeAction> actions) =
            await RegisterFixesWithDiagnosticsAsync(@"Verse.Log.ErrorOnce(text: ""boom"", key: 1);");

        Assert.Single(diagnostics);
        Assert.Empty(actions);
    }

    [Fact]
    public void FixAllProvider_IsTheBatchFixer()
    {
        Assert.Same(WellKnownFixAllProviders.BatchFixer, new VerseLogCodeFixProvider().GetFixAllProvider());
    }

    [Fact]
    public async Task FixingOneDiagnostic_LeavesAnUnrelatedClearCallUntouched()
    {
        string fixedText = await ApplyFixAsync(@"Verse.Log.Error(""boom""); Verse.Log.Clear();");

        Assert.Contains("Verse.Log.Clear();", fixedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Error_WithTrailingOptionalParameter_LeftOut_ReplacesWithQualifiedError()
    {
        string fixedText = await ApplyFixAsync(@"Verse.Log.Error(""boom"");", hostStub: TrailingOptionalStub);

        Assert.Contains(@"RimWorks.RimLogging.Log.Error(""boom"")", fixedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Error_PassingTheTrailingOptionalArgument_OffersNoFixButDiagnosticStillFires()
    {
        (ImmutableArray<Diagnostic> diagnostics, ImmutableArray<CodeAction> actions) =
            await RegisterFixesWithDiagnosticsAsync(@"Verse.Log.Error(""boom"", true);", hostStub: TrailingOptionalStub);

        Assert.Single(diagnostics);
        Assert.Empty(actions);
    }

    [Fact]
    public async Task ErrorOnce_WithAStringLiteralKey_MovesTheKeyFirstWithoutToString()
    {
        string fixedText = await ApplyFixAsync(
            @"Acme.Log.ErrorOnce(""x"", ""key"");",
            hostStub: AcmeStub,
            host: AcmeHost());

        Assert.Contains(@"RimWorks.RimLogging.Log.ErrorOnce(""key"", ""x"")", fixedText, StringComparison.Ordinal);
        Assert.DoesNotContain("ToString", fixedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ErrorOnce_WithAStringVariableKey_PassesTheKeyThroughAsItIs()
    {
        string fixedText = await ApplyFixAsync(
            @"Acme.Log.ErrorOnce(""x"", key);",
            "string key",
            hostStub: AcmeStub,
            host: AcmeHost());

        Assert.Contains(@"RimWorks.RimLogging.Log.ErrorOnce(key, ""x"")", fixedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WarningOnce_WithALongKey_OffersNoFixButDiagnosticStillFires()
    {
        (ImmutableArray<Diagnostic> diagnostics, ImmutableArray<CodeAction> actions) =
            await RegisterFixesWithDiagnosticsAsync(@"Acme.Log.WarningOnce(""x"", 7L);", hostStub: AcmeStub, host: AcmeHost());

        Assert.Equal(VerseLogUsageAnalyzer.SharedKeyRuleId, Assert.Single(diagnostics).Id);
        Assert.Empty(actions);
    }

    [Fact]
    public async Task AddedMethodWithAFix_IsRewrittenToItsFixMethod()
    {
        string fixedText = await ApplyFixAsync(
            @"Acme.Log.Notice(""x"");",
            hostStub: AcmeStub,
            host: AcmeHost());

        Assert.Contains(@"RimWorks.RimLogging.Log.Info(""x"")", fixedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddedMethodWithoutAFix_OffersNoFixButDiagnosticStillFires()
    {
        (ImmutableArray<Diagnostic> diagnostics, ImmutableArray<CodeAction> actions) =
            await RegisterFixesWithDiagnosticsAsync(@"Acme.Log.Trace(""x"");", hostStub: AcmeStub, host: AcmeHost());

        Assert.Equal(VerseLogUsageAnalyzer.LostChannelRuleId, Assert.Single(diagnostics).Id);
        Assert.Empty(actions);
    }

    private static (VerseLogUsageAnalyzer Analyzer, VerseLogCodeFixProvider Provider) AcmeHost()
    {
        return (
            new VerseLogUsageAnalyzer("Acme.Log", name => name switch
            {
                "Notice" => new AnalyzerMethods.Replacement(false, "Log.InfoTo(channel, message)", "Info", false),
                "Trace" => new AnalyzerMethods.Replacement(false, "Log.DebugTo(channel, message)", null, false),
                _ => AnalyzerMethods.Classify(name),
            }),
            new VerseLogCodeFixProvider("Acme.Log", name => name switch
            {
                "Notice" => new FixMethods.Replacement(false, "Log.InfoTo(channel, message)", "Info", false),
                "Trace" => new FixMethods.Replacement(false, "Log.DebugTo(channel, message)", null, false),
                _ => FixMethods.Classify(name),
            }));
    }

    private static async Task<string> ApplyFixAsync(
        string statement,
        string extraParameters = "",
        string hostStub = VerseStub,
        (VerseLogUsageAnalyzer Analyzer, VerseLogCodeFixProvider Provider)? host = null)
    {
        (Document document, ImmutableArray<Diagnostic> diagnostics) =
            await DiagnoseAsync(statement, extraParameters, hostStub, host?.Analyzer);
        Diagnostic diagnostic = Assert.Single(diagnostics);

        var actions = new List<CodeAction>();
        var context = new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None);
        await (host?.Provider ?? new VerseLogCodeFixProvider()).RegisterCodeFixesAsync(context);

        CodeAction fix = Assert.Single(actions);
        ImmutableArray<CodeActionOperation> operations = await fix.GetOperationsAsync(CancellationToken.None);
        ApplyChangesOperation apply = Assert.Single(operations.OfType<ApplyChangesOperation>());
        Document fixedDocument = apply.ChangedSolution.GetDocument(document.Id)!;

        // the fix must produce code that still compiles, not just text that looks right
        Compilation fixedCompilation = (await fixedDocument.Project.GetCompilationAsync())!;
        Assert.Empty(fixedCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));

        SourceText text = await fixedDocument.GetTextAsync();
        return text.ToString();
    }

    private static async Task<ImmutableArray<CodeAction>> RegisterFixesAsync(string statement, string extraParameters = "")
    {
        (_, ImmutableArray<CodeAction> actions) = await RegisterFixesWithDiagnosticsAsync(statement, extraParameters);
        return actions;
    }

    private static async Task<(ImmutableArray<Diagnostic> Diagnostics, ImmutableArray<CodeAction> Actions)>
        RegisterFixesWithDiagnosticsAsync(
            string statement,
            string extraParameters = "",
            string hostStub = VerseStub,
            (VerseLogUsageAnalyzer Analyzer, VerseLogCodeFixProvider Provider)? host = null)
    {
        (Document document, ImmutableArray<Diagnostic> diagnostics) =
            await DiagnoseAsync(statement, extraParameters, hostStub, host?.Analyzer);

        var actions = new List<CodeAction>();
        foreach (Diagnostic diagnostic in diagnostics)
        {
            var context = new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None);
            await (host?.Provider ?? new VerseLogCodeFixProvider()).RegisterCodeFixesAsync(context);
        }

        return (diagnostics, actions.ToImmutableArray());
    }

    private static async Task<(Document Document, ImmutableArray<Diagnostic> Diagnostics)> DiagnoseAsync(
        string statement,
        string extraParameters,
        string hostStub,
        VerseLogUsageAnalyzer? analyzer = null)
    {
        string caller = $"class Caller {{ void Run({extraParameters}) {{ {statement} }} }}";

        var workspace = new AdhocWorkspace();
        ProjectId projectId = ProjectId.CreateNewId();
        DocumentId callerId = DocumentId.CreateNewId(projectId);

        Solution solution = workspace.CurrentSolution
            .AddProject(projectId, "Test", "Test", LanguageNames.CSharp)
            .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReferences(projectId, FrameworkRefs)
            .AddDocument(DocumentId.CreateNewId(projectId), "Verse.cs", SourceText.From(hostStub))
            .AddDocument(DocumentId.CreateNewId(projectId), "RimLogging.cs", SourceText.From(RimLoggingStub))
            .AddDocument(callerId, "Caller.cs", SourceText.From(caller));

        Project project = solution.GetProject(projectId)!;
        Compilation compilation = (await project.GetCompilationAsync())!;
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));

        ImmutableArray<Diagnostic> diagnostics = await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer ?? new VerseLogUsageAnalyzer()))
            .GetAnalyzerDiagnosticsAsync();

        Document callerDocument = project.GetDocument(callerId)!;
        SyntaxTree callerTree = (await callerDocument.GetSyntaxTreeAsync())!;
        ImmutableArray<Diagnostic> callerDiagnostics = diagnostics
            .Where(d => d.Location.SourceTree == callerTree)
            .ToImmutableArray();

        return (callerDocument, callerDiagnostics);
    }
}
