namespace RimWorks.RimLogging.Bundle;

/// <summary>POSTs a bundle as JSON and reads the <c>url</c> field out of the JSON reply.</summary>
public sealed class BundleUploadClient
{
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

    /// <summary>
    /// POSTs the payload and returns the bundle URL from the response. Bad status, bad JSON, a
    /// missing url and transport errors all come back as a failed result instead of throwing.
    /// </summary>
    /// <param name="payload">The bundle to upload.</param>
    /// <returns>A <see cref="PublishResult"/> describing success with the bundle URL, or failure with an error message.</returns>
    public async System.Threading.Tasks.Task<PublishResult> UploadAsync(BundlePayload payload)
    {
        try
        {
            string json = BundleSerializer.Serialize(payload);
            using System.Net.Http.StringContent content = new System.Net.Http.StringContent(
                json, System.Text.Encoding.UTF8, "application/json");
            using System.Net.Http.HttpRequestMessage request =
                new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, _url) { Content = content };
            using System.Net.Http.HttpResponseMessage resp = await _http.SendAsync(request).ConfigureAwait(false);
            string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                return new PublishResult { Success = false, ErrorMessage = $"{(int)resp.StatusCode} {resp.ReasonPhrase}: {body}" };
            }

            try
            {
                using System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(body);
                string bundleUrl = doc.RootElement.GetProperty("url").GetString() ?? string.Empty;
                return new PublishResult { Success = true, Url = bundleUrl };
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
