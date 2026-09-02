using System;
using RimWorks.RimLogging;
using Xunit;

namespace RimWorks.RimLogging.Tests;

public class TickProviderTests : IDisposable
{
    public void Dispose()
    {
        Logging.TickProvider = null;
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void CurrentTick_NoProvider_IsNull()
    {
        Logging.TickProvider = null;

        Assert.Null(Logging.CurrentTick());
    }

    [Fact]
    public void CurrentTick_ReturnsWhatTheProviderGives()
    {
        Logging.TickProvider = () => 91422;

        Assert.Equal(91422, Logging.CurrentTick());
    }

    [Fact]
    public void CurrentTick_ProviderReturningNull_IsNull()
    {
        Logging.TickProvider = () => null;

        Assert.Null(Logging.CurrentTick());
    }

    [Fact]
    public void CurrentTick_ThrowingProvider_DoesNotEscape()
    {
        // this runs on every emit, so a bad provider must not take down the caller.
        // Find.TickManager throws on the main menu, which is exactly how this bit.
        Logging.TickProvider = () => throw new NullReferenceException();

        Assert.Null(Logging.CurrentTick());
    }

    [Fact]
    public void Emit_WithAThrowingProvider_StillLogs()
    {
        Logging.TickProvider = () => throw new InvalidOperationException();

        Exception? caught = Record.Exception(() => Log.Info("still logs"));

        Assert.Null(caught);
    }
}
