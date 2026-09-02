using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Concord;
using LudeonTK;
using RimWorks.RimLogging.Hijack;
using RimWorks.RimLogging.Patching;
using RimWorks.RimLogging.Viewer;
using Verse;

namespace RimWorks.RimLogging.Patches.Concord;

/// <summary>RimLogging's hooks as Concord injections. Ranked above Harmony.</summary>
public sealed class ConcordBackend : IPatchBackend, IPatchAttributionSource
{
    /// <inheritdoc/>
    public string Name => "Concord";

    /// <inheritdoc/>
    public int Priority => BackendPriority.Concord;

    /// <inheritdoc/>
    public void Apply()
    {
        Patcher.Apply(typeof(ConcordBackend).Assembly);
    }

    // Concord has no owner accessor yet; empty until that lands upstream
    /// <inheritdoc/>
    private static readonly object CacheLock = new object();

    private static volatile Dictionary<MethodBase, IReadOnlyList<string>> _ownerCache =
        new Dictionary<MethodBase, IReadOnlyList<string>>();

    /// <inheritdoc/>
    public IReadOnlyList<string>? OwnersFor(StackFrame frame)
    {
        // TODO(Concord.Ref 0.16.0): swap to Patcher.OwnersOfFrame(frame). the method on the stack
        // is concord's composed wrapper, so OwnersOf answers about the wrapper and finds nothing.
        // proven in game against a local 0.16.0 build; waiting on the published package.
        MethodBase? method = frame.GetMethod();
        if (method == null)
        {
            return Array.Empty<string>();
        }

        Dictionary<MethodBase, IReadOnlyList<string>> snapshot = _ownerCache;
        if (snapshot.TryGetValue(method, out IReadOnlyList<string>? hit))
        {
            return hit;
        }

        IReadOnlyList<string> owners = Patcher.OwnersOf(method) ?? Array.Empty<string>();
        lock (CacheLock)
        {
            _ownerCache = new Dictionary<MethodBase, IReadOnlyList<string>>(_ownerCache) { [method] = owners };
        }
        return owners;
    }
}

[Patch(typeof(Verse.Log))]
internal static class VerseLogPatch
{
    [Inject(At.Head, nameof(Verse.Log.Error), parameterTypes: [typeof(string)])]
    private static Control Error(string text) => LogHooks.OnError(text) ? Control.Continue : Control.Cancel;

    [Inject(At.Head, nameof(Verse.Log.Warning), parameterTypes: [typeof(string)])]
    private static Control Warning(string text) => LogHooks.OnWarning(text) ? Control.Continue : Control.Cancel;

    [Inject(At.Head, nameof(Verse.Log.Message), parameterTypes: [typeof(string)])]
    private static Control Message(string text) => LogHooks.OnMessage(text) ? Control.Continue : Control.Cancel;

    [Inject(At.Head, nameof(Verse.Log.TryOpenLogWindow))]
    private static Control TryOpenLogWindow() => ViewerHooks.OnTryOpenLogWindow() ? Control.Continue : Control.Cancel;
}

[Patch]
internal abstract class DebugWindowsOpenerPatch : DebugWindowsOpener
{
    [Inject(At.Head, "ToggleLogWindow")]
    private static Control ToggleLogWindow() => ViewerHooks.OnToggleLogWindow() ? Control.Continue : Control.Cancel;
}

[Patch]
internal abstract class UIRootPatch : UIRoot
{
    [Inject(At.Head, "CheckOpenLogWindow")]
    private static Control CheckOpenLogWindow() => ViewerHooks.OnCheckOpenLogWindow() ? Control.Continue : Control.Cancel;
}
