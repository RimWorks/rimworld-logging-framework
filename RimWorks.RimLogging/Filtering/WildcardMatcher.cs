using System;
using System.Text.RegularExpressions;

namespace RimWorks.RimLogging.Filtering;

/// <summary>
/// Matches channel names against wildcard patterns using <c>*</c> as a wildcard.
/// </summary>
public static class WildcardMatcher
{
    /// <summary>
    /// Tests whether <paramref name="input"/> matches <paramref name="pattern"/>. A trailing
    /// <c>.*</c> matches the prefix itself or any dot-delimited descendant; other <c>*</c>
    /// characters match any sequence of characters; a pattern with no <c>*</c> requires an exact
    /// ordinal match.
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
