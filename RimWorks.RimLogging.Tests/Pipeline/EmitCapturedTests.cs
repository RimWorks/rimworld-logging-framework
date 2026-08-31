using System;
using RimWorks.RimLogging;
using RimWorks.RimLogging.Capture;
using RimWorks.RimLogging.Sinks;
using Xunit;

namespace RimWorks.RimLogging.Tests.Pipeline;

public class EmitCapturedTests : IDisposable
{
    private readonly LogLevel _savedMin;
    private readonly bool _savedCapture;
    private readonly MemoryLogSink _sink = new MemoryLogSink();

    public EmitCapturedTests()
    {
        _savedMin = Logging.GlobalMinLevel;
        _savedCapture = Logging.CaptureStackTraces;
        SinkRegistry.DisposeAll();
        SinkRegistry.Register(_sink);
        Logging.GlobalMinLevel = LogLevel.Trace;
    }

    public void Dispose()
    {
        Logging.GlobalMinLevel = _savedMin;
        Logging.CaptureStackTraces = _savedCapture;
        SinkRegistry.Remove(_sink);
        _sink.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void EmitCaptured_PopulatesEntry()
    {
        Log.EmitCaptured(LogLevel.Error, "Unity", "boom", "stack here");

        LogEntry entry = Assert.Single(_sink.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal("Unity", entry.Channel);
        Assert.Equal("boom", entry.RenderedMessage);
        Assert.Equal("boom", entry.MessageTemplate);
        Assert.Equal("stack here", entry.StackTrace);
        Assert.Null(entry.Exception);
        Assert.False(entry.Source.IsCallerProvided);
    }

    [Fact]
    public void EmitCaptured_BelowGlobalMinLevel_IsDropped()
    {
        Logging.GlobalMinLevel = LogLevel.Warn;

        Log.EmitCaptured(LogLevel.Info, "Unity", "ignored", "stack");

        Assert.Empty(_sink.Entries);
    }

    [Fact]
    public void EmitCaptured_NullText_NormalisesToEmpty()
    {
        Log.EmitCaptured(LogLevel.Error, "Unity", null!, "stack");

        LogEntry entry = Assert.Single(_sink.Entries);
        Assert.Equal(string.Empty, entry.RenderedMessage);
        Assert.Equal(string.Empty, entry.MessageTemplate);
    }

    // Regression: captured Verse.Log entries previously routed through Log.Info/Warn/Error,
    // whose [CallerFilePath]/[CallerLineNumber] pinned the source to VerseLogPatches.cs for
    // every modded log line. The hijack now uses the stackTrace-less EmitCaptured overload,
    // which must leave the source empty (the real origin lives in the channel, not a file).
    [Fact]
    public void EmitCaptured_NoStackTrace_CaptureDisabled_LeavesSourceEmptyAndNoStackTrace()
    {
        Logging.CaptureStackTraces = false;

        Log.EmitCaptured(LogLevel.Info, "Mod.Cosmere.Lightweave", "hello");

        LogEntry entry = Assert.Single(_sink.Entries);
        Assert.Equal("Mod.Cosmere.Lightweave", entry.Channel);
        Assert.False(entry.Source.IsCallerProvided);
        Assert.Null(entry.StackTrace);
    }

    [Fact]
    public void EmitCaptured_NoStackTrace_CaptureEnabled_FillsStackTraceFromWalk()
    {
        Logging.CaptureStackTraces = true;

        Log.EmitCaptured(LogLevel.Info, "Mod.Cosmere.Lightweave", "hello");

        LogEntry entry = Assert.Single(_sink.Entries);
        Assert.Equal("Mod.Cosmere.Lightweave", entry.Channel);
        Assert.NotNull(entry.StackTrace);
        Assert.NotEqual(string.Empty, entry.StackTrace);
    }
}
