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

/// <summary>
/// Guards RIMLOG003. Log.Info(channel, message) silently binds to the params overload, so the
/// channel is logged as the message and the message is dropped. It shipped in EarlyInit once.
/// </summary>
public class ChannelArgumentAnalyzerTests
{
    private static readonly MetadataReference[] FrameworkRefs =
    {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(Path.Combine(
            Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Runtime.dll")),
    };

    // mirrors the real overload pairs: a params default-channel one next to a To sibling
    private const string RimLoggingStub = @"
namespace RimWorks.RimLogging
{
    public static class Log
    {
        public static void Info(string template, params object?[] args) { }
        public static void InfoTo(string channel, string template, object?[]? args = null) { }
        public static void Warn(string template, params object?[] args) { }
        public static void WarnTo(string channel, string template, object?[]? args = null) { }
        public static void Special(string template, params object?[] args) { }
    }
}";

    [Fact]
    public void ChannelThenMessage_IsTheBugThatShippedInEarlyInit()
    {
        ImmutableArray<Diagnostic> found = Diagnose(
            @"RimWorks.RimLogging.Log.Info(""RimWorks.RimLogging"", ""RimLogging initialized"");");

        Assert.Equal(ChannelArgumentAnalyzer.ChannelAsMessageRuleId, Assert.Single(found).Id);
    }

    [Fact]
    public void TheMessageNamesTheSiblingToCallInstead()
    {
        Diagnostic found = Assert.Single(Diagnose(
            @"RimWorks.RimLogging.Log.Warn(""Cosmere.Roshar"", ""stormlight low"");"));

        Assert.Contains("Log.WarnTo", found.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void ARealTemplate_WithSpacesAndAPlaceholder_ReportsNothing()
    {
        Assert.Empty(Diagnose(@"RimWorks.RimLogging.Log.Info(""loaded {0} defs"", 12);"));
    }

    [Fact]
    public void ATemplateEndingInASentenceDot_IsNotAChannel()
    {
        // "Something." is dotted but a trailing dot reads as prose, not a channel segment
        Assert.Empty(Diagnose(@"RimWorks.RimLogging.Log.Info(""Loading."", 12);"));
    }

    [Fact]
    public void AMessageWithNoDot_ReportsNothing()
    {
        Assert.Empty(Diagnose(@"RimWorks.RimLogging.Log.Info(""started"", 12);"));
    }

    [Fact]
    public void TheCorrectCall_ReportsNothing()
    {
        Assert.Empty(Diagnose(
            @"RimWorks.RimLogging.Log.InfoTo(""RimWorks.RimLogging"", ""RimLogging initialized"");"));
    }

    [Fact]
    public void ASingleArgument_IsJustAMessage_ReportsNothing()
    {
        Assert.Empty(Diagnose(@"RimWorks.RimLogging.Log.Info(""RimWorks.RimLogging"");"));
    }

    [Fact]
    public void MoreThanTwoArguments_ReportsNothing()
    {
        // InfoTo takes object?[]? not params, so the fix would not compile for these
        Assert.Empty(Diagnose(
            @"RimWorks.RimLogging.Log.Info(""A.B"", ""x"", ""y"");"));
    }

    [Fact]
    public void ALevelWithNoToSibling_ReportsNothing()
    {
        Assert.Empty(Diagnose(@"RimWorks.RimLogging.Log.Special(""A.B"", ""x"");"));
    }

    [Fact]
    public void ANamedArgument_WasChosenOnPurpose_ReportsNothing()
    {
        Assert.Empty(Diagnose(
            @"RimWorks.RimLogging.Log.Info(template: ""A.B"", args: ""x"");"));
    }

    [Fact]
    public void AnAliasedLog_IsStillFlagged()
    {
        string caller = @"
using Log = RimWorks.RimLogging.Log;
class Caller { void Run() { Log.Info(""A.B"", ""x""); } }";

        Assert.Equal(ChannelArgumentAnalyzer.ChannelAsMessageRuleId,
            Assert.Single(Diagnose(caller, asWholeFile: true)).Id);
    }

    [Fact]
    public void Diagnostic_AnchorsOnTheMemberAccess_NotTheWholeCall()
    {
        Diagnostic found = Assert.Single(Diagnose(
            @"RimWorks.RimLogging.Log.Info(""A.B"", ""x"");"));

        string flagged = found.Location.SourceTree!.GetText().ToString(found.Location.SourceSpan);
        Assert.Equal("RimWorks.RimLogging.Log.Info", flagged);
    }

    private static ImmutableArray<Diagnostic> Diagnose(string statement, bool asWholeFile = false)
    {
        string caller = asWholeFile ? statement : "class Caller { void Run() { " + statement + " } }";

        CSharpCompilation compilation = CSharpCompilation.Create(
            "ChannelArgumentAnalyzerTest",
            new[] { RimLoggingStub, caller }.Select(s => CSharpSyntaxTree.ParseText(s)),
            FrameworkRefs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));

        return compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new ChannelArgumentAnalyzer()))
            .GetAnalyzerDiagnosticsAsync()
            .GetAwaiter()
            .GetResult();
    }
}
