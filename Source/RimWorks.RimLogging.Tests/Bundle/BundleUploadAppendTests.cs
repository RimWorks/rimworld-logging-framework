using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RimWorks.RimLogging.Bundle;
using Xunit;

namespace RimWorks.RimLogging.Tests.Bundle;

public class BundleUploadAppendTests
{
    private const string Endpoint = "https://rimworks.example/api/bundle";

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public readonly List<string> Urls = new List<string>();
        public readonly List<string> Bodies = new List<string>();
        public readonly List<string?> Tokens = new List<string?>();
        public readonly Queue<HttpStatusCode> Statuses = new Queue<HttpStatusCode>();
        public string CreatedBody = "{\"url\":\"https://rimworks.org/logging?id=abc\",\"id\":\"abc\",\"token\":\"t1\"}";

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Urls.Add(request.RequestUri!.ToString());
            Bodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
            Tokens.Add(request.Headers.TryGetValues("x-bundle-token", out System.Collections.Generic.IEnumerable<string>? v)
                ? string.Join(",", v)
                : null);

            HttpStatusCode status = Statuses.Count > 0 ? Statuses.Dequeue() : HttpStatusCode.OK;
            string body = Urls.Count == 1 ? CreatedBody : "{}";
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }
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
        RecordingHandler h = new RecordingHandler();
        BundleUploadClient client = new BundleUploadClient(Endpoint, new HttpClient(h));

        PublishResult r = await client.UploadAsync(new BundlePayload());

        Assert.True(r.Success);
        Assert.Equal("abc", r.Id);
        Assert.Equal("t1", r.Token);
        Assert.Null(h.Tokens[0]);
    }

    [Fact]
    public async Task AppendAsync_PostsToTheChunkUrlWithTheToken()
    {
        RecordingHandler h = new RecordingHandler();
        BundleUploadClient client = new BundleUploadClient(Endpoint, new HttpClient(h));

        PublishResult r = await client.AppendAsync("abc", "t1", Entries(2));

        Assert.True(r.Success);
        Assert.Equal(Endpoint + "/abc/chunk", h.Urls[0]);
        Assert.Equal("t1", h.Tokens[0]);
        Assert.Contains("\"entries\"", h.Bodies[0]);
        Assert.Contains("line 1", h.Bodies[0]);
    }

    [Fact]
    public async Task AppendAsync_ARefusalComesBackAsAFailedResult()
    {
        RecordingHandler h = new RecordingHandler();
        h.Statuses.Enqueue(HttpStatusCode.Forbidden);
        BundleUploadClient client = new BundleUploadClient(Endpoint, new HttpClient(h));

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
        RecordingHandler h = new RecordingHandler();

        PublishResult r = await BundleUploadCoordinator.Upload(
            new BundlePayload { Entries = TwoChunksWorth() },
            new PublishOptions { RimWorksUrl = Endpoint },
            new HttpClient(h));

        Assert.True(r.Success);
        Assert.Equal("https://rimworks.org/logging?id=abc", r.Url);
        Assert.Equal(2, h.Urls.Count);
        Assert.Equal(Endpoint, h.Urls[0]);
        Assert.Equal(Endpoint + "/abc/chunk", h.Urls[1]);
        Assert.Equal("t1", h.Tokens[1]);
    }

    [Fact]
    public async Task Upload_AFailedAppendIsReportedEvenThoughTheBundleExists()
    {
        RecordingHandler h = new RecordingHandler();
        h.Statuses.Enqueue(HttpStatusCode.OK);
        h.Statuses.Enqueue(HttpStatusCode.Conflict);

        PublishResult r = await BundleUploadCoordinator.Upload(
            new BundlePayload { Entries = TwoChunksWorth() },
            new PublishOptions { RimWorksUrl = Endpoint },
            new HttpClient(h));

        Assert.False(r.Success);
        Assert.Contains("409", r.ErrorMessage);
    }

    [Fact]
    public async Task Append_SendsEveryBatchToTheSameBundle()
    {
        RecordingHandler h = new RecordingHandler();

        PublishResult r = await BundleUploadCoordinator.Append(
            "abc",
            "t1",
            TwoChunksWorth(),
            new PublishOptions { RimWorksUrl = Endpoint },
            new HttpClient(h));

        Assert.True(r.Success);
        Assert.Equal(2, h.Urls.Count);
        Assert.All(h.Urls, u => Assert.Equal(Endpoint + "/abc/chunk", u));
    }
}
