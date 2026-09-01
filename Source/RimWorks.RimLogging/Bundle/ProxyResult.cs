namespace RimWorks.RimLogging.Bundle;

/// <summary>
/// Outcome of a <see cref="ProxyClient.UploadAsync"/> call: either a success carrying the resulting gist URL,
/// or a failure carrying an error message.
/// </summary>
public sealed class ProxyResult
{
    /// <summary>Whether the upload succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>The URL of the created gist on success; otherwise <c>null</c>.</summary>
    public string? GistUrl { get; set; }

    /// <summary>A description of the failure on error; otherwise <c>null</c>.</summary>
    public string? ErrorMessage { get; set; }
}
