using System.Collections.Generic;
using System.Text;

namespace RimWorks.RimLogging.Bundle;

/// <summary>Renders a bundle as the plain text a paste host stores. Pure so it tests without the game.</summary>
internal static class BundleTextRenderer
{
    /// <summary>Returns the whole bundle as a readable report: versions, then mods, then the log.</summary>
    internal static string Render(BundlePayload payload)
    {
        if (payload == null) return string.Empty;

        StringBuilder sb = new StringBuilder();
        sb.Append("RimLogging bug bundle\n");
        sb.Append("RimWorld ").Append(Or(payload.RimWorldVersion, "unknown"))
          .Append(" / RimLogging ").Append(Or(payload.FrameworkVersion, "unknown")).Append('\n');

        AppendMods(sb, payload.Mods);
        AppendEntries(sb, payload.Entries);
        return sb.ToString();
    }

    private static void AppendMods(StringBuilder sb, List<BundlePayload.ModInfo> mods)
    {
        if (mods == null || mods.Count == 0)
        {
            sb.Append("\nMods: none recorded\n");
            return;
        }

        int active = 0;
        for (int i = 0; i < mods.Count; i++)
        {
            if (mods[i].Active) active++;
        }

        sb.Append('\n').Append("Mods: ").Append(mods.Count).Append(" loaded, ").Append(active).Append(" active\n");
        for (int i = 0; i < mods.Count; i++)
        {
            BundlePayload.ModInfo mod = mods[i];
            sb.Append("  ").Append(Or(mod.Name, "(unnamed)"));
            sb.Append(" [").Append(Or(mod.PackageId, "no package id")).Append(']');
            if (!string.IsNullOrEmpty(mod.Version)) sb.Append(' ').Append(mod.Version);
            if (!mod.Active) sb.Append(" (inactive)");
            sb.Append('\n');
        }
    }

    private static void AppendEntries(StringBuilder sb, List<BundlePayload.EntryDto> entries)
    {
        int count = entries == null ? 0 : entries.Count;
        sb.Append('\n').Append("Log: ").Append(count).Append(count == 1 ? " entry\n" : " entries\n");
        if (count == 0) return;

        for (int i = 0; i < entries!.Count; i++)
        {
            BundlePayload.EntryDto e = entries[i];
            // upper-cased for the paste host only: its log grammar keys off ALL CAPS levels.
            // Bundler.SerializeLevel stays mixed-case, the gist worker validates against that set.
            sb.Append(Or(e.Timestamp, "?")).Append("  ").Append(Or(e.Level, "?").ToUpperInvariant())
              .Append("  [").Append(Or(e.Channel, "?")).Append("]  ");

            // a multi-line message has to keep its tail indented, or the wrapped part lands at
            // column 0 and reads as a new entry
            string message = (e.Message ?? string.Empty).Replace("\r\n", "\n");
            int wrap = message.IndexOf('\n');
            sb.Append(wrap < 0 ? message : message.Substring(0, wrap));
            if (!string.IsNullOrEmpty(e.Source)) sb.Append("  (").Append(e.Source).Append(')');
            sb.Append('\n');
            if (wrap >= 0) sb.Append(Indent(message.Substring(wrap + 1)));

            AppendContext(sb, e.Context);
            if (!string.IsNullOrEmpty(e.Stack)) sb.Append(Indent(e.Stack!));
        }
    }

    private static void AppendContext(StringBuilder sb, Dictionary<string, object?>? context)
    {
        if (context == null || context.Count == 0) return;
        foreach (KeyValuePair<string, object?> pair in context)
        {
            sb.Append("    ").Append(pair.Key).Append('=').Append(pair.Value).Append('\n');
        }
    }

    private static string Indent(string block)
    {
        string[] lines = block.Replace("\r\n", "\n").Split('\n');
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Length == 0) continue;
            sb.Append("    ").Append(lines[i]).Append('\n');
        }
        return sb.ToString();
    }

    private static string Or(string? value, string fallback)
        => string.IsNullOrEmpty(value) ? fallback : value!;
}
