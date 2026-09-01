using System;
using System.Collections.Concurrent;

namespace RimWorks.RimLogging.Format;

/// <summary>Thread-safe cache of parsed MessageTemplate instances keyed by raw string.</summary>
internal static class TemplateCache
{
    private static readonly ConcurrentDictionary<string, MessageTemplate> _cache = new(StringComparer.Ordinal);

    /// <summary>Returns the cached MessageTemplate for <paramref name="raw"/>, parsing it on first use.</summary>
    internal static MessageTemplate Get(string raw)
        => _cache.GetOrAdd(raw, MessageTemplate.Parse);
}
