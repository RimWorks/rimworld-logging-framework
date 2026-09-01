using System.Text;

namespace RimWorks.RimLogging.Viewer;

/// <summary>Builds the copyable text for one entry. Pure so it tests without Verse.</summary>
internal static class EntryText {
    internal static string Trace(LogEntry entry) {
        return entry.StackTrace ?? entry.Exception?.ToString() ?? string.Empty;
    }

    internal static string WithStack(LogEntry entry) {
        string trace = Trace(entry);
        return trace.Length == 0 ? entry.RenderedMessage : entry.RenderedMessage + "\n\n" + trace;
    }

    // field names stay English so a pasted report reads the same wherever it lands
    internal static string Full(LogEntry entry) {
        StringBuilder builder = new StringBuilder();
        builder.Append("Timestamp: ").AppendLine(entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        builder.Append("Level: ").AppendLine(entry.Level.ToString().ToUpperInvariant());
        builder.Append("Channel: ").AppendLine(entry.Channel);
        if (!string.IsNullOrEmpty(entry.Mod)) {
            builder.Append("Mod: ").AppendLine(entry.Mod);
        }
        if (entry.Source.IsCallerProvided) {
            builder.Append("Source: ").Append(entry.Source.File).Append(':').AppendLine(entry.Source.Line.ToString());
        }
        if (entry.Context != null) {
            foreach (KeyValuePair<string, object?> pair in entry.Context) {
                builder.Append(pair.Key).Append(": ").AppendLine(pair.Value?.ToString() ?? "null");
            }
        }

        builder.AppendLine().AppendLine(entry.RenderedMessage);

        string trace = Trace(entry);
        if (trace.Length > 0) {
            builder.AppendLine().Append(trace);
        }
        return builder.ToString().TrimEnd();
    }
}
