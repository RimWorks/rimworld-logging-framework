using System;
using System.Collections.Generic;
using RimWorks.RimLogging.Format;
using Xunit;

namespace RimWorks.RimLogging.Tests.Format;

public class StructuredContextTests
{
    [Fact]
    public void Capture_NullObject_ReturnsNull()
    {
        IReadOnlyDictionary<string, object?>? result = StructuredContext.Capture(null);
        Assert.Null(result);
    }

    [Fact]
    public void Capture_AnonymousObject_ReturnsKeyedDictionary()
    {
        IReadOnlyDictionary<string, object?>? result = StructuredContext.Capture(new { foo = 1, bar = "x" });
        Assert.NotNull(result);
        Assert.Equal(1, result["foo"]);
        Assert.Equal("x", result["bar"]);
    }

    [Fact]
    public void Capture_SameAnonymousType_ReusesCachedAccessor()
    {
        int before = ContextReflector.CacheSize;
        StructuredContext.Capture(new { cacheTest = 42 });
        StructuredContext.Capture(new { cacheTest = 99 });
        int delta = ContextReflector.CacheSize - before;
        Assert.Equal(1, delta);
    }

    [Fact]
    public void Capture_PropertyNames_PreserveCase()
    {
        IReadOnlyDictionary<string, object?>? result = StructuredContext.Capture(new { FooBar = 1, BAZ = 2 });
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("FooBar"));
        Assert.True(result.ContainsKey("BAZ"));
    }

    [Fact]
    public void Capture_Exception_RoundTripsReference()
    {
        Exception ex = new Exception("boom");
        IReadOnlyDictionary<string, object?>? result = StructuredContext.Capture(new { ex });
        Assert.NotNull(result);
        Assert.Same(ex, result["ex"]);
    }
}
