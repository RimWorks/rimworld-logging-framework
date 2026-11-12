using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using RimWorks.RimLogging.Bundle;
using Xunit;

namespace RimWorks.RimLogging.Tests.Bundle;

public class BundleUploadAppendTests
{
    private const string CreatedBody = "{\"url\":\"https://rimworks.org/logging?id=abc\",\"id\":\"abc\",\"token\":\"t1\"}";

    private static LocalBundleServer Server()
    {
        return new LocalBundleServer { Body = i => i == 0 ? CreatedBody : "{}" };
    }

    private static BundlePayload.EntryDto Entry(string message)
    {
        return new BundlePayload.EntryDto
        {
            Timestamp = "2026-10-22T00:00:00Z",
            Level = "Info",
            Channel = "Vanilla",
            Source = "test",
            Message = message,
        };
    }

    private static List<BundlePayload.EntryDto> Entries(int count)
    {
        List<BundlePayload.EntryDto> list = new List<BundlePayload.EntryDto>();
        for (int i = 0; i < count; i++) list.Add(Entry("line " + i));
        return list;
    }

    [Fact]
    public async Task UploadAsync_ReadsBackTheIdAndTokenAnAppendNeeds()
    {
        using LocalBundleServer h = Server();
        BundleUploadClient client = new BundleUploadClient(h.Endpoint);

        PublishResult r = await client.UploadAsync(new BundlePayload());

        Assert.True(r.Success);
        Assert.Equal("abc", r.Id);
        Assert.Equal("t1", r.Token);
        Assert.Null(h.Tokens[0]);
    }

    [Fact]
    public async Task AppendAsync_PostsToTheChunkUrlWithTheToken()
    {
        using LocalBundleServer h = Server();
        BundleUploadClient client = new BundleUploadClient(h.Endpoint);

        PublishResult r = await client.AppendAsync("abc", "t1", Entries(2));

        Assert.True(r.Success);
        Assert.Equal(h.Endpoint + "/abc/chunk", h.Urls[0]);
        Assert.Equal("t1", h.Tokens[0]);
        Assert.Contains("\"entries\"", h.Bodies[0]);
        Assert.Contains("line 1", h.Bodies[0]);
    }

    [Fact]
    public async Task AppendAsync_ARefusalComesBackAsAFailedResult()
    {
        using LocalBundleServer h = Server();
        h.Statuses.Enqueue(HttpStatusCode.Forbidden);
        BundleUploadClient client = new BundleUploadClient(h.Endpoint);

        PublishResult r = await client.AppendAsync("abc", "wrong", Entries(1));

        Assert.False(r.Success);
        Assert.Contains("403", r.ErrorMessage);
    }

    private static List<BundlePayload.EntryDto> TwoChunksWorth()
    {
        return new List<BundlePayload.EntryDto>
        {
            Entry(new string('a', 3 * 1024 * 1024)),
            Entry(new string('b', 3 * 1024 * 1024)),
        };
    }

    [Fact]
    public async Task Upload_SplitsALongBundleIntoOneCreateAndTheRestAsAppends()
    {
        using LocalBundleServer h = Server();

        PublishResult r = await BundleUploadCoordinator.Upload(
            new BundlePayload { Entries = TwoChunksWorth() },
            new PublishOptions { RimWorksUrl = h.Endpoint });

        Assert.True(r.Success);
        Assert.Equal("https://rimworks.org/logging?id=abc", r.Url);
        Assert.Equal(2, h.Urls.Count);
        Assert.Equal(h.Endpoint, h.Urls[0]);
        Assert.Equal(h.Endpoint + "/abc/chunk", h.Urls[1]);
        Assert.Equal("t1", h.Tokens[1]);
    }

    [Fact]
    public async Task Upload_AFailedAppendIsReportedEvenThoughTheBundleExists()
    {
        using LocalBundleServer h = Server();
        h.Statuses.Enqueue(HttpStatusCode.OK);
        h.Statuses.Enqueue(HttpStatusCode.Conflict);

        PublishResult r = await BundleUploadCoordinator.Upload(
            new BundlePayload { Entries = TwoChunksWorth() },
            new PublishOptions { RimWorksUrl = h.Endpoint });

        Assert.False(r.Success);
        Assert.Contains("409", r.ErrorMessage);
    }

    [Fact]
    public async Task Append_SendsEveryBatchToTheSameBundle()
    {
        using LocalBundleServer h = Server();

        PublishResult r = await BundleUploadCoordinator.Append(
            "abc",
            "t1",
            TwoChunksWorth(),
            new PublishOptions { RimWorksUrl = h.Endpoint });

        Assert.True(r.Success);
        Assert.Equal(2, h.Urls.Count);
        Assert.All(h.Urls, u => Assert.Equal(h.Endpoint + "/abc/chunk", u));
    }
}
