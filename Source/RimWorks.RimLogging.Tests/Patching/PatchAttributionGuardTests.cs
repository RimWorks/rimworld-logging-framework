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

        IReadOnlyList<string>? owners = PatchAttributionGuard.OwnersFor(Frame);

        // no backend at all is a definite "nothing patched it", not "could not tell"
        Assert.NotNull(owners);
        Assert.Empty(owners!);
    }

    [Fact]
    public void OwnersFor_ProviderReturnsOwners_PassesThemThrough()
    {
        Logging.AttributionProvider = _ => ["some.mod"];

        Assert.Equal(["some.mod"], PatchAttributionGuard.OwnersFor(Frame));
    }

    [Fact]
    public void OwnersFor_ProviderThrows_IsUnavailableRatherThanEmpty()
    {
        Logging.AttributionProvider = _ => throw new InvalidOperationException("backend blew up");

        // a backend that blew up did not tell us the method is clean
        Assert.Null(PatchAttributionGuard.OwnersFor(Frame));
    }

    [Fact]
    public void OwnersFor_ProviderReturnsNull_StaysUnavailable()
    {
        Logging.AttributionProvider = _ => null;

        Assert.Null(PatchAttributionGuard.OwnersFor(Frame));
    }
}
