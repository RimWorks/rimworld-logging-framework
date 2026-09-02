using System.Collections.Generic;
using System.Diagnostics;

namespace RimWorks.RimLogging.Patching;

/// <summary>Reports which mods patched a method, so a thrown stack frame can name who touched it.</summary>
internal interface IPatchAttributionSource
{
    /// <summary>
    /// Owner ids that patched <paramref name="frame"/>'s method. Empty means the method really is
    /// unpatched; <c>null</c> means this source could not answer, which is not the same claim.
    /// Must never throw.
    /// </summary>
    IReadOnlyList<string>? OwnersFor(StackFrame frame);
}
