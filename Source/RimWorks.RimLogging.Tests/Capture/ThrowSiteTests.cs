using RimWorks.RimLogging.Capture;
using Xunit;

namespace RimWorks.RimLogging.Tests.Capture;

/// <summary>
/// Guards throw-site extraction. A captured exception is logged by whoever caught it, so without
/// this the Source column names vanilla's handler instead of the code that actually failed.
/// </summary>
public class ThrowSiteTests
{
    private const string MonoTrace = @"Exception filling window for LudeonTK.Dialog_Debug: System.InvalidOperationException: RimLogging stack probe
  at RimWorks.RimLogging.Dev.StackProbeDebugAction.ThrowStackProbe () [0x00000] in /home/aaron/projects/RimWorks/rimworld-logging-framework/Source/RimWorks.RimLogging/Dev/StackProbeDebugAction.cs:15
  at LudeonTK.DebugActionNode.Enter (LudeonTK.Dialog_Debug dialog) [0x00065] in <b4d967f0d45a413fbb0223eefee4f2ae>:0
  at Verse.Window.InnerWindowOnGUI (System.Int32 x) [0x001a6] in <b4d967f0d45a413fbb0223eefee4f2ae>:0 ";

    [Fact]
    public void TheFirstFrameWithRealSymbolsWins()
    {
        SourceLocation? site = ThrowSite.From(MonoTrace);

        Assert.NotNull(site);
        Assert.EndsWith("Dev/StackProbeDebugAction.cs", site.Value.File);
        Assert.Equal(15, site.Value.Line);
        Assert.Equal("ThrowStackProbe", site.Value.Method);
    }

    [Fact]
    public void FramesWithoutSymbolsAreSkipped()
    {
        // "<mvid>:0" is how Mono renders a frame from an assembly that shipped no pdb
        string trace = @"  at Verse.Window.InnerWindowOnGUI (System.Int32 x) [0x001a6] in <b4d967f0d45a413fbb0223eefee4f2ae>:0
  at Mod.Real.Thing (System.Int32 x) [0x1] in /src/Thing.cs:42 ";

        SourceLocation? site = ThrowSite.From(trace);

        Assert.Equal("/src/Thing.cs", site!.Value.File);
        Assert.Equal(42, site.Value.Line);
    }

    [Fact]
    public void ATraceWithNoSymbolsAtAllYieldsNothing()
    {
        string trace = @"  at Verse.Window.InnerWindowOnGUI (System.Int32 x) [0x001a6] in <b4d967f0d45a413fbb0223eefee4f2ae>:0
  at UnityEngine.GUI.CallWindowDelegate () [0x0] in <b4d967f0d45a413fbb0223eefee4f2ae>:0 ";

        Assert.Null(ThrowSite.From(trace));
    }

    [Fact]
    public void TheDotNetSpellingIsRead()
    {
        string trace = "   at RimObs.Hosting.BrowserLauncher.Open(String url) in /home/a/RimObs/Hosting/BrowserLauncher.cs:line 17";

        SourceLocation? site = ThrowSite.From(trace);

        Assert.Equal("/home/a/RimObs/Hosting/BrowserLauncher.cs", site!.Value.File);
        Assert.Equal(17, site.Value.Line);
        Assert.Equal("Open", site.Value.Method);
    }

    [Fact]
    public void OurOwnFormattedShapeIsRead()
    {
        // StackWalker.FormatTrace writes "at T.M (Path/File:15)"
        SourceLocation? site = ThrowSite.From("at Concord.Coex.Hook.Install (Concord.Detour/Hook.cs:120)");

        Assert.Equal("Concord.Detour/Hook.cs", site!.Value.File);
        Assert.Equal(120, site.Value.Line);
        Assert.Equal("Install", site.Value.Method);
    }

    [Fact]
    public void ALineNumberOfZeroIsNotASite()
    {
        Assert.Null(ThrowSite.From("  at Thing.Do () [0x0] in /src/Thing.cs:0 "));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no frames here at all")]
    [InlineData("Exception of type 'System.Exception' was thrown.")]
    public void NothingResolvableYieldsNull(string? trace)
    {
        Assert.Null(ThrowSite.From(trace));
    }
}
