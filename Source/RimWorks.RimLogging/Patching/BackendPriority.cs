namespace RimWorks.RimLogging.Patching;

/// <summary>Priorities for the shipped backends. Concord wins when both libraries are loaded.</summary>
public static class BackendPriority
{
    /// <summary>Priority of the Concord backend.</summary>
    public const int Concord = 100;

    /// <summary>Priority of the Harmony backend.</summary>
    public const int Harmony = 0;
}
