using System;
using RimWorks.RimLogging.Pipeline;
using Xunit;

namespace RimWorks.RimLogging.Tests.Pipeline;

public class LogThrottleTests
{
    private static readonly DateTime T0 = new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Once_FirstCallPasses_AndEveryLaterOneIsSuppressed()
    {
        LogThrottle t = new LogThrottle();

        Assert.True(t.Once("boom", T0));
        Assert.False(t.Once("boom", T0.AddSeconds(1)));
        Assert.False(t.Once("boom", T0.AddDays(400)));
    }

    [Fact]
    public void Once_DistinctKeysDoNotSuppressEachOther()
    {
        LogThrottle t = new LogThrottle();

        Assert.True(t.Once("a", T0));
        Assert.True(t.Once("b", T0));
    }

    [Fact]
    public void Every_SuppressesInsideTheInterval()
    {
        LogThrottle t = new LogThrottle();
        t.Every("k", TimeSpan.FromSeconds(10), T0);

        Assert.False(t.Every("k", TimeSpan.FromSeconds(10), T0.AddSeconds(9)));
    }

    [Fact]
    public void Every_AtTheBoundary_FiresAgain()
    {
        LogThrottle t = new LogThrottle();
        t.Every("k", TimeSpan.FromSeconds(10), T0);

        Assert.True(t.Every("k", TimeSpan.FromSeconds(10), T0.AddSeconds(10)));
    }

    [Fact]
    public void Every_ResetsTheWindowEachTimeItFires()
    {
        LogThrottle t = new LogThrottle();
        TimeSpan ten = TimeSpan.FromSeconds(10);
        t.Every("k", ten, T0);
        t.Every("k", ten, T0.AddSeconds(10));

        Assert.False(t.Every("k", ten, T0.AddSeconds(15)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Every_UntrackableKey_AlwaysPasses(string? key)
    {
        LogThrottle t = new LogThrottle();

        Assert.True(t.Every(key, TimeSpan.FromHours(1), T0));
        Assert.True(t.Every(key, TimeSpan.FromHours(1), T0));
    }
}
