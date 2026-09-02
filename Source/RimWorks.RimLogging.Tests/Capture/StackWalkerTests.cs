using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using RimWorks.RimLogging;
using RimWorks.RimLogging.Capture;
using Xunit;

// Namespace outside RimWorks.RimLogging.* so this test's frame is not skipped by WalkOnce.
namespace RimLoggingTestsExternal.Capture;

public class StackWalkerTests : System.IDisposable
{
    public void Dispose() => Logging.AttributionProvider = null;

    [Fact]
    public void WalkOnce_FromTest_ReturnsTestMethodName()
    {
        SourceLocation loc = StackWalker.WalkOnce();

        Assert.Equal(nameof(WalkOnce_FromTest_ReturnsTestMethodName), loc.Method);
        Assert.True(loc.IsCallerProvided);
    }

    [Fact]
    public void WalkOnce_FromFrameworkHelper_SkipsFrameworkFrameAndReturnsOuterCaller()
    {
        SourceLocation loc = TestStackWalkerHelper.CallWalker();

        Assert.Equal(
            nameof(WalkOnce_FromFrameworkHelper_SkipsFrameworkFrameAndReturnsOuterCaller),
            loc.Method);
        Assert.True(loc.IsCallerProvided);
    }

    [Fact]
    public void NormalizePath_WindowsModsPath_ProducesShortPath()
    {
        string result = StackWalker.NormalizePath(@"C:\Games\RimWorld\Mods\MyMod\Foo.cs");

        Assert.Equal($"MyMod{System.IO.Path.DirectorySeparatorChar}Foo", result);
    }

    [Fact]
    public void NormalizePath_UnixModsPath_ProducesShortPath()
    {
        string result = StackWalker.NormalizePath("/home/x/RimWorld/Mods/MyMod/Foo.cs");

        Assert.Equal("MyMod/Foo", result);
    }

    [Fact]
    public void NormalizePath_RimworldCosmereDoubleDir_Collapses()
    {
        string result = StackWalker.NormalizePath("/home/x/RimworldCosmere/RimworldCosmere/SomeMod/Bar.cs");

        Assert.Equal("SomeMod/Bar", result);
    }

    [Fact]
    public void NormalizePath_DupSiblingDir_CollapsesOnceMore()
    {
        string result = StackWalker.NormalizePath(@"C:\X\RimWorld\Mods\MyMod\MyMod\file.cs");

        Assert.Equal($"MyMod{System.IO.Path.DirectorySeparatorChar}file", result);
    }

    // regression: Replace(".cs", "") mangled "View.cshtml.cs" into "Viewhtml"
    [Fact]
    public void NormalizePath_DotCsMidString_OnlyStripsTrailingExtension()
    {
        string result = StackWalker.NormalizePath(@"C:\X\RimWorld\Mods\MyMod\View.cshtml.cs");

        Assert.Equal($"MyMod{System.IO.Path.DirectorySeparatorChar}View.cshtml", result);
    }

    [Fact]
    public void FirstCallerFrame_FromHelperInFrameworkNamespace_SkipsHelperAndReturnsTestMethod()
    {
        SourceLocation loc = TestStackWalkerHelper.CallFirstCallerFrame();

        Assert.Equal(
            nameof(FirstCallerFrame_FromHelperInFrameworkNamespace_SkipsHelperAndReturnsTestMethod),
            loc.Method);
        Assert.True(loc.IsCallerProvided);
    }

    // regression: a frame with no file name between us and the caller wiped out Source
    [Fact]
    public void FirstCallerFrame_SkipsReflectionInvokeFramesWithoutFileInfo_AndReturnsRealCaller()
    {
        System.Reflection.MethodInfo m = typeof(TestStackWalkerHelper).GetMethod(
            nameof(TestStackWalkerHelper.CallFirstCallerFrame))!;

        SourceLocation loc = (SourceLocation)m.Invoke(null, null)!;

        Assert.Equal(
            nameof(FirstCallerFrame_SkipsReflectionInvokeFramesWithoutFileInfo_AndReturnsRealCaller),
            loc.Method);
        Assert.True(loc.IsCallerProvided);
    }


    [Fact]
    public void NormalizePath_AssemblyAnchored_DropsAsmPrefix_AndReturnsRelativePath()
    {
        // The asm name anchors the cut, but is not included in the output. The channel column
        // already identifies the mod, so repeating it in the source path is pure noise.
        System.Reflection.Assembly asm = typeof(StackWalkerTests).Assembly;
        string asmName = asm.GetName().Name!;
        string file = $"/home/dev/anywhere/{asmName}/Capture/SomeFile.cs";

        string result = StackWalker.NormalizePath(file, typeof(StackWalkerTests));

        Assert.Equal("Capture/SomeFile", result);
    }

    [Fact]
    public void NormalizePath_AssemblyAnchored_NormalisesWindowsInputToOsSeparator()
    {
        System.Reflection.Assembly asm = typeof(StackWalkerTests).Assembly;
        string asmName = asm.GetName().Name!;
        string file = $@"C:\dev\stuff\{asmName}\Capture\WinFile.cs";

        string result = StackWalker.NormalizePath(file, typeof(StackWalkerTests));

        char sep = System.IO.Path.DirectorySeparatorChar;
        Assert.Equal($"Capture{sep}WinFile", result);
    }

    [Fact]
    public void NormalizePath_NoTypeProvided_FallsBackToLegacyRegex()
    {
        // Without a declaring type the assembly-anchor branch is skipped entirely, so a path
        // matching the legacy /RimWorld/Mods/<modFolder>/ shape must still normalise the old way.
        string result = StackWalker.NormalizePath("/home/x/RimWorld/Mods/Legacy/Sub/File.cs");

        Assert.Equal("Legacy/Sub/File", result);
    }

    [Fact]
    public void NormalizePath_AssemblyAnchorMisses_FallsBackToLegacyRegex()
    {
        // Declaring type is supplied but its assembly name doesn't appear in the source path,
        // so the assembly-anchor branch yields no anchor and we drop to the regex fallback.
        string result = StackWalker.NormalizePath(
            "/home/x/RimWorld/Mods/Legacy/Sub/File.cs",
            typeof(StackWalkerTests));

        Assert.Equal("Legacy/Sub/File", result);
    }

    [Fact]
    public void NormalizePath_EmptyInput_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, StackWalker.NormalizePath(string.Empty));
        Assert.Equal(string.Empty, StackWalker.NormalizePath(string.Empty, typeof(StackWalkerTests)));
    }

    [Fact]
    public void NormalizePath_RepeatCalls_ReturnSameCachedString()
    {
        // uses the regex fallback so it cannot collide with another test's cache entry
        string input = "/home/x/RimWorld/Mods/CacheTestMod/Foo.cs";

        string first = StackWalker.NormalizePath(input);
        string second = StackWalker.NormalizePath(input);

        Assert.Equal(first, second);
        Assert.Equal("CacheTestMod/Foo", first);
    }


    [Fact]
    public void NormalizePath_NoType_ScansLoadedAssembliesAndAnchorsByName()
    {
        // no Type given, so this resolves by finding the loaded assembly's name in the path
        System.Reflection.Assembly asm = typeof(StackWalkerTests).Assembly;
        string asmName = asm.GetName().Name!;
        string file = $"/home/dev/external/{asmName}/Scanned/Sample.cs";

        string result = StackWalker.NormalizePath(file);

        Assert.Equal("Scanned/Sample", result);
    }


    [Fact]
    public void NormalizePath_AssemblyAnchored_AcceptsPrefixSegment_UnixPath()
    {
        // project "RimObs.Library" builds assembly "RimObs", so anchor on the prefix segment
        System.Reflection.Assembly asm = typeof(StackWalkerTests).Assembly;
        string asmName = asm.GetName().Name!;
        string file = $"/home/dev/proj/{asmName}.Library/Bootstrap/Sample.cs";

        string result = StackWalker.NormalizePath(file, typeof(StackWalkerTests));

        Assert.Equal("Bootstrap/Sample", result);
    }

    [Fact]
    public void NormalizePath_AssemblyAnchored_AcceptsPrefixSegment_WindowsPath()
    {
        System.Reflection.Assembly asm = typeof(StackWalkerTests).Assembly;
        string asmName = asm.GetName().Name!;
        string file = $@"C:\dev\proj\{asmName}.Core\Bootstrap\Win.cs";

        string result = StackWalker.NormalizePath(file, typeof(StackWalkerTests));

        char sep = System.IO.Path.DirectorySeparatorChar;
        Assert.Equal($"Bootstrap{sep}Win", result);
    }


    [Fact]
    public void NormalizePath_AssemblyAnchored_StripsLeadingSourceSegment_UnixPath()
    {
        // "<root>/Source/<rest>" is a common layout, and Source/ carries nothing for the reader
        System.Reflection.Assembly asm = typeof(StackWalkerTests).Assembly;
        string asmName = asm.GetName().Name!;
        string file = $"/home/dev/proj/{asmName}/Source/Profiling/Utility/Foo.cs";

        string result = StackWalker.NormalizePath(file, typeof(StackWalkerTests));

        Assert.Equal("Profiling/Utility/Foo", result);
    }

    [Fact]
    public void NormalizePath_AssemblyAnchored_StripsLeadingSourceSegment_WindowsPath()
    {
        System.Reflection.Assembly asm = typeof(StackWalkerTests).Assembly;
        string asmName = asm.GetName().Name!;
        string file = $@"C:\dev\proj\{asmName}\Source\Profiling\Utility\Bar.cs";

        string result = StackWalker.NormalizePath(file, typeof(StackWalkerTests));

        char sep = System.IO.Path.DirectorySeparatorChar;
        Assert.Equal($"Profiling{sep}Utility{sep}Bar", result);
    }


    [Fact]
    public void NormalizePath_AssemblyAnchored_StripsSubProjectAndSourcePair_UnixPath()
    {
        // anchoring gives "Framework/Source/Fonts/FontLoader.cs", and both leading segments go
        System.Reflection.Assembly asm = typeof(StackWalkerTests).Assembly;
        string asmName = asm.GetName().Name!;
        string file = $"/home/dev/proj/{asmName}/Framework/Source/Fonts/FontLoader.cs";

        string result = StackWalker.NormalizePath(file, typeof(StackWalkerTests));

        Assert.Equal("Fonts/FontLoader", result);
    }

    [Fact]
    public void NormalizePath_LegacyRegex_StripsSubProjectAndSourcePair()
    {
        // regex path: after /Mods/ the mod folder and "Source" both drop off
        string result = StackWalker.NormalizePath(
            "/home/x/RimWorld/Mods/Dubs-Performance-Analyzer/Source/Profiling/Utility/ThreadSafeLogger.cs");

        Assert.Equal("Profiling/Utility/ThreadSafeLogger", result);
    }

    [Fact]
    public void FirstCallerType_FromFrameworkHelper_SkipsFrameworkFrame_AndReturnsOuterCallerType()
    {
        // Confirms the cheap walk used by Log.ResolveSource finds the caller type after
        // skipping RimWorks.RimLogging.* frames.
        System.Type? t = TestStackWalkerHelper.CallFirstCallerType();

        Assert.Equal(typeof(StackWalkerTests), t);
    }


    [Fact]
    public void FormatTrace_SkipsRimLoggingFrames_AndReturnsOuterCaller()
    {
        // Regression: FormatTrace and FirstCallerFrame now share CallerFrameClassifier.IsInternalFrame.
        // The trace must omit RimWorks.RimLogging.* frames and include the test method.
        System.Diagnostics.StackTrace st = TestStackWalkerHelper.CallStackTrace();

        string formatted = RimWorks.RimLogging.Capture.StackWalker.FormatTrace(st);

        Assert.DoesNotContain("RimWorks.RimLogging.", formatted);
        Assert.Contains(nameof(FormatTrace_SkipsRimLoggingFrames_AndReturnsOuterCaller), formatted);
    }

    [Fact]
    public void FormatTrace_out_NoProviderInstalled_PatchedByIsEmpty()
    {
        Logging.AttributionProvider = null;
        System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(0, true);

        StackWalker.FormatTrace(st, out IReadOnlyList<string>? patchedBy);

        // with no provider the answer is a definite "nothing", not "could not tell"
        Assert.NotNull(patchedBy);
        Assert.Empty(patchedBy!);
    }

    [Fact]
    public void FormatTrace_out_ProviderOwnsAFrameInTheWalk_ReturnsThatOwner()
    {
        MethodBase here = MethodBase.GetCurrentMethod()!;
        Logging.AttributionProvider = f => ReferenceEquals(f.GetMethod(), here) ? ["some.mod"] : System.Array.Empty<string>();
        System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(0, true);

        StackWalker.FormatTrace(st, out IReadOnlyList<string>? patchedBy);

        Assert.Equal(["some.mod"], patchedBy);
    }

    [Fact]
    public void FormatTrace_out_ProviderReportsTheSameOwnerTwice_DedupesIt()
    {
        Logging.AttributionProvider = _ => ["dup.mod", "dup.mod"];
        System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(0, true);

        StackWalker.FormatTrace(st, out IReadOnlyList<string>? patchedBy);

        Assert.Equal(["dup.mod"], patchedBy);
    }

    [Fact]
    public void FormatTrace_out_ProviderThrows_PatchedByIsUnavailable()
    {
        Logging.AttributionProvider = _ => throw new System.InvalidOperationException("backend blew up");
        System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(0, true);

        string formatted = StackWalker.FormatTrace(st, out IReadOnlyList<string>? patchedBy);

        // unavailable, not empty: an entry must not claim a method is unpatched when the
        // backend never managed to answer. the frames still reach the formatted trace.
        Assert.Null(patchedBy);
        Assert.Contains(nameof(FormatTrace_out_ProviderThrows_PatchedByIsUnavailable), formatted);
    }

    // a Harmony replacement DynamicMethod frame on Mono has a null GetMethod(); the provider
    // must still be queried with the frame itself so Harmony's native-address fallback can run.
    private sealed class NullMethodStackFrame : System.Diagnostics.StackFrame
    {
        public override MethodBase GetMethod() => null!;
    }

    [Fact]
    public void FormatTrace_out_FrameWithNullGetMethod_StillReachesTheProvider()
    {
        Logging.AttributionProvider = f => f.GetMethod() == null ? ["mono.mod"] : System.Array.Empty<string>();
        System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(new NullMethodStackFrame());

        StackWalker.FormatTrace(st, out IReadOnlyList<string>? patchedBy);

        Assert.Equal(["mono.mod"], patchedBy);
    }
}
