using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace RimWorks.RimLogging.Format;

/// <summary>Caches per-type property accessors for anonymous-object reflection.</summary>
internal static class ContextReflector
{
    private static readonly ConcurrentDictionary<Type, PropertyAccessor[]> _cache = new ConcurrentDictionary<Type, PropertyAccessor[]>();

    /// <summary>A cached accessor for a single property.</summary>
    internal readonly struct PropertyAccessor
    {
        /// <summary>Property name as declared on the type.</summary>
        internal readonly string Name;

        /// <summary>Delegate that reads the property value from an instance.</summary>
        internal readonly Func<object, object?> Get;

        internal PropertyAccessor(string name, Func<object, object?> get)
        {
            Name = name;
            Get = get;
        }
    }

    /// <summary>Returns cached accessors for all public instance properties of <paramref name="type"/>.</summary>
    internal static PropertyAccessor[] GetAccessors(Type type)
        => _cache.GetOrAdd(type, BuildAccessors);

    private static PropertyAccessor[] BuildAccessors(Type t)
    {
        PropertyInfo[] props = t.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        PropertyAccessor[] result = new PropertyAccessor[props.Length];
        for (int i = 0; i < props.Length; i++)
        {
            PropertyInfo p = props[i];
            result[i] = new PropertyAccessor(p.Name, obj => p.GetValue(obj));
        }
        return result;
    }

    /// <summary>Number of types currently in the accessor cache. For testing.</summary>
    internal static int CacheSize => _cache.Count;
}
