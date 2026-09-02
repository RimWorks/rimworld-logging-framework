using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using RimWorks.RimLogging.Sinks;

namespace RimWorks.RimLogging.Bundle;

/// <summary>
/// Backs the "Upload bundle" actions: finds the buffer, picks the publisher, and turns the
/// result into a message. Verse-free so the decision logic tests without the game.
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

    /// <summary>Returns <c>true</c> when the publisher id selects the gist path; anything else means Docbin.</summary>
    internal static bool UsesGist(string? publisher)
        => string.Equals(publisher, "gist", StringComparison.OrdinalIgnoreCase);

    /// <summary>Uploads the bundle through whichever publisher the options select.</summary>
    internal static Task<PublishResult> Upload(BundlePayload payload, PublishOptions options, HttpClient? http = null)
    {
        if (UsesGist(options.Publisher))
        {
            return new ProxyClient(options.ProxyUrl, http, options.GitHubToken).UploadAsync(payload);
        }

        return new DocbinClient(options.DocbinUrl, http, options.DocbinApiKey, options.DocbinVisibility)
            .UploadAsync(payload);
    }
}
