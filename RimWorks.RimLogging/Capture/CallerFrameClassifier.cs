using System;

namespace RimWorks.RimLogging.Capture;

/// <summary>
/// Pure predicate that decides whether a stack frame belongs to logging infrastructure
/// (and should be skipped when attributing a captured Verse.Log call to its originating
/// mod) or to a real caller (which should win channel attribution).
/// </summary>
/// <remarks>
/// Runtime patchers bundle their detour engine and analysis libraries (MonoMod, Mono.Cecil,
/// Iced, Microsoft.Cci, plus assorted System.* polyfills) under namespaces other than their
/// own top-level one. A namespace-only skip therefore lets MonoMod frames through and, because
/// the patcher mod is the one that ships those assemblies, every patched call gets misattributed
/// to that mod. Skipping by assembly name (and by the MonoMod / patcher namespaces independently,
/// for defence-in-depth) closes the gap. Concord weaves its wrappers through the same MonoMod
/// stack, so <c>Concord.*</c> frames are skipped alongside the legacy Harmony ones.
/// </remarks>
internal static class CallerFrameClassifier
{
    /// <summary>Assembly simple name of the Harmony 2.x library.</summary>
    internal const string HarmonyAssemblyName = "0Harmony";

    /// <summary>
    /// Returns <c>true</c> when a frame with the given declaring-type namespace and
    /// declaring-assembly simple name should be skipped during caller-channel resolution.
    /// </summary>
    /// <param name="declaringNamespace">
    /// <c>DeclaringType?.FullName</c> of the frame's <c>MethodBase</c>. May be <c>null</c>
    /// for DynamicMethod-emitted Harmony stubs.
    /// </param>
    /// <param name="assemblyName">
    /// <c>DeclaringType?.Assembly.GetName().Name</c> of the frame, or <c>null</c> when
    /// the frame has no declaring type.
    /// </param>
    internal static bool IsInternalFrame(string? declaringNamespace, string? assemblyName)
    {
        if (declaringNamespace == null) return true;
        if (declaringNamespace.StartsWith("HarmonyLib.", StringComparison.Ordinal)) return true;
        if (declaringNamespace.StartsWith("Concord.", StringComparison.Ordinal)) return true;
        if (declaringNamespace.StartsWith("MonoMod.", StringComparison.Ordinal)) return true;
        if (declaringNamespace.StartsWith("RimWorks.RimLogging.", StringComparison.Ordinal)) return true;
        if (string.Equals(assemblyName, HarmonyAssemblyName, StringComparison.Ordinal)) return true;
        return false;
    }
}
