using RimWorks.RimLogging.Capture;
using Xunit;

// Namespace outside RimWorks.RimLogging.* so this test's frame is not skipped by WalkOnce.
namespace RimLoggingTestsExternal.Capture;

public class StackWalkerTests
{
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

    // Regression: NormalizePath used to call Replace(".cs", "") which strips the substring
    // anywhere in the path, not just the trailing extension. A file whose name contains ".cs"
    // mid-string (e.g. a Razor-style "View.cshtml.cs") was mangled to "Viewhtml". Only the
    // trailing ".cs" extension must be removed.
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

    // Regression: prior FirstCallerFrame bailed on the first non-RimLogging frame whose
    // GetFileName() returned null. In production this meant any vanilla Verse/Unity frame
    // (or the Harmony-emitted dynamic stub) sitting between the framework and the real
    // caller wiped out the Source field. Reflection.Invoke injects similar PDB-less frames
    // (System.RuntimeMethodHandle / RuntimeMethodInfo.UnsafeInvokeInternal) into the
    // stack, so this drives the same scenario: the call chain has frames-without-files
    // between the framework-internal helper and the real test caller. The fix is for
    // FirstCallerFrame to walk past those instead of returning Empty.
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
        // The path cache should be transparent: same input -> same output, byte-identical.
        // We use a path that exercises the regex fallback so it doesn't conflict with the
        // assembly-anchored cache entry from other tests.
        string input = "/home/x/RimWorld/Mods/CacheTestMod/Foo.cs";

        string first = StackWalker.NormalizePath(input);
        string second = StackWalker.NormalizePath(input);

        Assert.Equal(first, second);
        Assert.Equal("CacheTestMod/Foo", first);
    }


    [Fact]
    public void NormalizePath_NoType_ScansLoadedAssembliesAndAnchorsByName()
    {
        // The test assembly is loaded in the AppDomain, so a path containing its simple name
        // as a segment is resolvable even without an explicit Type. This is the path used when
        // [CallerFilePath] supplies the file but no caller Type is available. Output is still
        // the asm-relative path -- no asm prefix.
        System.Reflection.Assembly asm = typeof(StackWalkerTests).Assembly;
        string asmName = asm.GetName().Name!;
        string file = $"/home/dev/external/{asmName}/Scanned/Sample.cs";

        string result = StackWalker.NormalizePath(file);

        Assert.Equal("Scanned/Sample", result);
    }


    [Fact]
    public void NormalizePath_AssemblyAnchored_AcceptsPrefixSegment_UnixPath()
    {
        // Repro of the RimObs case: project folder "RimObs.Library" produces assembly "RimObs".
        // The exact segment "/RimObs/" isn't in the source path but "/RimObs.Library/" is, and
        // we should anchor on it so the embedded source path collapses correctly.
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
        // Common project layout (Dubs Performance Analyzer, Lightweave subprojects, etc.):
        // sources live under <projectRoot>/Source/<rest>. The "Source" segment is a developer
        // convention with no reader value, so it should be stripped from the rendered path.
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
        // Lightweave-style layout: a top-level mod asm "Lightweave" anchors on /Lightweave/,
        // giving rel "Framework/Source/Fonts/FontLoader.cs". The "Framework/Source/" pair is
        // a sub-project + dev convention with no signal for the reader, so we strip both.
        System.Reflection.Assembly asm = typeof(StackWalkerTests).Assembly;
        string asmName = asm.GetName().Name!;
        string file = $"/home/dev/proj/{asmName}/Framework/Source/Fonts/FontLoader.cs";

        string result = StackWalker.NormalizePath(file, typeof(StackWalkerTests));

        Assert.Equal("Fonts/FontLoader", result);
    }

    [Fact]
    public void NormalizePath_LegacyRegex_StripsSubProjectAndSourcePair()
    {
        // Dubs-Performance-Analyzer-style layout falling through the legacy regex path: after
        // stripping /Mods/, the first segment is the mod folder and the next is "Source". Both
        // should disappear so the rendered path is the actual code location.
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
}
