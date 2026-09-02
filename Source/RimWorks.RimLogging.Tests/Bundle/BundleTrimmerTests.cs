using System.Text;
using RimWorks.RimLogging.Bundle;
using Xunit;

namespace RimWorks.RimLogging.Tests.Bundle;

public class BundleTrimmerTests
{
    [Fact]
    public void Cap_ContentUnderBudget_IsReturnedUnchanged()
    {
        string text = "line one\nline two\n";

        Assert.Equal(text, BundleTrimmer.Cap(text, 1000));
    }

    [Fact]
    public void Cap_OverBudget_StaysWithinTheBudget()
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < 5000; i++) sb.Append("entry ").Append(i).Append('\n');

        string capped = BundleTrimmer.Cap(sb.ToString(), 4096);

        Assert.True(Encoding.UTF8.GetByteCount(capped) <= 4096,
            $"expected <= 4096 bytes, got {Encoding.UTF8.GetByteCount(capped)}");
    }

    [Fact]
    public void Cap_OverBudget_KeepsTheHeaderAndTheNewestLines()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("HEADER\n");
        for (int i = 0; i < 5000; i++) sb.Append("entry ").Append(i).Append('\n');

        string capped = BundleTrimmer.Cap(sb.ToString(), 4096);

        Assert.StartsWith("HEADER", capped);
        Assert.Contains("entry 4999", capped);
        Assert.Contains("lines dropped", capped);
    }

    [Fact]
    public void Cap_NonAsciiNearTheEdge_StillFits()
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < 2000; i++) sb.Append("ошибка ").Append(i).Append('\n');

        string capped = BundleTrimmer.Cap(sb.ToString(), 2048);

        Assert.True(Encoding.UTF8.GetByteCount(capped) <= 2048,
            $"expected <= 2048 bytes, got {Encoding.UTF8.GetByteCount(capped)}");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Cap_NoBudget_ReturnsEmpty(int maxBytes)
    {
        Assert.Equal(string.Empty, BundleTrimmer.Cap("anything at all", maxBytes));
    }
}
