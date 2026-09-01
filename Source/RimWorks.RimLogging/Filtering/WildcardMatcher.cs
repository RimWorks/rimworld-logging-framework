using System;
using System.Text.RegularExpressions;

namespace RimWorks.RimLogging.Filtering;

/// <summary>
/// Matches channel names against wildcard patterns using <c>*</c> as a wildcard.
/// </summary>
public static class WildcardMatcher
{
    /// <summary>
    /// Matches a channel against a pattern. A trailing <c>.*</c> also matches the prefix itself,
    /// any other <c>*</c> matches any run of characters, and no <c>*</c> means exact.
    /// </summary>
    /// <param name="pattern">The wildcard pattern.</param>
    /// <param name="input">The channel name to test.</param>
    /// <returns><c>true</c> if the input matches the pattern; otherwise <c>false</c>.</returns>
    public static bool Match(string pattern, string input)
    {
        if (pattern.EndsWith(".*", StringComparison.Ordinal))
        {
            string prefix = pattern.Substring(0, pattern.Length - 2);
            return input.Equals(prefix, StringComparison.Ordinal)
                || input.StartsWith(prefix + ".", StringComparison.Ordinal);
        }
        if (pattern.IndexOf('*') < 0)
            return input.Equals(pattern, StringComparison.Ordinal);
        string esc = Regex.Escape(pattern).Replace("\\*", ".*");
        return Regex.IsMatch(input, "^" + esc + "$", RegexOptions.None, TimeSpan.FromSeconds(1));
    }
}
