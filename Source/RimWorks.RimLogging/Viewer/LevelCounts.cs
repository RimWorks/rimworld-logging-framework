using System.Collections.Generic;

namespace RimWorks.RimLogging.Viewer;

/// <summary>Per-level row counts for the toolbar pills. Fatal folds into Error: the Error pill's
/// toggle also flips Fatal, so its count has to match what that click actually shows or hides.</summary>
internal readonly struct LevelCounts
{
    private readonly int[] counts;

    private LevelCounts(int[] counts)
    {
        this.counts = counts;
    }

    public int For(LogLevel level)
    {
        int slot = (int)level;
        if (counts == null || slot < 0 || slot >= counts.Length)
        {
            return 0;
        }
        int total = counts[slot];
        return level == LogLevel.Error ? total + For(LogLevel.Fatal) : total;
    }

    public static LevelCounts FromSnapshot(IReadOnlyList<LogEntry> snapshot)
    {
        int[] counts = new int[6];
        for (int i = 0; i < snapshot.Count; i++)
        {
            int slot = (int)snapshot[i].Level;
            if (slot >= 0 && slot < counts.Length)
            {
                counts[slot]++;
            }
        }
        return new LevelCounts(counts);
    }

    /// <summary>Copies the array: the caller (the sink) keeps mutating its own copy after this returns.</summary>
    internal static LevelCounts FromCounts(int[] countsBySlot)
    {
        int[] copy = new int[countsBySlot.Length];
        System.Array.Copy(countsBySlot, copy, countsBySlot.Length);
        return new LevelCounts(copy);
    }
}
