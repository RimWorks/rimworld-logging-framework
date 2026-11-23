using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using RimWorks.RimLogging;
using RimWorks.RimLogging.Capture;
using RimWorks.RimLogging.Tests;
using Xunit;

// Namespace outside RimWorks.RimLogging.* so this test's frame is not skipped by the caller walk.
namespace RimLoggingTestsExternal.Pipeline;

/// <summary>
/// Guards source resolution when the caller hands us a stack trace string. Unity's bridge
/// supplies one, and skipping the walk for it left every bridged entry reading "(no source)".
/// </summary>
public class CapturedSourceTests : LogSinkFixtureBase
{
    private readonly bool _savedCapture = Logging.CaptureStackTraces;

    private const string AcmeCaller = "Forward";

    protected override void OnDispose()
    {
        Logging.CaptureStackTraces = _savedCapture;
        StackWalker.FirstCallerEndsWithoutFile = false;
        CallerFrameClassifier.HostFrameHook = null;
    }

    public static class HostLogPlumbing
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Capture(string text) => Log.EmitCaptured(LogLevel.Error, "Vanilla", text);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CaptureLive(string text) => Log.EmitCaptured(LogLevel.Info, "Vanilla", text, callerOnStack: true);
    }

    private static void EndWalksAtAFilelessCaller()
    {
        Logging.CaptureStackTraces = true;
        StackWalker.FirstCallerEndsWithoutFile = true;
        CallerFrameClassifier.HostFrameHook = (type, _) => type == typeof(HostLogPlumbing).FullName;
    }

    private static Action<string> AcmeCallerWithoutFile()
    {
        AssemblyBuilder asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Acme.Host"), AssemblyBuilderAccess.Run);
        TypeBuilder type = asm.DefineDynamicModule("Acme.Host")
            .DefineType("Acme.Host.Bridge", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
        MethodBuilder forward = type.DefineMethod(AcmeCaller, MethodAttributes.Public | MethodAttributes.Static,
            typeof(void), new[] { typeof(string) });
        forward.SetImplementationFlags(MethodImplAttributes.NoInlining);
        ILGenerator il = forward.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(HostLogPlumbing).GetMethod(nameof(HostLogPlumbing.Capture))!);
        il.Emit(OpCodes.Ret);
        return type.CreateType().GetMethod(AcmeCaller)!.CreateDelegate<Action<string>>();
    }

    [Fact]
    public void WithTheHostSwitchOn_ACallerWithoutAFileStaysTheSource_NotTheFramedOneBelowIt()
    {
        EndWalksAtAFilelessCaller();

        AcmeCallerWithoutFile()("boom");

        LogEntry entry = Assert.Single(_sink.Entries);
        Assert.Equal(string.Empty, entry.Source.File);
        Assert.Equal(0, entry.Source.Line);
        Assert.Equal(AcmeCaller, entry.Source.Method);
        Assert.Contains(nameof(WithTheHostSwitchOn_ACallerWithoutAFileStaysTheSource_NotTheFramedOneBelowIt), entry.StackTrace);
    }

    [Fact]
    public void WithTheHostSwitchOn_ATraceInTheMessageStillGivesTheThrowSite()
    {
        EndWalksAtAFilelessCaller();
        string text = "System.InvalidOperationException: boom\n"
            + "  at Some.Mod.Thing.Explode () [0x00000] in /mods/Thing/Source/Thing.cs:15 ";

        AcmeCallerWithoutFile()(text);

        LogEntry entry = Assert.Single(_sink.Entries);
        Assert.True(entry.Source.IsCallerProvided, "the throw site in the text should fill the source");
        Assert.Equal(15, entry.Source.Line);
        Assert.Equal("Explode", entry.Source.Method);
    }

    [Fact]
    public void ALiveCaptureWithASuppliedTraceStillResolvesASource()
    {
        Log.EmitCaptured(LogLevel.Error, "Unity", "boom", "unity stack", callerOnStack: true);

        LogEntry entry = Assert.Single(_sink.Entries);
        Assert.True(entry.Source.IsCallerProvided, "a live capture should resolve its caller");
        Assert.Equal(nameof(ALiveCaptureWithASuppliedTraceStillResolvesASource), entry.Source.Method);
    }

    [Fact]
    public void TheSuppliedTraceIsStillWhatGetsShown()
    {
        // the walk is only for the source; Unity's own string stays the displayed trace
        Log.EmitCaptured(LogLevel.Error, "Unity", "boom", "unity stack", callerOnStack: true);

        Assert.Equal("unity stack", Assert.Single(_sink.Entries).StackTrace);
    }

    [Fact]
    public void AReplayedCaptureResolvesNothing_BecauseItsCallerIsLongGone()
    {
        // VerseLogBackfill replays messages logged before the hijack installed, so the live
        // stack belongs to the drain, not to whoever wrote the line
        Log.EmitCaptured(LogLevel.Error, "Vanilla", "boom", "old stack");

        Assert.False(Assert.Single(_sink.Entries).Source.IsCallerProvided);
    }

    [Fact]
    public void AVerseCapturedExceptionResolvesItsThrowSiteFromTheMessage()
    {
        // Verse logs the exception's ToString() as the message, and our own walked trace holds
        // only symbol-free vanilla frames, so the message is the one carrying real file info
        string text = "Exception filling window for LudeonTK.Dialog_Debug: System.InvalidOperationException: boom\n"
            + "  at Some.Mod.Thing.Explode () [0x00000] in /mods/Thing/Source/Thing.cs:15 \n"
            + "  at Verse.Window.InnerWindowOnGUI (System.Int32 x) [0x1] in <b4d967f0>:0 ";

        // the supplied trace stands in for the vanilla frames the walk actually lands on: real
        // frames, no symbols. it is what "captured" holds, and it is why text has to be read too
        Log.EmitCaptured(LogLevel.Error, "Vanilla", text,
            "  at Verse.Window.InnerWindowOnGUI (System.Int32 x) [0x1] in <b4d967f0>:0 ");

        LogEntry entry = Assert.Single(_sink.Entries);
        Assert.True(entry.Source.IsCallerProvided, "the throw site should fill an empty source");
        Assert.Equal(15, entry.Source.Line);
        Assert.Equal("Explode", entry.Source.Method);
    }

    [Fact]
    public void StackCaptureOff_ALiveCaptureStillResolvesItsSource()
    {
        Logging.CaptureStackTraces = false;

        Log.EmitCaptured(LogLevel.Error, "Unity", "boom", "unity stack", callerOnStack: true);

        LogEntry entry = Assert.Single(_sink.Entries);
        Assert.True(entry.Source.IsCallerProvided, "a live capture should resolve its caller without a stored stack");
        Assert.Equal(nameof(StackCaptureOff_ALiveCaptureStillResolvesItsSource), entry.Source.Method);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LogLiveFromHere(string text)
        => Log.EmitCaptured(LogLevel.Info, "Vanilla", text, callerOnStack: true);

    [Fact]
    public void WithCaptureOff_ALiveLineGetsTheFileAndLineOfTheFirstFrameThatHasAFile()
    {
        foreach (bool capture in new[] { true, false })
        {
            Logging.CaptureStackTraces = capture;
            LogLiveFromHere("hello");
        }

        Assert.Equal(2, _sink.Entries.Count);
        SourceLocation walked = _sink.Entries[0].Source;
        SourceLocation looked = _sink.Entries[1].Source;
        Assert.True(walked.IsCallerProvided, "the full walk should find the caller");
        Assert.Equal(nameof(LogLiveFromHere), walked.Method);
        Assert.Equal(walked.File, looked.File);
        Assert.Equal(walked.Line, looked.Line);
        Assert.Equal(walked.Method, looked.Method);
        Assert.Null(_sink.Entries[1].StackTrace);
    }

    private static Action<string> AcmeChainWithoutFiles(MethodInfo target)
    {
        AssemblyBuilder asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Acme.Chain"), AssemblyBuilderAccess.Run);
        TypeBuilder type = asm.DefineDynamicModule("Acme.Chain")
            .DefineType("Acme.Chain.Relay", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
        MethodBuilder inner = type.DefineMethod("Inner", MethodAttributes.Public | MethodAttributes.Static,
            typeof(void), new[] { typeof(string) });
        inner.SetImplementationFlags(MethodImplAttributes.NoInlining);
        ILGenerator il = inner.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, target);
        il.Emit(OpCodes.Ret);
        MethodBuilder outer = type.DefineMethod("Outer", MethodAttributes.Public | MethodAttributes.Static,
            typeof(void), new[] { typeof(string) });
        outer.SetImplementationFlags(MethodImplAttributes.NoInlining);
        il = outer.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, inner);
        il.Emit(OpCodes.Ret);
        return type.CreateType().GetMethod("Outer")!.CreateDelegate<Action<string>>();
    }

    [Fact]
    public void WithCaptureOff_ACallerTwoFramesBelowFramesWithoutAFileGivesItsOwnFileAndLine()
    {
        CallerFrameClassifier.HostFrameHook = (type, _) => type == typeof(HostLogPlumbing).FullName;
        Action<string> relay = AcmeChainWithoutFiles(typeof(HostLogPlumbing).GetMethod(nameof(HostLogPlumbing.CaptureLive))!);

        foreach (bool capture in new[] { true, false })
        {
            Logging.CaptureStackTraces = capture;
            relay("hello");
        }

        Assert.Equal(2, _sink.Entries.Count);
        SourceLocation walked = _sink.Entries[0].Source;
        SourceLocation looked = _sink.Entries[1].Source;
        Assert.Equal(nameof(WithCaptureOff_ACallerTwoFramesBelowFramesWithoutAFileGivesItsOwnFileAndLine), looked.Method);
        Assert.True(looked.IsCallerProvided, "the caller below the fileless frames should give its file and line");
        Assert.Contains(nameof(CapturedSourceTests), looked.File);
        Assert.Equal(walked.File, looked.File);
        Assert.Equal(walked.Line, looked.Line);
    }

    public static class HookPlumbing
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Capture(string text)
        {
            System.Diagnostics.StackTrace walk = TakeWalk();
            Log.EmitCaptured(LogLevel.Info, "Vanilla", text, callerOnStack: true, callerWalk: walk);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static System.Diagnostics.StackTrace TakeWalk() => new System.Diagnostics.StackTrace(2, false);
    }

    [Fact]
    public void WithCaptureOff_ALiveLineResolvesItsSourceFromTheWalkTheHookAlreadyTook()
    {
        CallerFrameClassifier.HostFrameHook = (type, _) => type == typeof(HookPlumbing).FullName;

        foreach (bool capture in new[] { true, false })
        {
            Logging.CaptureStackTraces = capture;
            HookPlumbing.Capture("hello");
        }

        Assert.Equal(2, _sink.Entries.Count);
        SourceLocation walked = _sink.Entries[0].Source;
        SourceLocation looked = _sink.Entries[1].Source;
        Assert.Equal(nameof(WithCaptureOff_ALiveLineResolvesItsSourceFromTheWalkTheHookAlreadyTook), looked.Method);
        Assert.True(looked.IsCallerProvided, "the handed walk should resolve the caller");
        Assert.Equal(walked.File, looked.File);
        Assert.Equal(walked.Line, looked.Line);
    }

    [Fact]
    public void WithCaptureOff_AReplayedLineWhoseStoredTraceIsEmptyHasAnEmptySource()
    {
        Logging.CaptureStackTraces = false;

        Log.EmitCaptured(LogLevel.Info, "Vanilla", "hello", string.Empty, callerOnStack: false);

        LogEntry entry = Assert.Single(_sink.Entries);
        Assert.Equal(string.Empty, entry.Source.File);
        Assert.Equal(0, entry.Source.Line);
        Assert.Null(entry.Source.Method);
    }

    [Fact]
    public void WithCaptureOff_ALiveLineReportsPatchedByAsUnavailable()
    {
        Logging.CaptureStackTraces = false;
        LogLiveFromHere("captured");
        Log.Info("direct");

        Logging.CaptureStackTraces = true;
        LogLiveFromHere("captured");
        Log.Info("direct");

        Assert.Equal(4, _sink.Entries.Count);
        Assert.Null(_sink.Entries[0].PatchedBy);
        Assert.Null(_sink.Entries[1].PatchedBy);
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<string>>(_sink.Entries[2].PatchedBy));
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<string>>(_sink.Entries[3].PatchedBy));
    }

    [Fact]
    public void WithCaptureOff_ABridgeLineAndAReplayedLineKeepAnEmptyPatchedBy()
    {
        Logging.CaptureStackTraces = false;

        Log.EmitCaptured(LogLevel.Error, "Unity", "boom", "unity stack", callerOnStack: true);
        Log.EmitCaptured(LogLevel.Error, "Vanilla", "boom", "old stack", callerOnStack: false);

        Assert.Equal(2, _sink.Entries.Count);
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<string>>(_sink.Entries[0].PatchedBy));
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<string>>(_sink.Entries[1].PatchedBy));
    }
}
