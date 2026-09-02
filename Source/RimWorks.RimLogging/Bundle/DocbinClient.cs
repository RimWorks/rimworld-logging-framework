using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RimWorks.RimLogging.Bundle;

/// <summary>
/// Uploads a bundle to Docbin as plain text. An API key raises the size ceiling and lets the
/// paste carry a name and visibility; without one the anonymous endpoint is used.
/// </summary>
public sealed class DocbinClient
{
    private readonly string? apiKey;
    private readonly string baseUrl;
    private readonly HttpClient http;
    private readonly string? visibility;

    /// <summary>Creates a client targeting a Docbin instance.</summary>
    /// <param name="baseUrl">The Docbin base URL, for example <c>https://docbin.app</c>.</param>
    /// <param name="http">An optional HTTP client to reuse; a default with a 30-second timeout is created when <c>null</c>.</param>
    /// <param name="apiKey">An optional Docbin API key; when set the paste is authenticated.</param>
    /// <param name="visibility">The visibility for authenticated pastes; defaults to <c>unlisted</c>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="baseUrl"/> is <c>null</c>.</exception>
    public DocbinClient(string baseUrl, HttpClient? http = null, string? apiKey = null, string? visibility = null)
    {
        this.baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        this.http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        this.apiKey = apiKey;
        this.visibility = visibility;
    }

    /// <summary>
    /// Renders the bundle to text, trims it to Docbin's ceiling, and POSTs it. Bad status, bad
    /// JSON and transport errors all come back as a failed result instead of throwing.
    /// </summary>
    /// <param name="payload">The bundle to upload.</param>
    /// <returns>A <see cref="PublishResult"/> carrying the document URL, or the failure reason.</returns>
    public async Task<PublishResult> UploadAsync(BundlePayload payload)
    {
        try
        {
            bool authed = !string.IsNullOrWhiteSpace(apiKey);
            string text = BundleTrimmer.Cap(BundleTextRenderer.Render(payload), DocbinRequest.MaxBytes(authed));
            string url = DocbinRequest.Url(
                baseUrl, authed, DocbinVisibility.Effective(authed, visibility), DocbinRequest.NameFor(DateTime.UtcNow));

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
            if (authed) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(text, Encoding.UTF8, "text/plain");

            using HttpResponseMessage resp = await http.SendAsync(request).ConfigureAwait(false);
            string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                return new PublishResult { Success = false, ErrorMessage = $"{(int)resp.StatusCode} {resp.ReasonPhrase}: {body}" };
            }

            return new PublishResult { Success = true, Url = ExtractUrl(body) };
        }
        catch (Exception ex)
        {
            return new PublishResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private static string ExtractUrl(string body)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("url", out JsonElement url))
            {
                return url.GetString() ?? body.Trim();
            }
        }
        catch (JsonException)
        {
            // docbin answers with a bare URL when it is not asked for JSON
        }

        return body.Trim();
    }
}
