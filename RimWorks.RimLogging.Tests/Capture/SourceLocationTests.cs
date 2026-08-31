using RimWorks.RimLogging.Capture;
using Xunit;

namespace RimWorks.RimLogging.Tests.Capture;

public class SourceLocationTests
{
    [Fact]
    public void IsCallerProvided_TrueWhenLineAndFileSet()
    {
        SourceLocation loc = new SourceLocation("Foo.cs", 42, null);
        Assert.True(loc.IsCallerProvided);
    }

    [Fact]
    public void IsCallerProvided_FalseWhenLineZeroAndFileEmpty()
    {
        SourceLocation loc = new SourceLocation("", 0, null);
        Assert.False(loc.IsCallerProvided);
    }

    [Fact]
    public void Empty_IsNotCallerProvided()
    {
        Assert.False(SourceLocation.Empty.IsCallerProvided);
    }
}
