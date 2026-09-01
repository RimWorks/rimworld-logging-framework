namespace RimWorks.RimLogging.Bundle;

/// <summary>
/// Uploads a bundle to the proxy and reads the <c>url</c> field back out. A user's GitHub PAT
/// goes in the <c>X-Gist-Token</c> header, never the body, which is stored verbatim as the gist.
/// </summary>
public sealed class ProxyClient
{
    private readonly System.Net.Http.HttpClient _http;
    private readonly string _url;
    private readonly string? _githubToken;

    /// <summary>
    /// Creates a client targeting the given proxy URL. If no <see cref="System.Net.Http.HttpClient"/> is
    /// supplied, a default one with a 30-second timeout is used.
    /// </summary>
    /// <param name="url">The proxy endpoint URL to POST bundles to.</param>
    /// <param name="http">An optional HTTP client to reuse; a default client is created when <c>null</c>.</param>
    /// <param name="githubToken">An optional user-supplied GitHub PAT; when non-empty it is relayed to the proxy as an <c>X-Gist-Token</c> header so the gist is created under the user's account.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="url"/> is <c>null</c>.</exception>
    public ProxyClient(string url, System.Net.Http.HttpClient? http = null, string? githubToken = null)
    {
        _url = url ?? throw new System.ArgumentNullException(nameof(url));
        _http = http ?? new System.Net.Http.HttpClient { Timeout = System.TimeSpan.FromSeconds(30) };
        _githubToken = githubToken;
    }

    /// <summary>
    /// POSTs the payload and returns the gist URL from the response. Bad status, bad JSON, a
    /// missing url and transport errors all come back as a failed result instead of throwing.
    /// </summary>
    /// <param name="payload">The bundle to upload.</param>
    /// <returns>A <see cref="ProxyResult"/> describing success with the gist URL, or failure with an error message.</returns>
    public async System.Threading.Tasks.Task<ProxyResult> UploadAsync(BundlePayload payload)
    {
        try
        {
            string json = BundleSerializer.Serialize(payload);
            using System.Net.Http.StringContent content = new System.Net.Http.StringContent(
                json, System.Text.Encoding.UTF8, "application/json");
            using System.Net.Http.HttpRequestMessage request =
                new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, _url) { Content = content };
            if (!string.IsNullOrWhiteSpace(_githubToken))
                request.Headers.Add("X-Gist-Token", _githubToken);
            using System.Net.Http.HttpResponseMessage resp = await _http.SendAsync(request).ConfigureAwait(false);
            string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                return new ProxyResult { Success = false, ErrorMessage = $"{(int)resp.StatusCode} {resp.ReasonPhrase}: {body}" };
            }

            try
            {
                using System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(body);
                string gistUrl = doc.RootElement.GetProperty("url").GetString() ?? "";
                return new ProxyResult { Success = true, GistUrl = gistUrl };
            }
            catch (System.Text.Json.JsonException jex)
            {
                return new ProxyResult { Success = false, ErrorMessage = $"Malformed response JSON: {jex.Message}" };
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                return new ProxyResult { Success = false, ErrorMessage = "Response JSON missing 'url' field" };
            }
        }
        catch (System.Exception ex)
        {
            return new ProxyResult { Success = false, ErrorMessage = ex.Message };
        }
    }
}
