using System.Collections.Generic;

namespace RimWorks.RimLogging.Patching;

/// <summary>Ranks the loaded backends. Pure so it tests without Verse.</summary>
internal static class BackendChoice
{
    /// <summary>Orders backends best-first, so a library that throws hands over to the next.</summary>
    internal static List<IPatchBackend> Ranked(IEnumerable<IPatchBackend> found)
    {
        List<IPatchBackend> ranked = new List<IPatchBackend>(found);
        ranked.Sort(static (a, b) => b.Priority.CompareTo(a.Priority));
        return ranked;
    }
}
