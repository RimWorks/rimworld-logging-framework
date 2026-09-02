using System;
using System.Collections.Generic;

namespace RimWorks.RimLogging.Pipeline;

/// <summary>
/// Keyed suppression so a caller in a tick loop writes one entry instead of sixty a second.
/// Instantiable rather than static so tests do not share a table, and the clock is passed in.
/// </summary>
internal sealed class LogThrottle
{
    private readonly object gate = new object();
    private readonly Dictionary<string, DateTime> lastFired = new Dictionary<string, DateTime>(StringComparer.Ordinal);

    /// <summary>Returns <c>true</c> the first time a key is offered, and <c>false</c> after that.</summary>
    internal bool Once(string? key, DateTime now) => Every(key, TimeSpan.MaxValue, now);

    /// <summary>Returns <c>true</c> when the key has not fired inside <paramref name="interval"/>.</summary>
    internal bool Every(string? key, TimeSpan interval, DateTime now)
    {
        // an unkeyed call cannot be tracked, so it always passes rather than silently vanishing
        if (string.IsNullOrEmpty(key)) return true;

        lock (gate)
        {
            if (lastFired.TryGetValue(key!, out DateTime last) && now - last < interval) return false;
            lastFired[key!] = now;
            return true;
        }
    }
}
