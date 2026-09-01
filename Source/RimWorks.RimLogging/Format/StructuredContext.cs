using System.Collections.Generic;

namespace RimWorks.RimLogging.Format;

/// <summary>Captures an anonymous object's properties into a keyed dictionary for structured logging.</summary>
public static class StructuredContext
{
    /// <summary>
    /// Reflects <paramref name="obj"/>'s public instance properties into a read-only dictionary.
    /// Returns <see langword="null"/> when <paramref name="obj"/> is <see langword="null"/> or has no properties.
    /// </summary>
    public static IReadOnlyDictionary<string, object?>? Capture(object? obj)
    {
        if (obj == null) return null;
        ContextReflector.PropertyAccessor[] accessors = ContextReflector.GetAccessors(obj.GetType());
        if (accessors.Length == 0) return null;
        Dictionary<string, object?> d = new Dictionary<string, object?>(accessors.Length);
        for (int i = 0; i < accessors.Length; i++)
        {
            d[accessors[i].Name] = accessors[i].Get(obj);
        }
        return d;
    }
}
