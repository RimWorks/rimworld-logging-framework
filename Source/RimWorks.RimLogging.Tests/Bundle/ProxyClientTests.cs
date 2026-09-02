using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using RimWorks.RimLogging.Bundle;

namespace RimWorks.RimLogging.Tests.Bundle;

public class ProxyClientTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        public HttpStatusCode Status = HttpStatusCode.OK;
        public string Body = "{}";
        public Exception? ThrowOnSend;
        public HttpRequestMessage? LastRequest;
        public string? LastBody;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastBody = request.Content?.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
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
        StubHandler h = new StubHandler { Body = "{\"url\":\"https://gist.github.com/x/abc\"}" };
        HttpClient client = new HttpClient(h);
        ProxyClient proxy = new ProxyClient("https://proxy.example/upload", client);
        PublishResult r = await proxy.UploadAsync(new BundlePayload());
        Assert.True(r.Success);
        Assert.Equal("https://gist.github.com/x/abc", r.Url);
        Assert.Null(r.ErrorMessage);
    }

    [Fact]
    public async Task UploadAsync_500_ReturnsFailureWithBody()
    {
        StubHandler h = new StubHandler { Status = HttpStatusCode.InternalServerError, Body = "server down" };
        HttpClient client = new HttpClient(h);
        ProxyClient proxy = new ProxyClient("https://x/up", client);
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
        ProxyClient proxy = new ProxyClient("https://x/up", client);
        PublishResult r = await proxy.UploadAsync(new BundlePayload());
        Assert.False(r.Success);
        Assert.Contains("Malformed", r.ErrorMessage!);
    }

    [Fact]
    public async Task UploadAsync_MissingUrlField_ReportsAsError()
    {
        StubHandler h = new StubHandler { Body = "{\"other\":\"field\"}" };
        HttpClient client = new HttpClient(h);
        ProxyClient proxy = new ProxyClient("https://x/up", client);
        PublishResult r = await proxy.UploadAsync(new BundlePayload());
        Assert.False(r.Success);
        Assert.Contains("missing", r.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadAsync_NetworkException_Caught()
    {
        StubHandler h = new StubHandler { ThrowOnSend = new HttpRequestException("DNS failure") };
        HttpClient client = new HttpClient(h);
        ProxyClient proxy = new ProxyClient("https://x/up", client);
        PublishResult r = await proxy.UploadAsync(new BundlePayload());
        Assert.False(r.Success);
        Assert.Contains("DNS failure", r.ErrorMessage!);
    }

    [Fact]
    public void Ctor_NullUrl_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ProxyClient(null!));
    }

    [Fact]
    public async Task UploadAsync_WithToken_SendsXGistTokenHeader()
    {
        StubHandler h = new StubHandler { Body = "{\"url\":\"https://gist.github.com/x/abc\"}" };
        HttpClient client = new HttpClient(h);
        ProxyClient proxy = new ProxyClient("https://proxy.example/upload", client, githubToken: "ghp_secret");
        await proxy.UploadAsync(new BundlePayload());
        Assert.True(h.LastRequest!.Headers.TryGetValues("X-Gist-Token", out IEnumerable<string>? values));
        Assert.Equal("ghp_secret", Assert.Single(values!));
    }

    [Fact]
    public async Task UploadAsync_WithToken_DoesNotLeakIntoBody()
    {
        StubHandler h = new StubHandler { Body = "{\"url\":\"https://gist.github.com/x/abc\"}" };
        HttpClient client = new HttpClient(h);
        ProxyClient proxy = new ProxyClient("https://proxy.example/upload", client, githubToken: "ghp_secret");
        await proxy.UploadAsync(new BundlePayload());
        Assert.DoesNotContain("ghp_secret", h.LastBody!);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UploadAsync_WithoutToken_OmitsXGistTokenHeader(string? token)
    {
        StubHandler h = new StubHandler { Body = "{\"url\":\"https://gist.github.com/x/abc\"}" };
        HttpClient client = new HttpClient(h);
        ProxyClient proxy = new ProxyClient("https://proxy.example/upload", client, githubToken: token);
        await proxy.UploadAsync(new BundlePayload());
        Assert.False(h.LastRequest!.Headers.Contains("X-Gist-Token"));
    }
}
