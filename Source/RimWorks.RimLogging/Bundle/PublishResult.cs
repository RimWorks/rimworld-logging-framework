namespace RimWorks.RimLogging.Bundle;

/// <summary>
/// Outcome of a bundle upload: either a success carrying the resulting document URL,
/// or a failure carrying an error message.
/// </summary>
public sealed class PublishResult
{
    /// <summary>Whether the upload succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>The URL of the uploaded bundle on success; otherwise <c>null</c>.</summary>
    public string? Url { get; set; }

    /// <summary>A description of the failure on error; otherwise <c>null</c>.</summary>
    public string? ErrorMessage { get; set; }
}
