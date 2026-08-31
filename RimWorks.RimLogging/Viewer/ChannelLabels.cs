namespace RimWorks.RimLogging.Viewer;

/// <summary>Display strings the channel tree needs. Passed in so the filter logic stays free of Verse.</summary>
internal readonly struct ChannelLabels {
    public readonly string All;
    public readonly string ModGroup;
    public readonly string VanillaGroup;

    public ChannelLabels(string all, string modGroup, string vanillaGroup) {
        All = all;
        ModGroup = modGroup;
        VanillaGroup = vanillaGroup;
    }
}
