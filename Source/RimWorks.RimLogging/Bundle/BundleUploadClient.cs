namespace RimWorks.RimLogging.Bundle;

/// <summary>Nothing here throws: a bad status, bad JSON or a dead network all return a failed result.</summary>
public sealed class BundleUploadClient
{
    private const string TokenHeader = "x-bundle-token";
    private const char Separator = '/';
    private const string ChunkSegment = "chunk";

    private readonly System.Net.Http.HttpClient _http;
    private readonly string _url;

    /// <summary>
    /// Creates a client targeting the given upload URL. If no <see cref="System.Net.Http.HttpClient"/> is
    /// supplied, a default one with a 30-second timeout is used.
    /// </summary>
    /// <param name="url">The endpoint URL to POST bundles to.</param>
    /// <param name="http">An optional HTTP client to reuse; a default client is created when <c>null</c>.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="url"/> is <c>null</c>.</exception>
    public BundleUploadClient(string url, System.Net.Http.HttpClient? http = null)
    {
        _url = url ?? throw new System.ArgumentNullException(nameof(url));
        _http = http ?? new System.Net.Http.HttpClient { Timeout = System.TimeSpan.FromSeconds(30) };
    }

    /// <summary>POSTs the payload and returns the bundle URL, its id, and the token later appends need.</summary>
    /// <param name="payload">The bundle to upload.</param>
    /// <returns>A <see cref="PublishResult"/> describing success with the bundle URL, or failure with an error message.</returns>
    public System.Threading.Tasks.Task<PublishResult> UploadAsync(BundlePayload payload)
        => SendAsync(_url, BundleSerializer.Serialize(payload), null, ReadCreated);

    /// <summary>POSTs one more batch of entries onto a bundle this session already created.</summary>
    /// <param name="id">The bundle's id, from the result of <see cref="UploadAsync"/>.</param>
    /// <param name="token">The append token, from the result of <see cref="UploadAsync"/>.</param>
    /// <param name="entries">The entries to append.</param>
    /// <returns>A <see cref="PublishResult"/> carrying the bundle id on success.</returns>
    public System.Threading.Tasks.Task<PublishResult> AppendAsync(
        string id,
        string token,
        System.Collections.Generic.IReadOnlyList<BundlePayload.EntryDto> entries)
        => SendAsync(
            _url.TrimEnd(Separator) + Separator + id + Separator + ChunkSegment,
            BundleSerializer.SerializeChunk(entries),
            token,
            _ => new PublishResult { Success = true, Id = id });

    private static PublishResult ReadCreated(string body)
    {
        using System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(body);
        System.Text.Json.JsonElement root = doc.RootElement;
        return new PublishResult
        {
            Success = true,
            Url = root.GetProperty("url").GetString() ?? string.Empty,
            Id = root.TryGetProperty("id", out System.Text.Json.JsonElement id) ? id.GetString() : null,
            Token = root.TryGetProperty("token", out System.Text.Json.JsonElement token) ? token.GetString() : null,
        };
    }

    private async System.Threading.Tasks.Task<PublishResult> SendAsync(
        string url,
        string json,
        string? token,
        System.Func<string, PublishResult> read)
    {
        try
        {
            using System.Net.Http.StringContent content = new System.Net.Http.StringContent(
                json, System.Text.Encoding.UTF8, "application/json");
            using System.Net.Http.HttpRequestMessage request =
                new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, url) { Content = content };
            if (token != null) request.Headers.Add(TokenHeader, token);

            using System.Net.Http.HttpResponseMessage resp = await _http.SendAsync(request).ConfigureAwait(false);
            string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                return new PublishResult { Success = false, ErrorMessage = $"{(int)resp.StatusCode} {resp.ReasonPhrase}: {body}" };
            }

            try
            {
                return read(body);
            }
            catch (System.Text.Json.JsonException jex)
            {
                return new PublishResult { Success = false, ErrorMessage = $"Malformed response JSON: {jex.Message}" };
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                return new PublishResult { Success = false, ErrorMessage = "Response JSON missing 'url' field" };
            }
        }
        catch (System.Exception ex)
        {
            return new PublishResult { Success = false, ErrorMessage = ex.Message };
        }
    }
}
