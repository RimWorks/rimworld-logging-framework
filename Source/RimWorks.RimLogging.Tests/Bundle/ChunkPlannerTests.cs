using System.Collections.Generic;
using RimWorks.RimLogging.Bundle;
using Xunit;

namespace RimWorks.RimLogging.Tests.Bundle;

public class ChunkPlannerTests
{
    private static BundlePayload.EntryDto Entry(string message)
    {
        return new BundlePayload.EntryDto
        {
            Timestamp = "2026-10-22T00:00:00Z",
            Level = "Info",
            Channel = "Vanilla",
            Source = "test",
            Message = message,
        };
    }

    private static List<BundlePayload.EntryDto> Entries(int count)
    {
        List<BundlePayload.EntryDto> list = new List<BundlePayload.EntryDto>();
        for (int i = 0; i < count; i++) list.Add(Entry("line " + i));
        return list;
    }

    [Fact]
    public void AnEmptyBundlePlansNoChunks()
    {
        Assert.Empty(ChunkPlanner.Plan(Entries(0)));
    }

    [Fact]
    public void EntriesThatFitTogetherPlanOneChunk()
    {
        List<List<BundlePayload.EntryDto>> chunks = ChunkPlanner.Plan(Entries(50));

        Assert.Single(chunks);
        Assert.Equal(50, chunks[0].Count);
    }

    [Fact]
    public void TheEntryCountBudgetSplitsAChunk()
    {
        List<List<BundlePayload.EntryDto>> chunks =
            ChunkPlanner.Plan(Entries(25), maxEntriesPerChunk: 10);

        Assert.Equal(3, chunks.Count);
        Assert.Equal(10, chunks[0].Count);
        Assert.Equal(10, chunks[1].Count);
        Assert.Equal(5, chunks[2].Count);
    }

    [Fact]
    public void TheByteBudgetSplitsAChunk()
    {
        List<BundlePayload.EntryDto> entries = Entries(4);
        int one = BundleSerializer.MeasureEntry(entries[0]) + 1;

        List<List<BundlePayload.EntryDto>> chunks =
            ChunkPlanner.Plan(entries, maxChunkBytes: (one * 2) + 16);

        Assert.Equal(2, chunks.Count);
        Assert.Equal(2, chunks[0].Count);
        Assert.Equal(2, chunks[1].Count);
    }

    [Fact]
    public void EveryEntryKeepsItsPlaceAcrossTheChunks()
    {
        List<BundlePayload.EntryDto> entries = Entries(7);

        List<List<BundlePayload.EntryDto>> chunks =
            ChunkPlanner.Plan(entries, maxEntriesPerChunk: 3);

        List<BundlePayload.EntryDto> flat = new List<BundlePayload.EntryDto>();
        foreach (List<BundlePayload.EntryDto> chunk in chunks) flat.AddRange(chunk);
        Assert.Equal(entries, flat);
    }

    [Fact]
    public void AnEntryTooBigForTheBudgetStillGoesOnItsOwn()
    {
        List<BundlePayload.EntryDto> entries = new List<BundlePayload.EntryDto>
        {
            Entry("small"),
            Entry(new string('x', 4096)),
        };

        List<List<BundlePayload.EntryDto>> chunks = ChunkPlanner.Plan(entries, maxChunkBytes: 256);

        Assert.Equal(2, chunks.Count);
        Assert.Single(chunks[1]);
        Assert.Equal(entries[1], chunks[1][0]);
    }

    [Fact]
    public void PastTheChunkLimitTheOldestChunksAreDropped()
    {
        List<BundlePayload.EntryDto> entries = Entries(10);

        List<List<BundlePayload.EntryDto>> chunks =
            ChunkPlanner.Plan(entries, maxEntriesPerChunk: 1, maxChunks: 3);

        Assert.Equal(3, chunks.Count);
        Assert.Equal(entries[7], chunks[0][0]);
        Assert.Equal(entries[9], chunks[2][0]);
    }
}
