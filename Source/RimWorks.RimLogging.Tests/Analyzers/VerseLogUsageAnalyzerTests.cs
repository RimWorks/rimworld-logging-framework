using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using RimWorks.RimLogging.Analyzers;
using Xunit;

namespace RimWorks.RimLogging.Tests.Analyzers;

public class VerseLogUsageAnalyzerTests
{
    // Only the framework assemblies, so the negative test can leave RimLogging genuinely absent.
    private static readonly MetadataReference[] FrameworkRefs =
    {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(Path.Combine(
            Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Runtime.dll")),
    };

    private const string VerseStub = @"
namespace Verse
{
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

    private const string RimLoggingStub = @"
namespace RimWorks.RimLogging
{
    public static class Log
    {
        public static void ErrorTo(string channel, string message) { }
        public static void ErrorOnce(string key, string message) { }
        public static void ErrorOnceTo(string channel, string key, string message) { }
    }
}";

    [Fact]
    public void ErrorOnce_SharesVanillaKeySet_ReportsSharedKeyRule()
    {
        ImmutableArray<Diagnostic> found = Diagnose(@"Verse.Log.ErrorOnce(""boom"", 42);");

        Assert.Equal(VerseLogUsageAnalyzer.SharedKeyRuleId, Assert.Single(found).Id);
    }

    [Fact]
    public void WarningOnce_SharesVanillaKeySet_ReportsSharedKeyRule()
    {
        ImmutableArray<Diagnostic> found = Diagnose(@"Verse.Log.WarningOnce(""careful"", 7);");

        Assert.Equal(VerseLogUsageAnalyzer.SharedKeyRuleId, Assert.Single(found).Id);
    }

    [Fact]
    public void Error_IsCapturedButUnchannelled_ReportsLostChannelRule()
    {
        ImmutableArray<Diagnostic> found = Diagnose(@"Verse.Log.Error(""boom"");");

        Assert.Equal(VerseLogUsageAnalyzer.LostChannelRuleId, Assert.Single(found).Id);
    }

    [Fact]
    public void Message_ObjectOverload_IsFlaggedLikeTheStringOne()
    {
        ImmutableArray<Diagnostic> found = Diagnose(@"Verse.Log.Message(new object());");

        Assert.Equal(VerseLogUsageAnalyzer.LostChannelRuleId, Assert.Single(found).Id);
    }

    [Fact]
    public void Clear_IsNotALogWrite_ReportsNothing()
    {
        Assert.Empty(Diagnose("Verse.Log.Clear();"));
    }

    [Fact]
    public void WithoutARimLoggingReference_ReportsNothing()
    {
        Assert.Empty(Diagnose(@"Verse.Log.ErrorOnce(""boom"", 42);", withRimLogging: false));
    }

    [Fact]
    public void SharedKeyMessage_SpellsOutTheArgumentOrder()
    {
        Diagnostic found = Assert.Single(Diagnose(@"Verse.Log.ErrorOnce(""boom"", 42);"));

        // the key moves in front of the message and turns into a string, and nothing else catches that
        Assert.Contains("Log.ErrorOnceTo(channel, key, message)", found.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void Diagnostic_AnchorsOnTheMemberAccess_NotTheWholeCall()
    {
        Diagnostic found = Assert.Single(Diagnose(@"Verse.Log.Error(""boom"");"));

        string flagged = found.Location.SourceTree!.GetText().ToString(found.Location.SourceSpan);
        Assert.Equal("Verse.Log.Error", flagged);
    }

    [Fact]
    public void AliasedRimLoggingLog_WithTheSameMethodName_ReportsNothing()
    {
        // a mod that aliases Log globally writes Log.ErrorOnce for RimLogging's own method, and
        // matching on the name alone would flag every one of those
        string caller = @"
using Log = RimWorks.RimLogging.Log;
class Caller { void Run() { Log.ErrorOnce(""some-key"", ""boom""); } }";

        Assert.Empty(Diagnose(caller, asWholeFile: true));
    }

    [Fact]
    public void AliasedRimLoggingLog_OnTheChannelOverload_ReportsNothing()
    {
        string caller = @"
using Log = RimWorks.RimLogging.Log;
class Caller { void Run() { Log.ErrorOnceTo(""MPF.Worldgen"", ""some-key"", ""boom""); } }";

        Assert.Empty(Diagnose(caller, asWholeFile: true));
    }

    private static ImmutableArray<Diagnostic> Diagnose(
        string statement,
        bool withRimLogging = true,
        bool asWholeFile = false)
    {
        string caller = asWholeFile ? statement : "class Caller { void Run() { " + statement + " } }";
        string[] sources = withRimLogging
            ? new[] { VerseStub, RimLoggingStub, caller }
            : new[] { VerseStub, caller };

        CSharpCompilation compilation = CSharpCompilation.Create(
            "VerseLogAnalyzerTest",
            sources.Select(s => CSharpSyntaxTree.ParseText(s)),
            FrameworkRefs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));

        return compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new VerseLogUsageAnalyzer()))
            .GetAnalyzerDiagnosticsAsync()
            .GetAwaiter()
            .GetResult();
    }
}
