using System;
using System.Collections.Generic;
using RimWorks.RimLogging.Pipeline;
using Xunit;

namespace RimWorks.RimLogging.Tests.Pipeline;

public class LogScopeTests : IDisposable
{
    public LogScopeTests() => LogScope.Clear();

    public void Dispose()
    {
        LogScope.Clear();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Merge_NoScope_ReturnsTheContextUntouched()
    {
        Dictionary<string, object?> ctx = new Dictionary<string, object?> { ["a"] = 1 };

        Assert.Same(ctx, LogScope.Merge(ctx));
    }

    [Fact]
    public void Merge_NoScopeAndNoContext_IsNull()
    {
        Assert.Null(LogScope.Merge(null));
    }

    [Fact]
    public void Push_AttachesThePairToLaterEntries()
    {
        using (LogScope.Push("pawn", "Randy"))
        {
            IReadOnlyDictionary<string, object?>? merged = LogScope.Merge(null);

            Assert.NotNull(merged);
            Assert.Equal("Randy", merged!["pawn"]);
        }
    }

    [Fact]
    public void Push_IsUndoneOnDispose()
    {
        using (LogScope.Push("pawn", "Randy")) { }

        Assert.Null(LogScope.Merge(null));
        Assert.Equal(0, LogScope.Depth);
    }

    [Fact]
    public void Push_NestsAndUnwindsInOrder()
    {
        using (LogScope.Push("outer", 1))
        {
            using (LogScope.Push("inner", 2))
            {
                Assert.Equal(2, LogScope.Depth);
            }
            Assert.Equal(1, LogScope.Depth);
            Assert.False(LogScope.Merge(null)!.ContainsKey("inner"));
        }
    }

    [Fact]
    public void Merge_ExplicitContextWinsOverTheScope()
    {
        using (LogScope.Push("pawn", "Randy"))
        {
            IReadOnlyDictionary<string, object?>? merged =
                LogScope.Merge(new Dictionary<string, object?> { ["pawn"] = "Cassandra" });

            // naming the key at the call site meant that one
            Assert.Equal("Cassandra", merged!["pawn"]);
        }
    }

    [Fact]
    public void Dispose_OutOfOrder_DoesNotStrandLaterFrames()
    {
        IDisposable outer = LogScope.Push("outer", 1);
        LogScope.Push("inner", 2);

        outer.Dispose();

        // disposing the outer frame drops everything opened after it, rather than leaking
        Assert.Equal(0, LogScope.Depth);
    }

    [Fact]
    public void Dispose_Twice_IsHarmless()
    {
        IDisposable frame = LogScope.Push("k", 1);
        frame.Dispose();
        frame.Dispose();

        Assert.Equal(0, LogScope.Depth);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Push_UntrackableKey_IsANoOp(string? key)
    {
        using (LogScope.Push(key!, 1))
        {
            Assert.Equal(0, LogScope.Depth);
        }
    }
}
