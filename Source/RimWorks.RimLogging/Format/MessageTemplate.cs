using System;
using System.Collections.Generic;

namespace RimWorks.RimLogging.Format;

/// <summary>Parsed structured-logging template with holes and literal segments.</summary>
public sealed class MessageTemplate
{
    /// <summary>Original string with {Name}-style holes.</summary>
    public string Raw { get; }

    /// <summary>Named placeholders in order of appearance.</summary>
    public IReadOnlyList<string> Holes { get; }

    /// <summary>Literal text between holes (Segments.Count == Holes.Count + 1 always).</summary>
    public IReadOnlyList<string> Segments { get; }

    /// <summary>Constructs a template from its raw text, parsed holes, and literal segments.</summary>
    /// <param name="raw">Original template string.</param>
    /// <param name="holes">Named placeholders in order of appearance.</param>
    /// <param name="segments">Literal text between holes; must contain one more element than <paramref name="holes"/>.</param>
    public MessageTemplate(string raw, IReadOnlyList<string> holes, IReadOnlyList<string> segments)
    {
        Raw = raw ?? throw new ArgumentNullException(nameof(raw));
        Holes = holes ?? throw new ArgumentNullException(nameof(holes));
        Segments = segments ?? throw new ArgumentNullException(nameof(segments));
    }

    /// <summary>Parses a raw template string into named holes and surrounding literal segments.</summary>
    /// <summary>Parses a raw template string into named holes and surrounding literal segments.</summary>
    public static MessageTemplate Parse(string raw)
    {
        if (raw == null) throw new ArgumentNullException(nameof(raw));
        if (raw.Length == 0) return new MessageTemplate(raw, Array.Empty<string>(), new[] { string.Empty });

        System.Text.StringBuilder seg = new System.Text.StringBuilder();
        List<string> holes = new List<string>();
        List<string> segments = new List<string>();
        int i = 0;
        while (i < raw.Length)
        {
            i = AppendNext(raw, i, seg, holes, segments);
        }
        segments.Add(seg.ToString());
        return new MessageTemplate(raw, holes, segments);
    }

    private static int AppendNext(string raw, int i, System.Text.StringBuilder seg, List<string> holes, List<string> segments)
    {
        char c = raw[i];
        if (IsEscapedBrace(raw, i)) { seg.Append(c); return i + 2; }
        if (c == '{') return ReadHole(raw, i, seg, holes, segments);
        seg.Append(c);
        return i + 1;
    }

    private static bool IsEscapedBrace(string raw, int i) =>
        (raw[i] == '{' || raw[i] == '}') && i + 1 < raw.Length && raw[i + 1] == raw[i];

    private static int ReadHole(string raw, int i, System.Text.StringBuilder seg, List<string> holes, List<string> segments)
    {
        int close = raw.IndexOf('}', i + 1);
        if (close < 0)
        {
            seg.Append(raw, i, raw.Length - i);
            return raw.Length;
        }
        string name = raw.Substring(i + 1, close - i - 1);
        if (name.Length == 0 || ContainsBrace(name))
        {
            seg.Append(raw, i, close - i + 1);
            return close + 1;
        }
        segments.Add(seg.ToString());
        seg.Clear();
        holes.Add(name);
        return close + 1;
    }

    /// <summary>Renders the template by substituting positional args into holes and returns the formatted string plus a name-to-value context dictionary.</summary>
    public (string Rendered, IReadOnlyDictionary<string, object?>? Context) Render(object?[] args)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder(Raw.Length + 16);
        Dictionary<string, object?>? ctx = Holes.Count > 0 ? new Dictionary<string, object?>(Holes.Count) : null;
        for (int i = 0; i < Holes.Count; i++)
        {
            sb.Append(Segments[i]);
            if (i < args.Length)
            {
                object? v = args[i];
                sb.Append(FormatArg(v));
                ctx![Holes[i]] = v;
            }
            else
            {
                sb.Append('{').Append(Holes[i]).Append('}');
            }
        }
        sb.Append(Segments[Holes.Count]);
        return (sb.ToString(), ctx);
    }

    private static string FormatArg(object? v) => v switch
    {
        null => string.Empty,
        string s => s,
        IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
        _ => v.ToString() ?? string.Empty,
    };

    private static bool ContainsBrace(string s)
    {
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '{' || s[i] == '}') return true;
        }
        return false;
    }
}
