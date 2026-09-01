using RimWorks.RimLogging.Format;
using Xunit;

namespace RimWorks.RimLogging.Tests.Format;

public class TemplateCacheTests
{
    [Fact]
    public void Get_SameRaw_ReturnsSameInstance()
    {
        MessageTemplate first = TemplateCache.Get("hi {Name}");
        MessageTemplate second = TemplateCache.Get("hi {Name}");

        Assert.Same(first, second);
    }

    [Fact]
    public void Get_DifferentRaw_ProducesDifferentEntries()
    {
        MessageTemplate a = TemplateCache.Get("template {A}");
        MessageTemplate b = TemplateCache.Get("template {B}");

        Assert.NotSame(a, b);
        Assert.Equal("template {A}", a.Raw);
        Assert.Equal("template {B}", b.Raw);
    }

    [Fact]
    public void Get_HandlesLargeUniqueLoad_NoExceptions()
    {
        const int count = 10_000;
        System.Collections.Generic.HashSet<MessageTemplate> results = new(count, ReferenceEqualityComparer.Instance);

        for (int i = 0; i < count; i++)
        {
            results.Add(TemplateCache.Get($"t-{i}"));
        }

        Assert.Equal(count, results.Count);
    }
}
