using System;
using System.Collections.Generic;
using RimWorks.RimLogging.Pipeline;
using Xunit;

namespace RimWorks.RimLogging.Tests.Pipeline;

public class TimedScopeTests : LogSinkFixtureBase
{
    public override void Dispose()
    {
        TimedScope.TimestampProvider = null;
        base.Dispose();
    }

    // drives the monotonic clock so elapsed_ms is exact instead of whatever the test machine did
    private static void ClockAdvances(params long[] readings)
    {
        int index = 0;
        TimedScope.TimestampProvider = () => readings[index < readings.Length ? index++ : readings.Length - 1];
    }

    private LogEntry? Last() => _sink.Entries.Count > 0 ? _sink.Entries[_sink.Entries.Count - 1] : null;

    [Fact]
    public void Dispose_EmitsOnceWithTheMessageAndLevel()
    {
        using (Log.Timed("timed-sentinel"))
        {
        }

        LogEntry? entry = Last();
        Assert.NotNull(entry);
        Assert.Equal("timed-sentinel", entry!.RenderedMessage);
        Assert.Equal(LogLevel.Debug, entry.Level);
        Assert.Equal("default", entry.Channel);
    }

    [Fact]
    public void Dispose_PutsElapsedMillisecondsInContext()
    {
        ClockAdvances(0, System.Diagnostics.Stopwatch.Frequency / 2);

        using (Log.Timed("half a second"))
        {
        }

        IReadOnlyDictionary<string, object?>? ctx = Last()!.Context;
        Assert.NotNull(ctx);
        Assert.Equal(500d, Assert.IsType<double>(ctx!["elapsed_ms"]));
    }

    [Fact]
    public void Dispose_RoundsElapsedToOneDecimalPlace()
    {
        ClockAdvances(0, System.Diagnostics.Stopwatch.Frequency / 3);

        using (Log.Timed("a third"))
        {
        }

        double ms = (double)Last()!.Context!["elapsed_ms"]!;
        Assert.Equal(333.3, ms);
    }

    [Fact]
    public void Dispose_CalledTwice_EmitsOnce()
    {
        TimedScope scope = Log.Timed("double dispose");
        scope.Dispose();
        int countAfterFirst = _sink.Entries.Count;

        scope.Dispose();

        Assert.Equal(countAfterFirst, _sink.Entries.Count);
    }

    [Fact]
    public void TimedTo_UsesTheNamedChannelAndLevel()
    {
        using (Log.TimedTo("MPF.Worldgen", "river pass", LogLevel.Info))
        {
        }

        LogEntry? entry = Last();
        Assert.NotNull(entry);
        Assert.Equal("MPF.Worldgen", entry!.Channel);
        Assert.Equal(LogLevel.Info, entry.Level);
    }

    [Fact]
    public void Timed_BelowTheGlobalMinimum_EmitsNothingAndNeverReadsTheClock()
    {
        Logging.GlobalMinLevel = LogLevel.Warn;
        bool clockRead = false;
        TimedScope.TimestampProvider = () => { clockRead = true; return 0; };
        int before = _sink.Entries.Count;

        using (Log.Timed("gated out", LogLevel.Debug))
        {
        }

        Assert.Equal(before, _sink.Entries.Count);
        Assert.False(clockRead);
    }

    [Fact]
    public void Timed_BelowTheGlobalMinimum_AllocatesNothing()
    {
        Logging.GlobalMinLevel = LogLevel.Warn;
        // warm the path so first-call JIT does not count against the measurement
        using (Log.Timed("warmup", LogLevel.Debug))
        {
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
        {
            using (Log.Timed("gated out", LogLevel.Debug))
            {
            }
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void Dispose_StillEmitsWhenTheBlockThrows()
    {
        int before = _sink.Entries.Count;

        Assert.Throws<InvalidOperationException>((Action)(() =>
        {
            using (Log.Timed("threw partway"))
            {
                throw new InvalidOperationException("boom");
            }
        }));

        Assert.Equal(before + 1, _sink.Entries.Count);
        Assert.Equal("threw partway", Last()!.RenderedMessage);
    }
}
