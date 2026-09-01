using System.Collections.Generic;
using RimWorks.RimLogging.Bundle;
using Xunit;

namespace RimWorks.RimLogging.Tests.Bundle;

public class BundleSerializerTests
{
    [Fact]
    public void Serialize_Roundtrip_PreservesFields()
    {
        BundlePayload p = new BundlePayload
        {
            RimWorldVersion = "1.6.4444",
            FrameworkVersion = "v1.0.0-beta",
        };
        p.Mods.Add(new BundlePayload.ModInfo { Name = "Core", PackageId = "Ludeon.RimWorld", Active = true });
        p.Entries.Add(new BundlePayload.EntryDto
        {
            Timestamp = "2026-05-20T00:00:00Z",
            Level = "Info",
            Channel = "default",
            Source = "Foo.cs:1",
            Message = "hello",
            Context = new Dictionary<string, object?> { ["user"] = "alice" },
        });

        string json = BundleSerializer.Serialize(p);
        BundlePayload back = BundleSerializer.Deserialize(json);

        Assert.Equal(p.RimWorldVersion, back.RimWorldVersion);
        Assert.Equal(p.FrameworkVersion, back.FrameworkVersion);
        Assert.Single(back.Mods);
        Assert.Equal("Core", back.Mods[0].Name);
        Assert.Single(back.Entries);
        Assert.Equal("hello", back.Entries[0].Message);
        Assert.NotNull(back.Entries[0].Context);
        Assert.Equal("alice", back.Entries[0].Context!["user"]?.ToString());
    }

    [Fact]
    public void Serialize_NullCtx_OmittedFromOutput()
    {
        BundlePayload p = new BundlePayload();
        p.Entries.Add(new BundlePayload.EntryDto { Timestamp = "t", Level = "Info", Channel = "c", Source = "", Message = "m", Context = null });
        string json = BundleSerializer.Serialize(p);
        Assert.DoesNotContain("\"ctx\"", json);
    }

    [Fact]
    public void Serialize_NullStack_OmittedFromOutput()
    {
        BundlePayload p = new BundlePayload();
        p.Entries.Add(new BundlePayload.EntryDto { Timestamp = "t", Level = "Info", Channel = "c", Source = "", Message = "m", Stack = null });
        string json = BundleSerializer.Serialize(p);
        Assert.DoesNotContain("\"stack\"", json);
    }

    [Fact]
    public void Serialize_RichTextNotAutoStripped()
    {
        BundlePayload p = new BundlePayload();
        p.Entries.Add(new BundlePayload.EntryDto { Timestamp = "t", Level = "Info", Channel = "c", Source = "", Message = "<color=red>x</color>" });
        string json = BundleSerializer.Serialize(p);
        Assert.Contains("<color=red>x</color>", json);
    }
}
