using RimWorks.RimLogging;
using RimWorks.RimLogging.Dev;
using Xunit;

namespace RimWorks.RimLogging.Tests.Dev;

public class SeedEntryTests
{
    [Fact]
    public void At_EveryTwentyFifth_IsAnError()
    {
        Assert.Equal(LogLevel.Error, SeedEntry.At(24).Level);
        Assert.Equal(LogLevel.Error, SeedEntry.At(49).Level);
    }

    [Fact]
    public void At_EveryHundredth_IsFatal()
    {
        Assert.Equal(LogLevel.Fatal, SeedEntry.At(99).Level);
    }

    [Fact]
    public void At_ErrorsCarryAStackAndAmbientScope()
    {
        SeedEntry seed = SeedEntry.At(24);

        Assert.NotNull(seed.Stack);
        Assert.True(seed.Scoped);
    }

    [Fact]
    public void At_ChatterCarriesNoStack()
    {
        SeedEntry seed = SeedEntry.At(0);

        Assert.Null(seed.Stack);
        Assert.False(seed.Scoped);
    }

    [Fact]
    public void At_ABurstHoldsEnoughErrorsToNavigate()
    {
        int errors = 0;
        for (int i = 0; i < 400; i++)
        {
            if (SeedEntry.At(i).Level >= LogLevel.Error) errors++;
        }

        // next-error needs several to cycle through, not one
        Assert.Equal(16, errors);
    }

    [Fact]
    public void At_CoversEveryLevelSoGatingIsVisible()
    {
        bool trace = false, debug = false, info = false, warn = false;
        for (int i = 0; i < 100; i++)
        {
            switch (SeedEntry.At(i).Level)
            {
                case LogLevel.Trace: trace = true; break;
                case LogLevel.Debug: debug = true; break;
                case LogLevel.Info: info = true; break;
                case LogLevel.Warn: warn = true; break;
            }
        }

        Assert.True(trace && debug && info && warn, "a seed run must exercise every gate");
    }

    [Fact]
    public void At_SpreadsAcrossModsSoModFilteringIsTestable()
    {
        bool harmony = false, unattributed = false;
        for (int i = 0; i < 20; i++)
        {
            if (SeedEntry.At(i).Mod == "Harmony") harmony = true;
            if (SeedEntry.At(i).Mod == null) unattributed = true;
        }

        Assert.True(harmony && unattributed);
    }

    [Fact]
    public void At_ErrorsMentionExceptionSoTextFilteringIsTestable()
    {
        Assert.Contains("exception", SeedEntry.At(24).Message, System.StringComparison.OrdinalIgnoreCase);
    }
}
