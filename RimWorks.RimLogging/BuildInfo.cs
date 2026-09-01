namespace RimWorks.RimLogging;

/// <summary>
/// Framework version metadata embedded in bug-report bundles. The literals are placeholders;
/// a release build is expected to substitute the real revision and timestamp before publishing.
/// </summary>
public static class BuildInfo
{
    /// <summary>Framework revision string reported in bundle metadata (placeholder value <c>"0.0.0"</c>).</summary>
    public const string Revision = "2.2.1";

    /// <summary>UTC build timestamp in ISO-8601 form reported in bundle metadata (placeholder epoch value).</summary>
    public const string BuildTime = "2026-09-01T01:32:49.133Z";
}
