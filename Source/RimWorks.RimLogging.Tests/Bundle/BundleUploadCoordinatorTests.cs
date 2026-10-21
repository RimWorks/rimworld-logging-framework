using System.Collections.Generic;
using RimWorks.RimLogging;
using RimWorks.RimLogging.Bundle;
using RimWorks.RimLogging.Sinks;
using Xunit;

namespace RimWorks.RimLogging.Tests.Bundle;

public class BundleUploadCoordinatorTests
{
    [Fact]
    public void FindMemorySink_ReturnsMemorySinkWhenRegistered()
    {
        MemoryLogSink memory = new MemoryLogSink();
        List<ILogSink> sinks = [new OtherSink(), memory, new OtherSink()];

        Assert.Same(memory, BundleUploadCoordinator.FindMemorySink(sinks));
    }

    [Fact]
    public void FindMemorySink_ReturnsNullWhenAbsent()
    {
        List<ILogSink> sinks = [new OtherSink()];

        Assert.Null(BundleUploadCoordinator.FindMemorySink(sinks));
    }

    [Fact]
    public void FindMemorySink_ReturnsNullForEmpty()
    {
        Assert.Null(BundleUploadCoordinator.FindMemorySink([]));
    }

    [Fact]
    public void DescribeResult_Success_IncludesBundleUrl()
    {
        string message = BundleUploadCoordinator.DescribeResult(
            new PublishResult { Success = true, Url = "https://rimworks.org/b/abc" });

        Assert.Contains("https://rimworks.org/b/abc", message);
    }

    [Fact]
    public void DescribeResult_Failure_IncludesErrorMessage()
    {
        string message = BundleUploadCoordinator.DescribeResult(
            new PublishResult { Success = false, ErrorMessage = "429 rate limited" });

        Assert.Contains("429 rate limited", message);
    }

    private sealed class OtherSink : ILogSink
    {
        public string Name => "Other";
        public LogLevel MinLevel => LogLevel.Trace;
        public void Write(LogEntry entry) { }
        public void Flush() { }
        public void Dispose() { }
    }
}
