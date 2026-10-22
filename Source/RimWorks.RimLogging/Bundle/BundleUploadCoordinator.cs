using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using RimWorks.RimLogging.Sinks;

namespace RimWorks.RimLogging.Bundle;

/// <summary>
/// Backs the "Upload bundle" actions: finds the buffer, uploads it to rimworks.org, and turns
/// the result into a message. Verse-free so the decision logic tests without the game.
/// </summary>
public static class BundleUploadCoordinator
{
    /// <summary>Returns the first registered <see cref="MemoryLogSink"/>, or <c>null</c> when none is registered.</summary>
    /// <param name="sinks">The registered sinks to scan.</param>
    public static MemoryLogSink? FindMemorySink(IReadOnlyList<ILogSink> sinks)
    {
        for (int i = 0; i < sinks.Count; i++)
        {
            if (sinks[i] is MemoryLogSink memory) return memory;
        }
        return null;
    }

    /// <summary>Formats an upload outcome into a single user-facing line: the URL on success, the error otherwise.</summary>
    /// <param name="result">The upload outcome to describe.</param>
    public static string DescribeResult(PublishResult result)
    {
        if (result.Success) return $"Bundle uploaded: {result.Url}";
        return $"Bundle upload failed: {result.ErrorMessage}";
    }

    internal static async Task<PublishResult> Upload(BundlePayload payload, PublishOptions options, HttpClient? http = null)
    {
        BundleUploadClient client = new BundleUploadClient(options.RimWorksUrl, http);
        List<List<BundlePayload.EntryDto>> chunks = ChunkPlanner.Plan(payload.Entries);
        if (chunks.Count <= 1) return await client.UploadAsync(payload).ConfigureAwait(false);

        BundlePayload head = new BundlePayload
        {
            RimWorldVersion = payload.RimWorldVersion,
            FrameworkVersion = payload.FrameworkVersion,
            Mods = payload.Mods,
            Entries = chunks[0],
        };

        PublishResult created = await client.UploadAsync(head).ConfigureAwait(false);
        if (!created.Success) return created;

        PublishResult rest = await AppendAll(client, created, chunks, 1).ConfigureAwait(false);
        return rest.Success ? created : rest;
    }

    internal static async Task<PublishResult> Append(
        string id,
        string token,
        IReadOnlyList<BundlePayload.EntryDto> entries,
        PublishOptions options,
        HttpClient? http = null)
    {
        BundleUploadClient client = new BundleUploadClient(options.RimWorksUrl, http);
        List<List<BundlePayload.EntryDto>> chunks = ChunkPlanner.Plan(entries);
        PublishResult seed = new PublishResult { Success = true, Id = id, Token = token };
        return await AppendAll(client, seed, chunks, 0).ConfigureAwait(false);
    }

    private static async Task<PublishResult> AppendAll(
        BundleUploadClient client,
        PublishResult created,
        List<List<BundlePayload.EntryDto>> chunks,
        int from)
    {
        if (created.Id == null || created.Token == null)
        {
            return new PublishResult { Success = false, ErrorMessage = "Response JSON missing 'id' or 'token'" };
        }

        for (int i = from; i < chunks.Count; i++)
        {
            PublishResult appended =
                await client.AppendAsync(created.Id, created.Token, chunks[i]).ConfigureAwait(false);
            if (!appended.Success) return appended;
        }

        return created;
    }
}
