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

namespace RimWorks.RimLogging.Tests.Analyzers;

/// <summary>Guards the RIMLOG003 fix, which retargets Log.Info to Log.InfoTo.</summary>
public class ChannelArgumentCodeFixTests
{
    private static readonly MetadataReference[] FrameworkRefs =
    {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(Path.Combine(
            Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Runtime.dll")),
    };

    private const string RimLoggingStub = @"
namespace RimWorks.RimLogging
{
    public static class Log
    {
        public static void Info(string template, params object?[] args) { }
        public static void InfoTo(string channel, string template, object?[]? args = null) { }
        public static void Warn(string template, params object?[] args) { }
        public static void WarnTo(string channel, string template, object?[]? args = null) { }
    }
}";

    [Fact]
    public async Task RetargetsInfoToInfoTo_KeepingBothArguments()
    {
        string fixedText = await ApplyFixAsync(
            @"RimWorks.RimLogging.Log.Info(""RimWorks.RimLogging"", ""RimLogging initialized"");");

        Assert.Contains(
            @"Log.InfoTo(""RimWorks.RimLogging"", ""RimLogging initialized"")",
            fixedText,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RetargetsWarnToWarnTo()
    {
        string fixedText = await ApplyFixAsync(@"RimWorks.RimLogging.Log.Warn(""A.B"", ""careful"");");

        Assert.Contains(@"Log.WarnTo(""A.B"", ""careful"")", fixedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheFixedCallStillCompiles()
    {
        // ApplyFixAsync asserts zero compiler errors after the rewrite, so reaching here proves it
        await ApplyFixAsync(@"RimWorks.RimLogging.Log.Info(""A.B"", ""x"");");
    }

    [Fact]
    public async Task OffersOneFixNamedAfterTheSibling()
    {
        (_, ImmutableArray<CodeAction> actions) =
            await RegisterAsync(@"RimWorks.RimLogging.Log.Info(""A.B"", ""x"");");

        Assert.Equal("Call Log.InfoTo", Assert.Single(actions).Title);
    }

    [Fact]
    public async Task NothingToFix_OffersNoAction()
    {
        (ImmutableArray<Diagnostic> diagnostics, ImmutableArray<CodeAction> actions) =
            await RegisterAsync(@"RimWorks.RimLogging.Log.InfoTo(""A.B"", ""x"");");

        Assert.Empty(diagnostics);
        Assert.Empty(actions);
    }

    [Fact]
    public async Task FixingOneCall_LeavesAnUnrelatedOneAlone()
    {
        string fixedText = await ApplyFixAsync(
            @"RimWorks.RimLogging.Log.Info(""A.B"", ""x""); RimWorks.RimLogging.Log.Info(""plain message"");");

        Assert.Contains(@"Log.Info(""plain message"")", fixedText, StringComparison.Ordinal);
    }

    [Fact]
    public void FixAllIsSupported_SoAWholeProjectCanBeCleaned()
    {
        Assert.NotNull(new ChannelArgumentCodeFixProvider().GetFixAllProvider());
    }

    private static async Task<string> ApplyFixAsync(string statement)
    {
        (Document document, ImmutableArray<Diagnostic> diagnostics) = await DiagnoseAsync(statement);
        Diagnostic diagnostic = Assert.Single(diagnostics);

        List<CodeAction> actions = new List<CodeAction>();
        CodeFixContext context = new CodeFixContext(
            document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None);
        await new ChannelArgumentCodeFixProvider().RegisterCodeFixesAsync(context);

        CodeAction fix = Assert.Single(actions);
        ImmutableArray<CodeActionOperation> operations = await fix.GetOperationsAsync(CancellationToken.None);
        ApplyChangesOperation apply = Assert.Single(operations.OfType<ApplyChangesOperation>());
        Document fixedDocument = apply.ChangedSolution.GetDocument(document.Id)!;

        // the fix must produce code that still compiles, not just text that looks right
        Compilation fixedCompilation = (await fixedDocument.Project.GetCompilationAsync())!;
        Assert.Empty(fixedCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));

        return (await fixedDocument.GetTextAsync()).ToString();
    }

    private static async Task<(ImmutableArray<Diagnostic>, ImmutableArray<CodeAction>)> RegisterAsync(
        string statement)
    {
        (Document document, ImmutableArray<Diagnostic> diagnostics) = await DiagnoseAsync(statement);

        List<CodeAction> actions = new List<CodeAction>();
        foreach (Diagnostic diagnostic in diagnostics)
        {
            CodeFixContext context = new CodeFixContext(
                document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None);
            await new ChannelArgumentCodeFixProvider().RegisterCodeFixesAsync(context);
        }

        return (diagnostics, actions.ToImmutableArray());
    }

    private static async Task<(Document, ImmutableArray<Diagnostic>)> DiagnoseAsync(string statement)
    {
        string caller = "class Caller { void Run() { " + statement + " } }";

        AdhocWorkspace workspace = new AdhocWorkspace();
        ProjectId projectId = ProjectId.CreateNewId();
        DocumentId callerId = DocumentId.CreateNewId(projectId);

        Solution solution = workspace.CurrentSolution
            .AddProject(projectId, "Test", "Test", LanguageNames.CSharp)
            .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable))
            .AddMetadataReferences(projectId, FrameworkRefs)
            .AddDocument(DocumentId.CreateNewId(projectId), "RimLogging.cs", SourceText.From(RimLoggingStub))
            .AddDocument(callerId, "Caller.cs", SourceText.From(caller));

        Project project = solution.GetProject(projectId)!;
        Compilation compilation = (await project.GetCompilationAsync())!;
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));

        ImmutableArray<Diagnostic> diagnostics = await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new ChannelArgumentAnalyzer()))
            .GetAnalyzerDiagnosticsAsync();

        return (project.GetDocument(callerId)!, diagnostics);
    }
}
