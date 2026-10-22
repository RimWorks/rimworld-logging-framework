using System.Collections.Generic;
using RimWorks.RimLogging.Bundle;

namespace RimWorks.RimLogging.Viewer;

internal sealed class BundleShareSession
{
    private int sentCount;
    private LogEntry? lastSent;

    internal string? Id { get; private set; }

    internal string? Token { get; private set; }

    internal string? Url { get; private set; }

    internal bool CanAppend(IReadOnlyList<LogEntry> snapshot)
    {
        return Id != null
            && Token != null
            && sentCount > 0
            && snapshot.Count >= sentCount
            && ReferenceEquals(snapshot[sentCount - 1], lastSent);
    }

    internal List<LogEntry> Pending(IReadOnlyList<LogEntry> snapshot)
    {
        List<LogEntry> pending = new List<LogEntry>();
        for (int i = sentCount; i < snapshot.Count; i++) pending.Add(snapshot[i]);
        return pending;
    }

    internal void Record(PublishResult result, string url, IReadOnlyList<LogEntry> snapshot)
    {
        Id = result.Id;
        Token = result.Token ?? Token;
        Url = url;
        sentCount = snapshot.Count;
        lastSent = snapshot.Count > 0 ? snapshot[snapshot.Count - 1] : null;
    }

    internal void Clear()
    {
        Id = null;
        Token = null;
        Url = null;
        sentCount = 0;
        lastSent = null;
    }
}
