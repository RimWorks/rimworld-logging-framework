using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public IReadOnlyList<string> OwnersFor(StackFrame frame) => Array.Empty<string>();
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
