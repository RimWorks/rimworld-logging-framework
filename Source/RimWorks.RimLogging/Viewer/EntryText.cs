using System.Text;

namespace RimWorks.RimLogging.Viewer;

/// <summary>Builds the copyable text for one entry. Pure so it tests without Verse.</summary>
internal static class EntryText
{
    /// <summary>
    /// Splits a message into the single line a list row can show and the number of lines left
    /// behind. The count is returned separately so the row can style it apart from the message.
    /// </summary>
    internal static (string Head, int Extra) SplitRow(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return (string.Empty, 0);
        }

        int firstBreak = message.IndexOf('\n');
        if (firstBreak < 0)
        {
            return (message, 0);
        }

        int extra = 0;
        for (int i = firstBreak + 1; i < message.Length; i++)
        {
            if (message[i] == '\n')
            {
                extra++;
            }
        }
        // a trailing newline closes the last line rather than starting another
        if (message[message.Length - 1] != '\n')
        {
            extra++;
        }

        return (message.Substring(0, firstBreak).TrimEnd('\r'), extra);
    }

    /// <summary>The label shown beside a collapsed row, for example <c>(+12 lines)</c>.</summary>
    internal static string ExtraLinesLabel(int extra)
        => extra == 1 ? "(+1 line)" : "(+" + extra + " lines)";

    internal static string Trace(LogEntry entry)
    {
        return entry.StackTrace ?? entry.Exception?.ToString() ?? string.Empty;
    }

    internal static string WithStack(LogEntry entry)
    {
        string trace = Trace(entry);
        return trace.Length == 0 ? entry.RenderedMessage : entry.RenderedMessage + "\n\n" + trace;
    }

    // field names stay English so a pasted report reads the same wherever it lands
    internal static string Full(LogEntry entry)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("Timestamp: ").AppendLine(entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        builder.Append("Level: ").AppendLine(entry.Level.ToString().ToUpperInvariant());
        builder.Append("Channel: ").AppendLine(entry.Channel);
        if (!string.IsNullOrEmpty(entry.Mod))
        {
            builder.Append("Mod: ").AppendLine(entry.Mod);
        }
        if (entry.PatchedBy == null)
        {
            builder.AppendLine("Patched by: unavailable, this patch library cannot report owners");
        }
        else if (entry.PatchedBy.Count > 0)
        {
            builder.Append("Patched by: ").AppendLine(string.Join(", ", entry.PatchedBy));
        }
        if (entry.Tick.HasValue)
        {
            builder.Append("Tick: ").AppendLine(entry.Tick.Value.ToString());
        }
        if (entry.Source.IsCallerProvided)
        {
            builder.Append("Source: ").Append(entry.Source.File).Append(':').AppendLine(entry.Source.Line.ToString());
        }
        if (entry.Context != null)
        {
            foreach (KeyValuePair<string, object?> pair in entry.Context)
            {
                builder.Append(pair.Key).Append(": ").AppendLine(pair.Value?.ToString() ?? "null");
            }
        }

        builder.AppendLine().AppendLine(entry.RenderedMessage);

        string trace = Trace(entry);
        if (trace.Length > 0)
        {
            builder.AppendLine().Append(trace);
        }
        return builder.ToString().TrimEnd();
    }
}
