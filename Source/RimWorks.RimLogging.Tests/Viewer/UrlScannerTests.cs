using System;
using System.Collections.Generic;
using RimWorks.RimLogging.Viewer;
using Xunit;

namespace RimWorks.RimLogging.Tests.Viewer;

public class UrlScannerTests
{
    private static LogEntry Entry(string message) => new LogEntry
    {
        Timestamp = new DateTime(2026, 1, 1),
        Level = LogLevel.Info,
        Channel = "Test",
        MessageTemplate = message,
        RenderedMessage = message,
    };

    [Fact]
    public void Find_PicksUpAUrlInASentence()
    {
        Assert.Equal(["http://127.0.0.1:41261/"], UrlScanner.Find("pickle: listening at http://127.0.0.1:41261/"));
    }

    [Fact]
    public void Find_TrimsTrailingSentencePunctuation()
    {
        Assert.Equal(["https://docbin.app/x"], UrlScanner.Find("uploaded to https://docbin.app/x."));
        Assert.Equal(["https://docbin.app/x"], UrlScanner.Find("(see https://docbin.app/x)"));
    }

    [Fact]
    public void Find_KeepsPathAndQueryIntact()
    {
        Assert.Equal(["https://a.test/p/q?x=1&y=2#frag"],
            UrlScanner.Find("go to https://a.test/p/q?x=1&y=2#frag now"));
    }

    [Fact]
    public void Find_MatchesHttpAndHttpsOnly()
    {
        Assert.Empty(UrlScanner.Find("ftp://a.test/x and file:///tmp/y"));
    }

    [Fact]
    public void Find_IsCaseInsensitiveOnTheScheme()
    {
        Assert.Single(UrlScanner.Find("see HTTPS://A.TEST/x"));
    }

    [Fact]
    public void Find_DeduplicatesRepeats()
    {
        Assert.Single(UrlScanner.Find("http://a.test/x twice: http://a.test/x"));
    }

    [Fact]
    public void Find_CapsTheCountSoOneEntryCannotFloodThePane()
    {
        string text = "http://a.test/1 http://a.test/2 http://a.test/3 http://a.test/4 "
            + "http://a.test/5 http://a.test/6 http://a.test/7";

        Assert.Equal(UrlScanner.DefaultMax, UrlScanner.Find(text).Count);
        Assert.Equal(2, UrlScanner.Find(text, max: 2).Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("no links here at all")]
    public void Find_NothingToFind_ReturnsEmpty(string? text)
    {
        Assert.Empty(UrlScanner.Find(text));
    }

    [Fact]
    public void ForEntry_ReadsTheMessageAndTheStackTogether()
    {
        IReadOnlyList<string> found = UrlScanner.ForEntry(
            Entry("failed, see http://a.test/msg"), "at Foo() // http://a.test/trace");

        Assert.Equal(["http://a.test/msg", "http://a.test/trace"], found);
    }

    [Fact]
    public void ForEntry_NoTrace_StillReadsTheMessage()
    {
        Assert.Single(UrlScanner.ForEntry(Entry("see http://a.test/x"), null));
    }
}
