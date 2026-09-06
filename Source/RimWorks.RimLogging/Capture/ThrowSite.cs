using System;
using System.Text.RegularExpressions;

namespace RimWorks.RimLogging.Capture;

/// <summary>
/// Reads the throw site out of a formatted stack trace. A captured exception is logged by
/// whoever caught it, so the caller walk finds the handler rather than the code that failed.
/// </summary>
public static class ThrowSite
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    // Mono writes "at T.M () [0x0] in /p/F.cs:15", .NET writes "at T.M() in /p/F.cs:line 15".
    // A frame with no symbols reads "in <mvid>:0", so a leading '<' rules the line out.
    private static readonly Regex MonoOrDotNet = new Regex(
        @"\sin\s(?<file>[^<\r\n]+?):(?:line\s)?(?<line>\d+)\s*$",
        RegexOptions.Compiled, RegexTimeout);

    // our own FormatTrace shape, "at T.M (Path/File:15)"
    private static readonly Regex Formatted = new Regex(
        @"\((?<file>[^<>()\r\n]+?):(?<line>\d+)\)\s*$",
        RegexOptions.Compiled, RegexTimeout);

    private static readonly Regex MethodName = new Regex(
        @"^\s*at\s+(?<method>[^\s(]+)",
        RegexOptions.Compiled, RegexTimeout);

    /// <summary>The first frame carrying a real file and line, or <c>null</c> when none does.</summary>
    public static SourceLocation? From(string? trace)
    {
        if (string.IsNullOrEmpty(trace)) return null;

        foreach (string line in trace!.Split('\n'))
        {
            Match hit = MonoOrDotNet.Match(line);
            if (!hit.Success) hit = Formatted.Match(line);
            if (!hit.Success) continue;

            if (!int.TryParse(hit.Groups["line"].Value, out int number) || number <= 0) continue;

            string file = hit.Groups["file"].Value.Trim();
            if (file.Length == 0) continue;

            return new SourceLocation(file, number, MethodOf(line));
        }

        return null;
    }

    // "RimWorks.RimLogging.Dev.Probe.Throw" becomes "Throw", matching what the walk reports
    private static string? MethodOf(string line)
    {
        Match hit = MethodName.Match(line);
        if (!hit.Success) return null;

        string full = hit.Groups["method"].Value;
        int dot = full.LastIndexOf('.');
        return dot >= 0 && dot < full.Length - 1 ? full.Substring(dot + 1) : full;
    }
}
