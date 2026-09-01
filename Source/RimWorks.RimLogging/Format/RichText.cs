namespace RimWorks.RimLogging.Format;

/// <summary>Strips Unity rich-text markup tags from log messages before writing to file sinks.</summary>
public static class RichText
{
    private static readonly System.TimeSpan RegexTimeout = System.TimeSpan.FromSeconds(1);

    private static readonly System.Text.RegularExpressions.Regex _stripPattern = new(
        @"</?(color(?:=[^>]*)?|b|i|size(?:=[^>]*)?)>",
        System.Text.RegularExpressions.RegexOptions.Compiled |
        System.Text.RegularExpressions.RegexOptions.IgnoreCase,
        RegexTimeout);

    /// <summary>Returns <paramref name="input"/> with all recognized rich-text tags removed.</summary>
    public static string Strip(string? input)
    {
        if (string.IsNullOrEmpty(input)) return input ?? string.Empty;
        return _stripPattern.Replace(input, string.Empty);
    }
}
