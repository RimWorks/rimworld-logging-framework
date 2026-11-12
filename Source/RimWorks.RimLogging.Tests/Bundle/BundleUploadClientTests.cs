using System;
using System.Net;
using System.Threading.Tasks;
using RimWorks.RimLogging.Bundle;
using Xunit;

namespace RimWorks.RimLogging.Tests.Bundle;

public class BundleUploadClientTests
{
    [Fact]
    public async Task UploadAsync_Success_ReturnsUrl()
    {
        using LocalBundleServer server = new LocalBundleServer { Body = _ => "{\"url\":\"https://rimworks.org/b/abc\"}" };
        BundleUploadClient proxy = new BundleUploadClient(server.Endpoint);
        PublishResult r = await proxy.UploadAsync(new BundlePayload());
        Assert.True(r.Success);
        Assert.Equal("https://rimworks.org/b/abc", r.Url);
        Assert.Null(r.ErrorMessage);
    }

    [Fact]
    public async Task UploadAsync_500_ReturnsFailureWithBody()
    {
        using LocalBundleServer server = new LocalBundleServer { Body = _ => "server down" };
        server.Statuses.Enqueue(HttpStatusCode.InternalServerError);
        BundleUploadClient proxy = new BundleUploadClient(server.Endpoint);
        PublishResult r = await proxy.UploadAsync(new BundlePayload());
        Assert.False(r.Success);
        Assert.Contains("500", r.ErrorMessage!);
        Assert.Contains("server down", r.ErrorMessage!);
    }

    [Fact]
    public async Task UploadAsync_MalformedJson_ReportsAsError()
    {
        using LocalBundleServer server = new LocalBundleServer { Body = _ => "not json" };
        BundleUploadClient proxy = new BundleUploadClient(server.Endpoint);
        PublishResult r = await proxy.UploadAsync(new BundlePayload());
        Assert.False(r.Success);
        Assert.Contains("Malformed", r.ErrorMessage!);
    }

    [Fact]
    public async Task UploadAsync_MissingUrlField_ReportsAsError()
    {
        using LocalBundleServer server = new LocalBundleServer { Body = _ => "{\"other\":\"field\"}" };
        BundleUploadClient proxy = new BundleUploadClient(server.Endpoint);
        PublishResult r = await proxy.UploadAsync(new BundlePayload());
        Assert.False(r.Success);
        Assert.Contains("missing", r.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadAsync_NothingListening_ReturnsAFailedResult()
    {
        BundleUploadClient proxy = new BundleUploadClient(LocalBundleServer.ClosedEndpoint());
        PublishResult r = await proxy.UploadAsync(new BundlePayload());
        Assert.False(r.Success);
        Assert.False(string.IsNullOrEmpty(r.ErrorMessage));
    }

    [Fact]
    public void Ctor_NullUrl_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new BundleUploadClient(null!));
    }
}
