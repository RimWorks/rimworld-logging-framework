using System.Collections.Generic;

namespace RimWorks.RimLogging.Format;

/// <summary>Renders log entries against a token-based format template (tokens: ts, level, channel, mod, source, message, ctx, exc).</summary>
public static class DefaultFormat
{
    /// <summary>The default format template applied when a channel specifies no override.</summary>
    public const string Default = "[{ts}] [{level}] [{channel}] [{source}] {message}{ctx}{exc}";

    /// <summary>Renders the full template for the given entry, substituting all recognized tokens.</summary>
    /// <param name="template">The format template string.</param>
    /// <param name="entry">The log entry supplying token values.</param>
    /// <param name="stripRichText">When <c>true</c>, rich-text tags are stripped from the message token.</param>
    /// <returns>The fully rendered line.</returns>
    public static string Render(string template, LogEntry entry, bool stripRichText)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder(template.Length + entry.RenderedMessage.Length + 64);
        int cursor = 0;
        foreach ((int open, int close, string token) in ScanTokens(template))
        {
            if (open > cursor) sb.Append(template, cursor, open - cursor);
            string resolved = ResolveToken(token, entry, stripRichText);
            if (resolved.Length == 0 && TryConsumeEmptyBracketGroup(template, open, close, sb, out int advance))
            {
                cursor = advance;
                continue;
            }
            sb.Append(resolved);
            cursor = close + 1;
        }
        if (cursor < template.Length) sb.Append(template, cursor, template.Length - cursor);
        return sb.ToString();
    }

    /// <summary>
    /// Eats the <c>[ ]</c> around an empty token, plus one trailing space, so an optional
    /// field leaves nothing behind instead of a stray <c>[]</c>.
    /// </summary>
    private static bool TryConsumeEmptyBracketGroup(string template, int openBrace, int closeBrace, System.Text.StringBuilder sb, out int advance)
    {
        advance = 0;
        if (openBrace == 0 || closeBrace + 1 >= template.Length) return false;
        if (template[openBrace - 1] != '[' || template[closeBrace + 1] != ']') return false;
        if (sb.Length == 0 || sb[sb.Length - 1] != '[') return false;
        sb.Length -= 1;
        int next = closeBrace + 2;
        if (next < template.Length && template[next] == ' ') next += 1;
        advance = next;
        return true;
    }

    /// <summary>
    /// Renders everything before <c>{message}</c>, including the literal text just before it.
    /// Returns the whole rendered template when there is no <c>{message}</c>.
    /// </summary>
    /// <param name="template">The format template string.</param>
    /// <param name="entry">The log entry supplying token values.</param>
    /// <param name="stripRichText">When <c>true</c>, rich-text tags are stripped from token values.</param>
    /// <returns>The rendered prefix up to (but excluding) the <c>{message}</c> token, or the full template if no such token exists.</returns>
    public static string RenderPrefixOnly(string template, LogEntry entry, bool stripRichText)
    {
        (int open, _) = FindToken(template, "message");
        string prefix = open < 0 ? template : template.Substring(0, open);
        return Render(prefix, entry, stripRichText);
    }

    /// <summary>
    /// Renders everything after <c>{message}</c>, which is where <c>{ctx}</c> and <c>{exc}</c>
    /// sit in the default template. Returns empty when the template has no <c>{message}</c>.
    /// </summary>
    /// <param name="template">The format template string.</param>
    /// <param name="entry">The log entry supplying token values.</param>
    /// <param name="stripRichText">When <c>true</c>, rich-text tags are stripped from token values.</param>
    /// <returns>The rendered tail after the <c>{message}</c> token.</returns>
    public static string RenderSuffixOnly(string template, LogEntry entry, bool stripRichText)
    {
        (_, int close) = FindToken(template, "message");
        return close < 0 ? string.Empty : Render(template.Substring(close + 1), entry, stripRichText);
    }

    /// <summary>
    /// Builds a line whose prefix is wrapped in a Unity colour tag and whose message and tail
    /// are not, for sinks that colour the prefix alone.
    /// </summary>
    /// <param name="template">The format template string.</param>
    /// <param name="entry">The log entry supplying token values.</param>
    /// <param name="colorHex">Six-digit hex colour applied to the prefix.</param>
    /// <returns>The rendered line, exception and context included.</returns>
    public static string RenderWithColoredPrefix(string template, LogEntry entry, string colorHex)
        => "<color=#" + colorHex + ">"
           + RenderPrefixOnly(template, entry, stripRichText: false)
           + "</color>"
           + entry.RenderedMessage
           + RenderSuffixOnly(template, entry, stripRichText: false);

    /// <summary>
    /// Returns the brace positions of the <c>{token}</c> occurrence, or (-1, -1) if absent.
    /// Tokenizes through <see cref="ScanTokens"/> so only a whole-token match counts.
    /// </summary>
    private static (int Open, int Close) FindToken(string template, string token)
    {
        foreach ((int open, int close, string t) in ScanTokens(template))
            if (t == token) return (open, close);
        return (-1, -1);
    }

    /// <summary>
    /// Yields every well-formed <c>{token}</c> in order, stopping at the first unclosed brace.
    /// <see cref="Render"/> and <see cref="IndexOfToken"/> share it so they cannot drift apart.
    /// </summary>
    private static IEnumerable<(int OpenIndex, int CloseIndex, string Token)> ScanTokens(string template)
    {
        int i = 0;
        while (i < template.Length)
        {
            int open = template.IndexOf('{', i);
            if (open < 0) yield break;
            int close = template.IndexOf('}', open + 1);
            if (close < 0) yield break;
            yield return (open, close, template.Substring(open + 1, close - open - 1));
            i = close + 1;
        }
    }

    private static string ResolveToken(string token, LogEntry e, bool strip)
    {
        switch (token)
        {
            case "ts": return e.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
            case "level": return e.Level.ToString().ToUpperInvariant();
            case "channel": return e.Channel;
            case "mod": return e.Mod ?? string.Empty;
            case "source": return e.Source.IsCallerProvided ? e.Source.File + ":" + e.Source.Line : string.Empty;
            case "message": return strip ? RichText.Strip(e.RenderedMessage) : e.RenderedMessage;
            case "ctx": return RenderUnconsumedContext(e);
            case "exc": return e.Exception != null ? "\n" + e.Exception.ToString() : string.Empty;
            default: return "{" + token + "}";
        }
    }

    private static string RenderUnconsumedContext(LogEntry e)
    {
        if (e.Context == null || e.Context.Count == 0) return string.Empty;
        MessageTemplate t = TemplateCache.Get(e.MessageTemplate);
        bool any = false;
        System.Text.StringBuilder sb = new System.Text.StringBuilder(" {");
        foreach (KeyValuePair<string, object?> kv in e.Context)
        {
            if (t.Holes.Contains(kv.Key)) continue;
            if (any) sb.Append(", ");
            sb.Append(kv.Key).Append('=').Append(kv.Value);
            any = true;
        }
        sb.Append('}');
        return any ? sb.ToString() : string.Empty;
    }
}
