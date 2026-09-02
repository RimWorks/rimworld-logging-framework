using System.Collections.Generic;
using RimWorks.RimLogging.Bundle;
using Xunit;

namespace RimWorks.RimLogging.Tests.Bundle;

public class BundleTextRendererTests
{
    private static BundlePayload Payload() => new BundlePayload
    {
        RimWorldVersion = "1.6.4871",
        FrameworkVersion = "2.4.3",
        Mods =
        [
            new BundlePayload.ModInfo { Name = "Harmony", PackageId = "brrainz.harmony", Version = "2.4.2", Active = true },
            new BundlePayload.ModInfo { Name = "Old Mod", PackageId = "someone.old", Active = false },
        ],
        Entries =
        [
            new BundlePayload.EntryDto
            {
                Timestamp = "2026-09-02T05:27:35.372Z", Level = "ERROR", Channel = "default",
                Message = "it broke", Source = "Thing.cs:42", Stack = "at Foo.Bar()\nat Baz.Qux()",
            },
        ],
    };

    [Fact]
    public void Render_LeadsWithBothVersions()
    {
        string text = BundleTextRenderer.Render(Payload());

        Assert.Contains("RimWorld 1.6.4871", text);
        Assert.Contains("RimLogging 2.4.3", text);
    }

    [Fact]
    public void Render_CountsLoadedAndActiveModsSeparately()
    {
        Assert.Contains("Mods: 2 loaded, 1 active", BundleTextRenderer.Render(Payload()));
    }

    [Fact]
    public void Render_MarksInactiveModsSoTheLoadOrderIsReadable()
    {
        string text = BundleTextRenderer.Render(Payload());

        Assert.Contains("Old Mod [someone.old] (inactive)", text);
        Assert.Contains("Harmony [brrainz.harmony] 2.4.2", text);
    }

    [Fact]
    public void Render_KeepsTheEntryFieldsAndIndentsTheStack()
    {
        string text = BundleTextRenderer.Render(Payload());

        Assert.Contains("ERROR", text);
        Assert.Contains("[default]", text);
        Assert.Contains("it broke", text);
        Assert.Contains("(Thing.cs:42)", text);
        Assert.Contains("    at Foo.Bar()", text);
    }

    [Fact]
    public void Render_WritesStructuredContextUnderItsEntry()
    {
        BundlePayload payload = Payload();
        payload.Entries[0].Context = new Dictionary<string, object?> { ["pawn"] = "Randy" };

        Assert.Contains("pawn=Randy", BundleTextRenderer.Render(payload));
    }

    [Fact]
    public void Render_EmptyBundle_StillSaysSoRatherThanThrowing()
    {
        string text = BundleTextRenderer.Render(new BundlePayload());

        Assert.Contains("Mods: none recorded", text);
        Assert.Contains("Log: 0 entries", text);
    }

    [Fact]
    public void Render_NullPayload_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, BundleTextRenderer.Render(null!));
    }

    [Fact]
    public void Render_UpperCasesTheLevel()
    {
        BundlePayload payload = Payload();
        payload.Entries[0].Level = "Warning";

        // the paste host's log grammar only colours ALL CAPS levels
        Assert.Contains("  WARNING  ", BundleTextRenderer.Render(payload));
    }

    [Fact]
    public void Render_LeavesTheMessageCasingAlone()
    {
        BundlePayload payload = Payload();
        payload.Entries[0].Message = "Exception ticking Randy";

        Assert.Contains("Exception ticking Randy", BundleTextRenderer.Render(payload));
    }

    [Fact]
    public void Render_MultiLineMessage_IndentsTheWrappedTail()
    {
        BundlePayload payload = Payload();
        payload.Entries[0].Message = "SteamAPI.Init() failed. Possible causes:\nSteam not running\nno appid file";

        string text = BundleTextRenderer.Render(payload);

        // the tail must never sit at column 0, where it would read as another entry
        Assert.Contains("  SteamAPI.Init() failed. Possible causes:", text);
        Assert.Contains("    Steam not running", text);
        Assert.Contains("    no appid file", text);
        Assert.DoesNotContain("\nSteam not running", text);
    }

    [Fact]
    public void Render_MultiLineMessage_KeepsTheSourceOnTheFirstLine()
    {
        BundlePayload payload = Payload();
        payload.Entries[0].Message = "first\nsecond";
        payload.Entries[0].Source = "Thing.cs:42";

        Assert.Contains("first  (Thing.cs:42)", BundleTextRenderer.Render(payload));
    }

    [Fact]
    public void Render_CarriageReturnsAreNormalised()
    {
        BundlePayload payload = Payload();
        payload.Entries[0].Message = "one\r\ntwo";

        Assert.DoesNotContain("\r", BundleTextRenderer.Render(payload));
    }
}
