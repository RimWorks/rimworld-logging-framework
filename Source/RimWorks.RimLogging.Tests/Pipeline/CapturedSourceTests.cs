using System;
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
    public void StackCaptureOff_MeansNoSourceEither_SinceTheWalkIsWhatCosts()
    {
        Logging.CaptureStackTraces = false;

        Log.EmitCaptured(LogLevel.Error, "Unity", "boom", "unity stack", callerOnStack: true);

        Assert.False(Assert.Single(_sink.Entries).Source.IsCallerProvided);
    }
}
