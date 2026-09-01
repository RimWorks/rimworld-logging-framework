using System;
using System.Runtime.CompilerServices;
using RimWorks.RimLogging;
using RimWorks.RimLogging.Format;
using Xunit;

namespace RimWorks.RimLogging.Tests;

public class LogInfoTests : LogSinkFixtureBase
{
    // Overload 1: params. Must pass an explicit array to avoid overload 4 winning.
    // Log.Info("template", "alice") resolves to overload 4 (channel, template) in C#.
    // To exercise overload 1 (default channel + params), pass no args or pass args as array.
    [Fact]
    public void Info_DefaultChannelTemplateArgs_PopulatesEntry()
    {
        // Overload 1 (params) is reachable with zero args or by casting to avoid overload-4 win.
        // With zero args, the params array is empty and channel defaults to "default".
        Log.Info("user-info-test-A-sentinel");

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal("default", entry!.Channel);
        Assert.Equal(LogLevel.Info, entry.Level);
        Assert.Equal("user-info-test-A-sentinel", entry.RenderedMessage);
    }

    [Fact]
    public void Info_DefaultChannelTemplateArgs_WithParamsArray_PopulatesContext()
    {
        // Exercise overload 1 with an explicit object?[] to force params path, not overload-4.
        object?[] args = ["alice"];
        Log.Info("user {Name} did thing test-A2", args);

        // Note: Log.Info(string, object?[]) resolves to overload 2 (template+array+caller-info).
        // Overload 2 uses CallerLineNumber/CallerFilePath so Source.IsCallerProvided == true.
        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal("default", entry!.Channel);
        Assert.Equal("user alice did thing test-A2", entry.RenderedMessage);
        Assert.NotNull(entry.Context);
        Assert.Equal("alice", entry.Context!["Name"]);
    }

    [Fact]
    public void Info_DefaultChannelMessageContext_CapturesAnonymousProps()
    {
        // Overload 3: (string message, object context, ...)
        // Requires the second arg to not be string/object?[] to pick this overload.
        Log.Info("msg-test-B", new { a = 1, b = "x" });

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal("default", entry!.Channel);
        Assert.Equal("msg-test-B", entry.RenderedMessage);
        Assert.NotNull(entry.Context);
        Assert.Equal(1, entry.Context!["a"]);
        Assert.Equal("x", entry.Context["b"]);
    }

    [Fact]
    public void Info_ExplicitChannel_RoutesChannelUnchanged()
    {
        // Overload 4: (string channel, string template, object?[]? args = null, ...)
        Log.Info("audit", "hello {Who} test-C", new object?[] { "world" });

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal("audit", entry!.Channel);
        Assert.Equal("hello world test-C", entry.RenderedMessage);
    }

    [Fact]
    public void Info_CallerInfo_PopulatesSourceLocation()
    {
        // Overload 2 requires explicit named args to bypass overload-1 (params) which wins
        // when an object?[] is passed without named line/file args. Provide explicit values
        // to verify EmitInternal routes them into SourceLocation correctly.
        Log.Info("caller-test-D template {X}", new object?[] { 42 }, line: 77, file: "/proj/Foo.cs");

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.True(entry!.Source.IsCallerProvided);
        Assert.Equal(77, entry.Source.Line);
        // Caller-info paths now share StackWalker.NormalizePath: a path outside the RimWorld
        // layout keeps its relative dir structure (only the leading slash + trailing .cs go).
        Assert.Equal("proj/Foo", entry.Source.File);
    }

    [Fact]
    public void Info_BelowGlobalMinLevel_IsDropped()
    {
        Logging.GlobalMinLevel = LogLevel.Warn;
        int countBefore = _sink.Entries.Count;

        Log.Info("dropped message test-E");

        Assert.Equal(countBefore, _sink.Entries.Count);
    }

    [Fact]
    public void Info_TemplateParseCached_SecondCallReturnsSameInstance()
    {
        string template = "unique-template-for-cache-test-F {Val}";

        MessageTemplate first = TemplateCache.Get(template);
        MessageTemplate second = TemplateCache.Get(template);
        Assert.Same(first, second);
    }

    [Fact]
    public void Info_NullTemplate_NormalisesToEmpty()
    {
        // Overload 1 (params) with null template: no NRE expected.
        Exception? thrown = Record.Exception(() => Log.Info((string)null!));

        Assert.Null(thrown);
        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal(string.Empty, entry!.MessageTemplate);
        Assert.Equal(string.Empty, entry.RenderedMessage);
    }

    [Fact]
    public void Info_NoArgsNoContext_RendersTemplateLiteral()
    {
        // Overload 1 (params) with no args: rendered == template, context == null.
        Log.Info("literal message test-H");

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal("literal message test-H", entry!.RenderedMessage);
        Assert.Null(entry.Context);
    }

    [Fact]
    public void Info_MoreArgsThanHoles_DoesNotThrow()
    {
        // Overload 4 with extra args array: MessageTemplate.Render drops extras.
        Exception? thrown = Record.Exception(() =>
            Log.Info("default", "hello {Name} test-I", new object?[] { "alice", "extra" }));

        Assert.Null(thrown);
        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal("hello alice test-I", entry!.RenderedMessage);
    }

    [Fact]
    public void Info_FewerArgsThanHoles_DoesNotThrow()
    {
        // Overload 4 with fewer args than holes: unmatched holes are left as-is.
        Exception? thrown = Record.Exception(() =>
            Log.Info("default", "hi {A} {B} test-J", new object?[] { "only-a" }));

        Assert.Null(thrown);
        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal("hi only-a {B} test-J", entry!.RenderedMessage);
    }

    [Fact]
    public void Info_EmitCapturedThroughDispatchSync()
    {
        Log.Info("emit-check test-K");

        Assert.Single(_sink.Entries);
    }

    [Fact]
    public void Info_DefaultsLevelToInfo()
    {
        Log.Info("level-check test-L");

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal(LogLevel.Info, entry!.Level);
    }

    [Fact]
    public void Info_ExplicitChannelWithContext_CapturesPropsAndChannel()
    {
        // Overload 5: (string channel, string message, object context, ...)
        Log.Info("diagnostics", "structured-test-M", new { x = 99 });

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal("diagnostics", entry!.Channel);
        Assert.NotNull(entry.Context);
        Assert.Equal(99, entry.Context!["x"]);
    }

    [Fact]
    public void Info_Exception_DefaultChannel_PopulatesEntryException()
    {
        Exception ex = new InvalidOperationException("info-ex-test");

        Log.Info(ex, "info-exception-message");

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal(LogLevel.Info, entry!.Level);
        Assert.Same(ex, entry.Exception);
    }

    [Fact]
    public void Info_Exception_ExplicitChannel_PopulatesEntryException()
    {
        Exception ex = new InvalidOperationException("info-ex-channel-test");

        Log.Info("info-chan", ex, "info-exception-channel-message");

        LogEntry? entry = _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;
        Assert.NotNull(entry);
        Assert.Equal(LogLevel.Info, entry!.Level);
        Assert.Equal("info-chan", entry.Channel);
        Assert.Same(ex, entry.Exception);
    }
}
