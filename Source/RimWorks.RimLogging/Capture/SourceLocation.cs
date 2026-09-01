namespace RimWorks.RimLogging.Capture;

/// <summary>
/// Where a log event came from. Filled by the compile-time caller attributes, or by the
/// stack walker when those are unavailable.
/// </summary>
public readonly struct SourceLocation
{
    /// <summary>Gets the source file path at the call site.</summary>
    public string File { get; }

    /// <summary>Gets the line number within <see cref="File"/> at the call site.</summary>
    public int Line { get; }

    /// <summary>Gets the member name at the call site, or <c>null</c> when unavailable.</summary>
    public string? Method { get; }

    /// <summary>
    /// Initializes a new <see cref="SourceLocation"/>.
    /// </summary>
    /// <param name="file">Source file path. <c>null</c> is treated as <see cref="string.Empty"/>.</param>
    /// <param name="line">Line number (1-based). Zero indicates an unknown location.</param>
    /// <param name="method">Member name, or <c>null</c> when unavailable.</param>
    public SourceLocation(string file, int line, string? method)
    {
        File = file ?? string.Empty;
        Line = line;
        Method = method;
    }

    /// <summary>
    /// Gets a value indicating whether this location was populated by caller attributes rather than a runtime
    /// stack-walk fallback. true when both is non-empty and  is greater than zero.
    /// </summary>
    public bool IsCallerProvided => Line > 0 && !string.IsNullOrEmpty(File);

    /// <summary>
    /// A <see cref="SourceLocation"/> that represents an unknown or unavailable location.
    /// <see cref="IsCallerProvided"/> is always <c>false</c> for this instance.
    /// </summary>
    public static SourceLocation Empty { get; } = new SourceLocation(string.Empty, 0, null);
}
