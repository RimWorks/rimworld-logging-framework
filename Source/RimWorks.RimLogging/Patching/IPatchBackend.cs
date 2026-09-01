namespace RimWorks.RimLogging.Patching;

/// <summary>
/// One patching library. A backend states RimLogging's hooks in its own terms, so the core
/// never references Harmony or Concord.
/// </summary>
public interface IPatchBackend
{
    /// <summary>Name used in the log line that reports which backend won.</summary>
    string Name { get; }

    /// <summary>Higher wins when more than one library is loaded.</summary>
    int Priority { get; }

    /// <summary>Applies every RimLogging hook. Called on the winning backend only.</summary>
    void Apply();
}
