using System.Collections.Generic;
using RimWorks.RimLogging.Capture;
using Xunit;

namespace RimWorks.RimLogging.Tests.Capture;

public class ModResolutionTests
{
    private static Dictionary<string, string> Map(params (string Asm, string Mod)[] pairs)
    {
        Dictionary<string, string> map = new Dictionary<string, string>();
        foreach ((string asm, string mod) in pairs) map[asm] = mod;
        return map;
    }

    [Fact]
    public void ResolveFromPath_PathThroughKnownAssemblyDir_StripsToTailAndResolvesMod()
    {
        IReadOnlyDictionary<string, string> map = Map(("RimWorks.RimLogging", "RimLogging"));
        string file = "/home/aaron/projects/RimWorks/rimworld-logging-framework/RimWorks.RimLogging/Bootstrap/StaticInit.cs";

        (string path, string? mod) = ModResolution.ResolveFromPath(file, map);

        Assert.Equal("Bootstrap/StaticInit", path);
        Assert.Equal("RimLogging", mod);
    }

    [Fact]
    public void ResolveFromPath_WindowsSeparators_AreHandled()
    {
        IReadOnlyDictionary<string, string> map = Map(("MyMod", "My Mod"));
        string file = @"C:\dev\src\MyMod\Sub\Thing.cs";

        (string path, string? mod) = ModResolution.ResolveFromPath(file, map);

        Assert.Equal("Sub/Thing", path);
        Assert.Equal("My Mod", mod);
    }

    [Fact]
    public void ResolveFromPath_MatchesLastSegmentWhenAssemblyNameRepeats()
    {
        IReadOnlyDictionary<string, string> map = Map(("MyMod", "My Mod"));
        string file = "/repos/MyMod/vendor/MyMod/Core/Foo.cs";

        (string path, string? mod) = ModResolution.ResolveFromPath(file, map);

        Assert.Equal("Core/Foo", path);
        Assert.Equal("My Mod", mod);
    }

    [Fact]
    public void ResolveFromPath_NoSegmentMatches_FallsBackToNormalizePathWithNoMod()
    {
        IReadOnlyDictionary<string, string> map = Map(("SomeOtherAsm", "Other"));
        string file = @"C:\RimWorld\Mods\Foo\Source\Bar.cs";

        (string path, string? mod) = ModResolution.ResolveFromPath(file, map);

        Assert.Equal(StackWalker.NormalizePath(file), path);
        Assert.Null(mod);
    }

    [Fact]
    public void ResolveFromPath_EmptyMap_FallsBack()
    {
        string file = "/x/MyMod/Foo.cs";

        (string path, string? mod) = ModResolution.ResolveFromPath(file, Map());

        Assert.Equal(StackWalker.NormalizePath(file), path);
        Assert.Null(mod);
    }
}
