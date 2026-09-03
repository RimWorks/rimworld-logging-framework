using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RimWorks.RimLogging.Capture;
using RimWorks.RimLogging.Format;

namespace RimWorks.RimLogging;

/// <summary>Entry point for emitting log messages through the RimLogging framework.</summary>
public static class Log
{
    /// <summary>Name of the default log channel used when no channel is specified.</summary>
    public const string DefaultChannel = "default";

    private static readonly Pipeline.LogThrottle Throttle = new Pipeline.LogThrottle();

    /// <summary>
    /// Attaches a key and value to every entry emitted on this thread until the returned handle
    /// is disposed. Nested scopes stack, and an explicit context at the call site still wins.
    /// </summary>
    /// <param name="key">The context key to attach.</param>
    /// <param name="value">The value to attach.</param>
    /// <returns>A handle that removes the pair when disposed.</returns>
    public static IDisposable PushContext(string key, object? value) => Pipeline.LogScope.Push(key, value);

    /// <summary>Log at Trace using a templated message and positional args (default channel).</summary>
    public static void Trace(
        string template,
        params object?[] args)
    {
        EmitInternal(LogLevel.Trace, DefaultChannel, template, args, structuredContext: null, exception: null,
                 new CallSite(SourceLocation.Empty, 0, string.Empty));
    }

    /// <summary>Log at Trace with an exception and a context message (default channel).</summary>
    public static void Trace(
        Exception ex,
        string message,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Trace, DefaultChannel, message, null, null, ex, new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Trace using a templated message, positional args, and explicit caller info (default channel).</summary>
    public static void TraceAt(
        string template,
        object?[] args,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Trace, DefaultChannel, template, args, null, null, new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Trace using a plain message and an anonymous-object context (default channel).</summary>
    public static void TraceWith(
        string message,
        object context,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Trace, DefaultChannel, message, args: null, structuredContext: context, exception: null,
                    new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Trace using an explicit channel, a templated message, and optional positional args.</summary>
    public static void TraceTo(
        string channel,
        string template,
        object?[]? args = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Trace, channel, template, args, null, null, new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Trace using an explicit channel, a plain message, and an anonymous-object context.</summary>
    public static void TraceTo(
        string channel,
        string message,
        object context,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Trace, channel, message, args: null, structuredContext: context, exception: null,
                    new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Trace with an exception and a context message (explicit channel).</summary>
    public static void TraceTo(
        string channel,
        Exception ex,
        string message,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Trace, channel, message, null, null, ex, new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Debug using a templated message and positional args (default channel).</summary>
    public static void Debug(
        string template,
        params object?[] args)
    {
        EmitInternal(LogLevel.Debug, DefaultChannel, template, args, structuredContext: null, exception: null,
                 new CallSite(SourceLocation.Empty, 0, string.Empty));
    }

    /// <summary>Log at Debug with an exception and a context message (default channel).</summary>
    public static void Debug(
        Exception ex,
        string message,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Debug, DefaultChannel, message, null, null, ex, new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Debug using a templated message, positional args, and explicit caller info (default channel).</summary>
    public static void DebugAt(
        string template,
        object?[] args,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Debug, DefaultChannel, template, args, null, null, new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Debug using a plain message and an anonymous-object context (default channel).</summary>
    public static void DebugWith(
        string message,
        object context,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Debug, DefaultChannel, message, args: null, structuredContext: context, exception: null,
                    new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Debug using an explicit channel, a templated message, and optional positional args.</summary>
    public static void DebugTo(
        string channel,
        string template,
        object?[]? args = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Debug, channel, template, args, null, null, new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Debug using an explicit channel, a plain message, and an anonymous-object context.</summary>
    public static void DebugTo(
        string channel,
        string message,
        object context,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Debug, channel, message, args: null, structuredContext: context, exception: null,
                    new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Debug with an exception and a context message (explicit channel).</summary>
    public static void DebugTo(
        string channel,
        Exception ex,
        string message,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Debug, channel, message, null, null, ex, new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Info using a templated message and positional args (default channel).</summary>
    public static void Info(
        string template,
        params object?[] args)
    {
        EmitInternal(LogLevel.Info, DefaultChannel, template, args, structuredContext: null, exception: null,
                 new CallSite(SourceLocation.Empty, 0, string.Empty));
    }

    /// <summary>Log at Info with an exception and a context message (default channel).</summary>
    public static void Info(
        Exception ex,
        string message,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Info, DefaultChannel, message, null, null, ex, new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Info using a templated message, positional args, and explicit caller info (default channel).</summary>
    public static void InfoAt(
        string template,
        object?[] args,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Info, DefaultChannel, template, args, null, null, new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Info using a plain message and an anonymous-object context (default channel).</summary>
    public static void InfoWith(
        string message,
        object context,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Info, DefaultChannel, message, args: null, structuredContext: context, exception: null,
                    new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Info using an explicit channel, a templated message, and optional positional args.</summary>
    public static void InfoTo(
        string channel,
        string template,
        object?[]? args = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Info, channel, template, args, null, null, new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Info using an explicit channel, a plain message, and an anonymous-object context.</summary>
    public static void InfoTo(
        string channel,
        string message,
        object context,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Info, channel, message, args: null, structuredContext: context, exception: null,
                    new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Info with an exception and a context message (explicit channel).</summary>
    public static void InfoTo(
        string channel,
        Exception ex,
        string message,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Info, channel, message, null, null, ex, new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Warn using a templated message and positional args (default channel).</summary>
    public static void Warn(
        string template,
        params object?[] args)
    {
        EmitInternal(LogLevel.Warn, DefaultChannel, template, args, structuredContext: null, exception: null,
                 new CallSite(SourceLocation.Empty, 0, string.Empty));
    }

    /// <summary>Log at Warn with an exception and a context message (default channel).</summary>
    public static void Warn(
        Exception ex,
        string message,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Warn, DefaultChannel, message, null, null, ex, new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Warn using a templated message, positional args, and explicit caller info (default channel).</summary>
    public static void WarnAt(
        string template,
        object?[] args,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Warn, DefaultChannel, template, args, null, null, new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Warn using a plain message and an anonymous-object context (default channel).</summary>
    public static void WarnWith(
        string message,
        object context,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Warn, DefaultChannel, message, args: null, structuredContext: context, exception: null,
                    new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Warn using an explicit channel, a templated message, and optional positional args.</summary>
    public static void WarnTo(
        string channel,
        string template,
        object?[]? args = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Warn, channel, template, args, null, null, new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Warn using an explicit channel, a plain message, and an anonymous-object context.</summary>
    public static void WarnTo(
        string channel,
        string message,
        object context,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Warn, channel, message, args: null, structuredContext: context, exception: null,
                    new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Warn with an exception and a context message (explicit channel).</summary>
    public static void WarnTo(
        string channel,
        Exception ex,
        string message,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Warn, channel, message, null, null, ex, new CallSite(SourceLocation.Empty, line, file));

    /// <summary>
    /// Log at Warn only the first time this key is seen. For callers in a tick loop, where the
    /// same warning would otherwise land sixty times a second.
    /// </summary>
    public static void WarnOnce(
        string key,
        string message,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => WarnOnceTo(DefaultChannel, key, message, line, file);

    /// <summary>
    /// Log at Warn only the first time this key is seen, on an explicit channel. The key is
    /// global, so the same key on two channels still only fires once.
    /// </summary>
    public static void WarnOnceTo(
        string channel,
        string key,
        string message,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
    {
        if (!Throttle.Once(key, DateTime.UtcNow)) return;
        EmitInternal(LogLevel.Warn, channel, message, null, null, null, new CallSite(SourceLocation.Empty, line, file));
    }

    /// <summary>Log at Error using a templated message and positional args (default channel).</summary>
    public static void Error(
        string template,
        params object?[] args)
    {
        EmitInternal(LogLevel.Error, DefaultChannel, template, args, structuredContext: null, exception: null,
                 new CallSite(SourceLocation.Empty, 0, string.Empty));
    }

    /// <summary>Log at Error with an exception and a context message (default channel).</summary>
    public static void Error(
        Exception ex,
        string message,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Error, DefaultChannel, message, null, null, ex, new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Error using a templated message, positional args, and explicit caller info (default channel).</summary>
    public static void ErrorAt(
        string template,
        object?[] args,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Error, DefaultChannel, template, args, null, null, new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Error using a plain message and an anonymous-object context (default channel).</summary>
    public static void ErrorWith(
        string message,
        object context,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Error, DefaultChannel, message, args: null, structuredContext: context, exception: null,
                    new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Error using an explicit channel, a templated message, and optional positional args.</summary>
    public static void ErrorTo(
        string channel,
        string template,
        object?[]? args = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Error, channel, template, args, null, null, new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Error using an explicit channel, a plain message, and an anonymous-object context.</summary>
    public static void ErrorTo(
        string channel,
        string message,
        object context,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Error, channel, message, args: null, structuredContext: context, exception: null,
                    new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Error with an exception and a context message (explicit channel).</summary>
    public static void ErrorTo(
        string channel,
        Exception ex,
        string message,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Error, channel, message, null, null, ex, new CallSite(SourceLocation.Empty, line, file));

    /// <summary>
    /// Log at Error only the first time this key is seen. For callers in a tick loop, where the
    /// same error would otherwise land sixty times a second.
    /// </summary>
    public static void ErrorOnce(
        string key,
        string message,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => ErrorOnceTo(DefaultChannel, key, message, line, file);

    /// <summary>
    /// Log at Error only the first time this key is seen, on an explicit channel. The key is
    /// global, so the same key on two channels still only fires once.
    /// </summary>
    public static void ErrorOnceTo(
        string channel,
        string key,
        string message,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
    {
        if (!Throttle.Once(key, DateTime.UtcNow)) return;
        EmitInternal(LogLevel.Error, channel, message, null, null, null, new CallSite(SourceLocation.Empty, line, file));
    }

    /// <summary>Log at Fatal using a templated message and positional args (default channel).</summary>
    public static void Fatal(
        string template,
        params object?[] args)
    {
        EmitInternal(LogLevel.Fatal, DefaultChannel, template, args, structuredContext: null, exception: null,
                 new CallSite(SourceLocation.Empty, 0, string.Empty));
    }

    /// <summary>Log at Fatal with an exception and a context message (default channel).</summary>
    public static void Fatal(
        Exception ex,
        string message,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Fatal, DefaultChannel, message, null, null, ex, new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Fatal using a templated message, positional args, and explicit caller info (default channel).</summary>
    public static void FatalAt(
        string template,
        object?[] args,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Fatal, DefaultChannel, template, args, null, null, new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Fatal using a plain message and an anonymous-object context (default channel).</summary>
    public static void FatalWith(
        string message,
        object context,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Fatal, DefaultChannel, message, args: null, structuredContext: context, exception: null,
                    new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Fatal using an explicit channel, a templated message, and optional positional args.</summary>
    public static void FatalTo(
        string channel,
        string template,
        object?[]? args = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Fatal, channel, template, args, null, null, new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Fatal using an explicit channel, a plain message, and an anonymous-object context.</summary>
    public static void FatalTo(
        string channel,
        string message,
        object context,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Fatal, channel, message, args: null, structuredContext: context, exception: null,
                    new CallSite(SourceLocation.Empty, line, file));

    /// <summary>Log at Fatal with an exception and a context message (explicit channel).</summary>
    public static void FatalTo(
        string channel,
        Exception ex,
        string message,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => EmitInternal(LogLevel.Fatal, channel, message, null, null, ex, new CallSite(SourceLocation.Empty, line, file));

    private static void EmitInternal(
        LogLevel level,
        string channel,
        string template,
        object?[]? args,
        object? structuredContext,
        Exception? exception,
        CallSite site)
    {
        // Global gate: the cheapest possible short-circuit. NO formatting, NO reflection.
        if (level < Logging.GlobalMinLevel) return;

        // then the channel's own gate, which only narrows further, and is memoised per channel
        string resolvedChannel = channel ?? DefaultChannel;
        Channels.ChannelSettings settings = Logging.SettingsFor(resolvedChannel);
        if (level < settings.MinLevelOr(Logging.GlobalMinLevel)) return;

        // A single stack walk, reused for both the formatted trace and the source fallback.
        System.Diagnostics.StackTrace? walk = settings.ShouldCaptureStack(level, Logging.CaptureStackTraces)
            ? new System.Diagnostics.StackTrace(1, true)
            : null;
        IReadOnlyList<string>? patchedBy = Array.Empty<string>();
        string? capturedTrace = walk != null ? Capture.StackWalker.FormatTrace(walk, out patchedBy) : null;

        SourceLocation src = ResolveSource(site.Line, site.File, site.Source, walk, out string? mod);
        (string rendered, IReadOnlyDictionary<string, object?>? ctx) = RenderMessage(template, args, structuredContext);
        ctx = Pipeline.LogScope.Merge(ctx);

        LogEntry entry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Channel = resolvedChannel,
            MessageTemplate = template ?? string.Empty,
            RenderedMessage = rendered,
            Context = ctx,
            Source = src,
            Tick = Logging.CurrentTick(),
            StackTrace = string.IsNullOrEmpty(capturedTrace) ? null : capturedTrace,
            Exception = exception,
            Mod = mod,
            PatchedBy = patchedBy,
        };

        Logging.Emit(entry);
    }

    /// <summary>
    /// Bundles the call-site source coordinates (explicit location plus the [CallerLineNumber]/[CallerFilePath]
    /// values) so the emit entry point stays within a reasonable parameter count.
    /// </summary>
    private readonly struct CallSite
    {
        public readonly SourceLocation Source;
        public readonly int Line;
        public readonly string File;

        public CallSite(SourceLocation source, int line, string file)
        {
            Source = source;
            Line = line;
            File = file;
        }
    }

    /// <summary>
    /// Resolves the source location for an entry: caller-info file/line first (also yielding the originating
    /// mod via ), then an explicit caller-provided location, then a single stack walk as the fallback.
    /// </summary>
    private static SourceLocation ResolveSource(int line, string file, SourceLocation explicitSource, System.Diagnostics.StackTrace? walk, out string? mod)
    {
        mod = null;
        if (line > 0 && !string.IsNullOrEmpty(file))
        {
            // the caller attributes give a path but no Type, and normalisation needs the
            // assembly to find the mod folder
            System.Type? callerType = ResolveCallerType(walk);
            if (callerType != null)
            {
                string shortPath = StackWalker.NormalizePath(file, callerType);
                mod = ModNameCache.ForAssembly(callerType.Assembly);
                return new SourceLocation(shortPath, line, null);
            }
            (string fallbackPath, string? resolvedMod) = ModResolution.ResolveFromPath(file, ModNameCache.Map());
            mod = resolvedMod;
            return new SourceLocation(fallbackPath, line, null);
        }
        if (explicitSource.IsCallerProvided) return explicitSource;
        return walk != null ? StackWalker.FirstCallerFrame(walk) : StackWalker.WalkOnce();
    }

    private static System.Type? ResolveCallerType(System.Diagnostics.StackTrace? walk)
    {
        if (walk != null) return StackWalker.FirstCallerType(walk);
        System.Diagnostics.StackTrace cheap = new System.Diagnostics.StackTrace(1, false);
        return StackWalker.FirstCallerType(cheap);
    }

    /// <summary>
    /// Renders the message template against  and merges in any structured context object. Returns the rendered
    /// string and the combined context dictionary (null when no context was supplied).
    /// </summary>
    private static (string rendered, IReadOnlyDictionary<string, object?>? context) RenderMessage(
        string template, object?[]? args, object? structuredContext)
    {
        string rendered;
        IReadOnlyDictionary<string, object?>? ctx = null;
        if (args != null && args.Length > 0)
        {
            Format.MessageTemplate t = TemplateCache.Get(template);
            (rendered, ctx) = t.Render(args);
        }
        else
        {
            rendered = template ?? string.Empty;
        }

        if (structuredContext != null)
        {
            IReadOnlyDictionary<string, object?>? captured = StructuredContext.Capture(structuredContext);
            if (captured != null)
                ctx = ctx == null ? captured : MergeContext(ctx, captured);
        }

        return (rendered, ctx);
    }

    /// <summary>Merges two context dictionaries, with <paramref name="overrides"/> winning on key collisions.</summary>
    private static Dictionary<string, object?> MergeContext(
        IReadOnlyDictionary<string, object?> baseCtx, IReadOnlyDictionary<string, object?> overrides)
    {
        Dictionary<string, object?> merged = new Dictionary<string, object?>(baseCtx.Count + overrides.Count);
        foreach (KeyValuePair<string, object?> kv in baseCtx) merged[kv.Key] = kv.Value;
        foreach (KeyValuePair<string, object?> kv in overrides) merged[kv.Key] = kv.Value;
        return merged;
    }

    /// <summary>
    /// Entry point for logs captured from outside our call sites, so the Unity bridge and the
    /// Verse.Log hijack. Source location is empty because file and line mean nothing here.
    /// </summary>
    internal static void EmitCaptured(LogLevel level, string channel, string text, string? stackTrace = null, string? mod = null)
    {
        if (level < Logging.GlobalMinLevel) return;

        Channels.ChannelSettings settings = Logging.SettingsFor(channel);
        if (level < settings.MinLevelOr(Logging.GlobalMinLevel)) return;

        System.Diagnostics.StackTrace? walk = (stackTrace == null && settings.ShouldCaptureStack(level, Logging.CaptureStackTraces))
            ? new System.Diagnostics.StackTrace(1, true)
            : null;
        IReadOnlyList<string>? patchedBy = Array.Empty<string>();
        string? captured = stackTrace ?? (walk != null ? Capture.StackWalker.FormatTrace(walk, out patchedBy) : null);
        SourceLocation src = walk != null ? Capture.StackWalker.FirstCallerFrame(walk) : SourceLocation.Empty;

        LogEntry e = new LogEntry
        {
            Timestamp = System.DateTime.UtcNow,
            Level = level,
            Channel = channel,
            MessageTemplate = text ?? string.Empty,
            RenderedMessage = text ?? string.Empty,
            Source = src,
            Context = Pipeline.LogScope.Merge(null),
            Tick = Logging.CurrentTick(),
            StackTrace = string.IsNullOrEmpty(captured) ? null : captured,
            Mod = mod,
            PatchedBy = patchedBy,
        };
        Logging.Emit(e);
    }
}
