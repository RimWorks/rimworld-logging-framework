namespace RimWorks.RimLogging.Viewer;

internal readonly struct LogChannel {
    public readonly string Id;
    public readonly string Name;
    public readonly int Count;
    public readonly int Depth;
    public readonly bool HasError;
    public readonly bool HasChildren;
    public readonly bool Expanded;

    public LogChannel(string id, string name, int count, int depth, bool hasError, bool hasChildren, bool expanded) {
        Id = id;
        Name = name;
        Count = count;
        Depth = depth;
        HasError = hasError;
        HasChildren = hasChildren;
        Expanded = expanded;
    }
}
