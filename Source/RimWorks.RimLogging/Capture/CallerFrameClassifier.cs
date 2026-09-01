using System;

namespace RimWorks.RimLogging.Capture;

/// <summary>
/// Decides whether a stack frame is logging infrastructure to skip, or the real caller.
/// </summary>
/// <remarks>
/// Skips by assembly name as well as namespace. Patchers ship MonoMod and Cecil under
/// namespaces that are not their own, so a namespace-only skip blames the patcher mod.
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
