using System;
using System.Collections.Generic;
using System.Diagnostics;
using RimWorks.RimLogging.Patching;
using Xunit;

namespace RimWorks.RimLogging.Tests.Patching;

public class PatchAttributionGuardTests : IDisposable
{
    private static readonly StackFrame Frame = new StackFrame();

    public void Dispose() => Logging.AttributionProvider = null;

    [Fact]
    public void OwnersFor_NoProviderInstalled_IsEmpty()
    {
        Logging.AttributionProvider = null;

        Assert.Empty(PatchAttributionGuard.OwnersFor(Frame));
    }

    [Fact]
    public void OwnersFor_ProviderReturnsOwners_PassesThemThrough()
    {
        Logging.AttributionProvider = _ => ["some.mod"];

        Assert.Equal(["some.mod"], PatchAttributionGuard.OwnersFor(Frame));
    }

    [Fact]
    public void OwnersFor_ProviderThrows_IsEmptyRatherThanPropagating()
    {
        Logging.AttributionProvider = _ => throw new InvalidOperationException("backend blew up");

        IReadOnlyList<string> owners = PatchAttributionGuard.OwnersFor(Frame);

        Assert.Empty(owners);
    }

    [Fact]
    public void OwnersFor_ProviderReturnsNull_IsEmptyRatherThanNull()
    {
        Logging.AttributionProvider = _ => null!;

        Assert.Empty(PatchAttributionGuard.OwnersFor(Frame));
    }
}
