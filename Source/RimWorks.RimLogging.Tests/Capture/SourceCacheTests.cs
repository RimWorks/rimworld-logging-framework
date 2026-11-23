using System;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using RimWorks.RimLogging;
using RimWorks.RimLogging.Capture;
using RimWorks.RimLogging.Tests;
using Xunit;

namespace RimLoggingTestsExternal.Capture;

public class SourceCacheTests : LogSinkFixtureBase
{
    private readonly bool _savedCapture = Logging.CaptureStackTraces;

    protected override void OnDispose()
    {
        Logging.CaptureStackTraces = _savedCapture;
        CallerFrameClassifier.HostFrameHook = null;
    }

    public static class Plumbing
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CaptureLive(string text) => Log.EmitCaptured(LogLevel.Info, "Vanilla", text, callerOnStack: true);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LogFromOneCallSite(string text)
        => Log.EmitCaptured(LogLevel.Info, "Vanilla", text, callerOnStack: true);

    [Fact]
    public void TheSecondLineFromOneCallSiteGivesTheSameSourceWithoutReadingFileInfoAgain()
    {
        Logging.CaptureStackTraces = false;
        long[] resolutions = new long[3];

        for (int i = 0; i < 2; i++)
        {
            resolutions[i] = SourceCache.Resolutions;
            LogFromOneCallSite("hello");
        }
        resolutions[2] = SourceCache.Resolutions;

        Assert.Equal(2, _sink.Entries.Count);
        Assert.True(_sink.Entries[0].Source.IsCallerProvided, "the first line should resolve its caller");
        Assert.Equal(nameof(LogFromOneCallSite), _sink.Entries[0].Source.Method);
        Assert.Equal(_sink.Entries[0].Source.File, _sink.Entries[1].Source.File);
        Assert.Equal(_sink.Entries[0].Source.Line, _sink.Entries[1].Source.Line);
        Assert.Equal(_sink.Entries[0].Source.Method, _sink.Entries[1].Source.Method);
        Assert.Equal(0, resolutions[2] - resolutions[1]);
    }

    [Fact]
    public void AFrameWhoseMethodIsADynamicMethodDoesNotThrow()
    {
        Logging.CaptureStackTraces = false;
        CallerFrameClassifier.HostFrameHook = (type, _) => type == typeof(Plumbing).FullName;
        DynamicMethod relay = new DynamicMethod("Relay", typeof(void), new[] { typeof(string) }, typeof(SourceCacheTests).Module);
        ILGenerator il = relay.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(Plumbing).GetMethod(nameof(Plumbing.CaptureLive))!);
        il.Emit(OpCodes.Ret);
        Action<string> call = (Action<string>)relay.CreateDelegate(typeof(Action<string>));

        Exception? thrown = Record.Exception(() => call("hello"));

        Assert.Null(thrown);
        LogEntry entry = Assert.Single(_sink.Entries);
        Assert.True(entry.Source.IsCallerProvided, "the frame below the dynamic method should give the source");
        Assert.Contains(nameof(SourceCacheTests), entry.Source.File);
    }
}
