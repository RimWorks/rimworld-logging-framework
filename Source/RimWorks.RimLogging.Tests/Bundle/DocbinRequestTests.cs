using System;
using RimWorks.RimLogging.Bundle;
using Xunit;

namespace RimWorks.RimLogging.Tests.Bundle;

public class DocbinRequestTests
{
    [Fact]
    public void Url_Anonymous_UsesTheAnonEndpointAndCarriesNoName()
    {
        string url = DocbinRequest.Url("https://docbin.app", false, "public", "ignored");

        Assert.Equal("https://docbin.app/api/docs/paste/anon?type=text&language=rimworld", url);
    }

    [Fact]
    public void Url_Authenticated_CarriesVisibilityAndName()
    {
        string url = DocbinRequest.Url("https://docbin.app", true, "private", "rimlogging-20260902-051500");

        Assert.Contains("/api/docs/paste?", url);
        Assert.Contains("visibility=private", url);
        Assert.Contains("name=rimlogging-20260902-051500", url);
    }

    [Fact]
    public void Url_TrailingSlash_IsNotDoubled()
    {
        Assert.DoesNotContain("app//api", DocbinRequest.Url("https://docbin.app/", false, null, null));
    }

    [Fact]
    public void Url_BlankVisibility_FallsBackToUnlisted()
    {
        Assert.Contains("visibility=unlisted", DocbinRequest.Url("https://docbin.app", true, "  ", "n"));
    }

    [Fact]
    public void MaxBytes_AuthedCeilingIsHigherAndBothLeaveHeaderRoom()
    {
        Assert.True(DocbinRequest.MaxBytes(true) > DocbinRequest.MaxBytes(false));
        Assert.True(DocbinRequest.MaxBytes(false) < 1048576);
        Assert.True(DocbinRequest.MaxBytes(true) < 5242880);
    }

    [Fact]
    public void NameFor_IsSortableAndPrefixed()
    {
        Assert.Equal("rimlogging-20260902-051500", DocbinRequest.NameFor(new DateTime(2026, 9, 2, 5, 15, 0, DateTimeKind.Utc)));
    }
}
