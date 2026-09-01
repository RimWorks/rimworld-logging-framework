using System.Collections.Generic;
using RimWorks.RimLogging.Patching;
using Xunit;

namespace RimWorks.RimLogging.Tests.Patching;

public class BackendChoiceTests
{
    private sealed class FakeBackend(string name, int priority) : IPatchBackend
    {
        public string Name { get; } = name;

        public int Priority { get; } = priority;

        public void Apply()
        {
        }
    }

    [Fact]
    public void Ranked_BothLibrariesLoaded_PutsConcordFirst()
    {
        List<IPatchBackend> ranked = BackendChoice.Ranked([
            new FakeBackend("Harmony", BackendPriority.Harmony),
            new FakeBackend("Concord", BackendPriority.Concord),
        ]);

        Assert.Equal(["Concord", "Harmony"], ranked.ConvertAll(b => b.Name));
    }

    [Fact]
    public void Ranked_ConcordListedFirstAlready_KeepsItFirst()
    {
        List<IPatchBackend> ranked = BackendChoice.Ranked([
            new FakeBackend("Concord", BackendPriority.Concord),
            new FakeBackend("Harmony", BackendPriority.Harmony),
        ]);

        Assert.Equal(["Concord", "Harmony"], ranked.ConvertAll(b => b.Name));
    }

    [Fact]
    public void Ranked_OnlyHarmonyLoaded_StillReturnsIt()
    {
        List<IPatchBackend> ranked = BackendChoice.Ranked([new FakeBackend("Harmony", BackendPriority.Harmony)]);

        Assert.Equal(["Harmony"], ranked.ConvertAll(b => b.Name));
    }

    [Fact]
    public void Ranked_NoBackendLoaded_IsEmptyRatherThanNull()
    {
        Assert.Empty(BackendChoice.Ranked([]));
    }

    [Fact]
    public void Ranked_ConcordOutranksHarmony()
    {
        Assert.True(BackendPriority.Concord > BackendPriority.Harmony);
    }
}
