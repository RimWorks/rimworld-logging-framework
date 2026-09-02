using System.Collections.Generic;
using System.Text;

namespace RimWorks.RimLogging.Bundle;

/// <summary>Trims bundle text to fit a paste host's byte budget. Pure so it tests without the game.</summary>
internal static class BundleTrimmer
{
    private static readonly Encoding Utf8 = new UTF8Encoding(false);

    /// <summary>
    /// Returns <paramref name="content"/> unchanged when it fits, otherwise a copy keeping the
    /// header and the newest lines with a marker naming how many were dropped.
    /// </summary>
    internal static string Cap(string content, int maxBytes)
    {
        if (content == null) return string.Empty;
        if (maxBytes <= 0) return string.Empty;
        if (Utf8.GetByteCount(content) <= maxBytes) return content;

        string[] lines = content.Replace("\r\n", "\n").Split('\n');
        int headBudget = maxBytes / 5;

        List<string> head = new List<string>();
        int headBytes = 0;
        int line = 0;
        for (; line < lines.Length; line++)
        {
            int cost = Utf8.GetByteCount(lines[line]) + 1;
            if (headBytes + cost > headBudget) break;
            head.Add(lines[line]);
            headBytes += cost;
        }

        List<string> tail = new List<string>();
        int tailBytes = 0;
        int tailBudget = maxBytes - headBytes - 256;
        int back = lines.Length - 1;
        for (; back >= line; back--)
        {
            int cost = Utf8.GetByteCount(lines[back]) + 1;
            if (tailBytes + cost > tailBudget) break;
            tail.Insert(0, lines[back]);
            tailBytes += cost;
        }

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < head.Count; i++) sb.Append(head[i]).Append('\n');
        sb.Append("... ").Append(back - line + 1).Append(" lines dropped to fit the upload limit ...\n");
        for (int i = 0; i < tail.Count; i++) sb.Append(tail[i]).Append('\n');
        return sb.ToString();
    }
}
