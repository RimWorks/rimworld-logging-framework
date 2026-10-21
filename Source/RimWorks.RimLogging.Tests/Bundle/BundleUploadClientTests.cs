using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RimWorks.RimLogging.Bundle;
using Xunit;

namespace RimWorks.RimLogging.Tests.Bundle;

public class BundleUploadClientTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        public HttpStatusCode Status = HttpStatusCode.OK;
        public string Body = "{}";
        public Exception? ThrowOnSend;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (ThrowOnSend != null) throw ThrowOnSend;
            HttpResponseMessage resp = new HttpResponseMessage(Status)
            {
                Content = new StringContent(Body, Encoding.UTF8, "application/json"),
            };
            return Task.FromResult(resp);
        }
    }

    [Fact]
    public async Task UploadAsync_Success_ReturnsUrl()
    {
        StubHandler h = new StubHandler { Body = "{\"url\":\"https://rimworks.org/b/abc\"}" };
        HttpClient client = new HttpClient(h);
        BundleUploadClient proxy = new BundleUploadClient("https://rimworks.example/api/bundle", client);
        PublishResult r = await proxy.UploadAsync(new BundlePayload());
        Assert.True(r.Success);
        Assert.Equal("https://rimworks.org/b/abc", r.Url);
        Assert.Null(r.ErrorMessage);
    }

    [Fact]
    public async Task UploadAsync_500_ReturnsFailureWithBody()
    {
        StubHandler h = new StubHandler { Status = HttpStatusCode.InternalServerError, Body = "server down" };
        HttpClient client = new HttpClient(h);
        BundleUploadClient proxy = new BundleUploadClient("https://x/up", client);
        PublishResult r = await proxy.UploadAsync(new BundlePayload());
        Assert.False(r.Success);
        Assert.Contains("500", r.ErrorMessage!);
        Assert.Contains("server down", r.ErrorMessage!);
    }

    [Fact]
    public async Task UploadAsync_MalformedJson_ReportsAsError()
    {
        StubHandler h = new StubHandler { Body = "not json" };
        HttpClient client = new HttpClient(h);
        BundleUploadClient proxy = new BundleUploadClient("https://x/up", client);
        PublishResult r = await proxy.UploadAsync(new BundlePayload());
        Assert.False(r.Success);
        Assert.Contains("Malformed", r.ErrorMessage!);
    }

    [Fact]
    public async Task UploadAsync_MissingUrlField_ReportsAsError()
    {
        StubHandler h = new StubHandler { Body = "{\"other\":\"field\"}" };
        HttpClient client = new HttpClient(h);
        BundleUploadClient proxy = new BundleUploadClient("https://x/up", client);
        PublishResult r = await proxy.UploadAsync(new BundlePayload());
        Assert.False(r.Success);
        Assert.Contains("missing", r.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadAsync_NetworkException_Caught()
    {
        StubHandler h = new StubHandler { ThrowOnSend = new HttpRequestException("DNS failure") };
        HttpClient client = new HttpClient(h);
        BundleUploadClient proxy = new BundleUploadClient("https://x/up", client);
        PublishResult r = await proxy.UploadAsync(new BundlePayload());
        Assert.False(r.Success);
        Assert.Contains("DNS failure", r.ErrorMessage!);
    }

    [Fact]
    public void Ctor_NullUrl_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new BundleUploadClient(null!));
    }
}
