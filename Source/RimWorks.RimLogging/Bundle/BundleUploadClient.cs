namespace RimWorks.RimLogging.Bundle;

/// <summary>Nothing here throws: a bad status, bad JSON or a dead network all return a failed result.</summary>
public sealed class BundleUploadClient
{
    private const string TokenHeader = "x-bundle-token";
    private const char Separator = '/';
    private const string ChunkSegment = "chunk";
    private const int TimeoutMs = 30_000;

    private readonly string _url;

    /// <summary>Creates a client targeting the given upload URL.</summary>
    /// <param name="url">The endpoint URL to POST bundles to.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="url"/> is <c>null</c>.</exception>
    public BundleUploadClient(string url)
    {
        _url = url ?? throw new System.ArgumentNullException(nameof(url));
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

    private static async System.Threading.Tasks.Task<PublishResult> SendAsync(
        string url,
        string json,
        string? token,
        System.Func<string, PublishResult> read)
    {
        try
        {
#pragma warning disable SYSLIB0014
            System.Net.HttpWebRequest request = System.Net.WebRequest.CreateHttp(url);
#pragma warning restore SYSLIB0014
            request.Method = "POST";
            request.ContentType = "application/json; charset=utf-8";
            request.Timeout = TimeoutMs;
            request.ReadWriteTimeout = TimeoutMs;
            if (token != null) request.Headers[TokenHeader] = token;

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
            using (System.IO.Stream stream = await request.GetRequestStreamAsync().ConfigureAwait(false))
            {
                await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            }

            System.Net.HttpWebResponse resp;
            try
            {
                resp = (System.Net.HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false);
            }
            catch (System.Net.WebException wex) when (wex.Response is System.Net.HttpWebResponse refused)
            {
                resp = refused;
            }

            using (resp)
            {
                string body;
                using (System.IO.StreamReader reader = new System.IO.StreamReader(resp.GetResponseStream(), System.Text.Encoding.UTF8))
                {
                    body = await reader.ReadToEndAsync().ConfigureAwait(false);
                }

                int status = (int)resp.StatusCode;
                if (status < 200 || status > 299)
                {
                    return new PublishResult { Success = false, ErrorMessage = $"{status} {resp.StatusDescription}: {body}" };
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
        }
        catch (System.Exception ex)
        {
            return new PublishResult { Success = false, ErrorMessage = ex.Message };
        }
    }
}
