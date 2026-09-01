using System.Text;

namespace RimWorks.RimLogging.Capture;

/// <summary>
/// Converts raw mod packageIds into clean dot-separated channel name segments
/// by stripping invalid characters and normalising dot sequences.
/// </summary>
public static class PackageIdSanitizer
{
    /// <summary>
    /// Keeps letters, digits, underscores and dots, collapses repeated dots, and trims dots
    /// off both ends. Returns <c>"Unknown"</c> if nothing is left.
    /// </summary>
    /// <param name="packageId">The raw mod packageId, e.g. <c>"com.cryptiklemur.rimobs"</c>.</param>
    /// <returns>A sanitized segment string, or <c>"Unknown"</c> when no valid characters remain.</returns>
    public static string ToChannelSegment(string packageId)
    {
        if (string.IsNullOrEmpty(packageId)) return "Unknown";

        StringBuilder sb = new StringBuilder(packageId.Length);
        bool lastWasDot = false;

        for (int i = 0; i < packageId.Length; i++)
        {
            char c = packageId[i];
            if (c == '.')
            {
                if (!lastWasDot && sb.Length > 0) sb.Append('.');
                lastWasDot = true;
            }
            else if (char.IsLetterOrDigit(c) || c == '_')
            {
                sb.Append(c);
                lastWasDot = false;
            }
        }

        while (sb.Length > 0 && sb[sb.Length - 1] == '.') sb.Length--;

        return sb.Length == 0 ? "Unknown" : sb.ToString();
    }
}
