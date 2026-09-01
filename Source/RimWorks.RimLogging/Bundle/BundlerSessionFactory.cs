using System.Collections.Generic;
using System.Reflection;

namespace RimWorks.RimLogging.Bundle;

/// <summary>
/// Convenience factory that builds a <see cref="BundlePayload"/> for the currently running game session,
/// supplying the live RimWorld version, framework revision, and captured mod list automatically.
/// </summary>
public static class BundlerSessionFactory
{
    /// <summary>
    /// Builds a bundle for the running session, pulling the current RimWorld version, framework revision,
    /// and a snapshot of the loaded mods from the live game state.
    /// </summary>
    /// <param name="entries">The log entries to include in the bundle.</param>
    /// <returns>A <see cref="BundlePayload"/> populated with the current session's metadata.</returns>
    public static BundlePayload BuildForRunningSession(IReadOnlyList<LogEntry> entries)
    {
        return Bundler.Build(
            entries,
            RimWorld.VersionControl.CurrentVersionString,
            Revision(),
            ModListSnapshot.Capture()
        );
    }

    // comes from the build, so a release stamps it without touching tracked source
    private static string Revision()
    {
        Assembly assembly = typeof(BundlerSessionFactory).Assembly;
        string? version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrEmpty(version))
        {
            return assembly.GetName().Version?.ToString() ?? "0.0.0";
        }
        int plus = version!.IndexOf('+');
        return plus < 0 ? version : version.Substring(0, plus);
    }
}
