using System.Collections.Generic;

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
            BuildInfo.Revision,
            ModListSnapshot.Capture()
        );
    }
}
