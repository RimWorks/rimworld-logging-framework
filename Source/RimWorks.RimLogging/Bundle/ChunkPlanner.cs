using System.Collections.Generic;

namespace RimWorks.RimLogging.Bundle;

/// <summary>
/// Splits a bundle's entries into batches the upload endpoint accepts, measuring each entry at its
/// real serialized size rather than estimating one.
/// </summary>
public static class ChunkPlanner
{
    /// <summary>Planned under the endpoint's 5 MiB body limit, leaving room for the JSON around the entries.</summary>
    public const int MaxChunkBytes = 4 * 1024 * 1024;

    /// <summary>The entry count the endpoint accepts in one batch.</summary>
    public const int MaxEntriesPerChunk = 10000;

    /// <summary>The batch count the endpoint accepts in one bundle.</summary>
    public const int MaxChunks = 20;

    private const int EnvelopeBytes = 16;

    /// <summary>
    /// Batches entries oldest first, dropping the oldest batches when they need more than
    /// <paramref name="maxChunks"/>.
    /// </summary>
    /// <param name="entries">The entries to batch, oldest first.</param>
    /// <param name="maxChunkBytes">The serialized byte budget for one batch.</param>
    /// <param name="maxEntriesPerChunk">The entry count budget for one batch.</param>
    /// <param name="maxChunks">How many batches the bundle may hold.</param>
    /// <returns>The batches to upload, oldest first. Empty when <paramref name="entries"/> is empty.</returns>
    public static List<List<BundlePayload.EntryDto>> Plan(
        IReadOnlyList<BundlePayload.EntryDto> entries,
        int maxChunkBytes = MaxChunkBytes,
        int maxEntriesPerChunk = MaxEntriesPerChunk,
        int maxChunks = MaxChunks)
    {
        List<List<BundlePayload.EntryDto>> chunks = new List<List<BundlePayload.EntryDto>>();
        if (entries == null || entries.Count == 0) return chunks;

        List<BundlePayload.EntryDto> current = new List<BundlePayload.EntryDto>();
        int used = EnvelopeBytes;

        for (int i = 0; i < entries.Count; i++)
        {
            int cost = BundleSerializer.MeasureEntry(entries[i]) + 1;
            bool full = current.Count >= maxEntriesPerChunk ||
                (current.Count > 0 && used + cost > maxChunkBytes);
            if (full)
            {
                chunks.Add(current);
                current = new List<BundlePayload.EntryDto>();
                used = EnvelopeBytes;
            }

            current.Add(entries[i]);
            used += cost;
        }

        chunks.Add(current);
        if (chunks.Count > maxChunks) chunks.RemoveRange(0, chunks.Count - maxChunks);
        return chunks;
    }
}
