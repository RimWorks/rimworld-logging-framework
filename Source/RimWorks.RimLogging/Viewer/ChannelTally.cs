namespace RimWorks.RimLogging.Viewer;

/// <summary>Per-channel row and error counts. Counts, not flags, so eviction can decrement.</summary>
internal struct ChannelTally {
    public int Count;
    public int ErrorCount;
}
