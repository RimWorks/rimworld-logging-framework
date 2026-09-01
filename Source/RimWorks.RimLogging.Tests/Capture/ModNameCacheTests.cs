using System;
using System.Collections.Generic;
using RimWorks.RimLogging.Capture;
using Xunit;

namespace RimWorks.RimLogging.Tests.Capture;

public class ModNameCacheTests : IDisposable
{
    public ModNameCacheTests() => ModNameCache.ClearForTests();

    public void Dispose()
    {
        ModNameCache.ClearForTests();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Map_NoProvider_ReturnsEmpty()
    {
        Assert.Empty(ModNameCache.Map());
    }

    [Fact]
    public void Map_CachesFirstNonEmptyResult_ProviderCalledOnce()
    {
        int calls = 0;
        ModNameCache.Provider = () =>
        {
            calls++;
            return new Dictionary<string, string> { ["Asm"] = "Mod" };
        };

        ModNameCache.Map();
        ModNameCache.Map();

        Assert.Equal(1, calls);
    }

    [Fact]
    public void Map_DoesNotCacheEmptyResult_RetriesUntilPopulated()
    {
        int calls = 0;
        ModNameCache.Provider = () =>
        {
            calls++;
            return calls < 2
                ? new Dictionary<string, string>()
                : new Dictionary<string, string> { ["Asm"] = "Mod" };
        };

        Assert.Empty(ModNameCache.Map());
        Assert.Equal("Mod", ModNameCache.Map()["Asm"]);
    }

    [Fact]
    public void Map_ThrowingProvider_ReturnsEmptyAndDoesNotCache()
    {
        ModNameCache.Provider = () => throw new InvalidOperationException("boom");

        Assert.Empty(ModNameCache.Map());
    }

    [Fact]
    public void Map_ThrowingProvider_InvokesOnProviderError()
    {
        Exception? captured = null;
        ModNameCache.OnProviderError = ex => captured = ex;
        InvalidOperationException expected = new("boom");
        ModNameCache.Provider = () => throw expected;

        ModNameCache.Map();

        Assert.Same(expected, captured);
    }

    [Fact]
    public void FolderMap_ThrowingProvider_InvokesOnProviderError()
    {
        Exception? captured = null;
        ModNameCache.OnProviderError = ex => captured = ex;
        InvalidOperationException expected = new("folder-boom");
        ModNameCache.FolderProvider = () => throw expected;

        ModNameCache.FolderMap();

        Assert.Same(expected, captured);
    }

    [Fact]
    public void Map_ThrowingProvider_DoesNotThrowWhenHookUnset()
    {
        ModNameCache.OnProviderError = null;
        ModNameCache.Provider = () => throw new InvalidOperationException("boom");

        Assert.Empty(ModNameCache.Map());
    }
}
