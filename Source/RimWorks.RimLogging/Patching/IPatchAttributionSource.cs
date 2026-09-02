using System.Collections.Generic;
using System.Diagnostics;

namespace RimWorks.RimLogging.Patching;

/// <summary>Reports which mods patched a method, so a thrown stack frame can name who touched it.</summary>
internal interface IPatchAttributionSource
{
    /// <summary>Owner ids that patched <paramref name="frame"/>'s method, or empty. Must never throw.</summary>
    IReadOnlyList<string> OwnersFor(StackFrame frame);
}
