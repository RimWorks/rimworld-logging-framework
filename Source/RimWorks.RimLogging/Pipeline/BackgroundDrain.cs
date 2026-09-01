using System;
using System.Threading;

namespace RimWorks.RimLogging.Pipeline;

/// <summary>
/// Drains  entries on a single, dedicated background thread (RimLogging-Drain) and dispatches each entry to
/// a caller-supplied callback.
/// </summary>
/// <remarks>
/// Back-off strategy: spin for the first 64 empty polls, then 1 ms up to poll 256, then sleep 5 ms
/// thereafter. Any successful dequeue resets the counter to zero.
/// </remarks>
internal sealed class BackgroundDrain : IDisposable
{
    private readonly MpscQueue<LogEntry> _queue;
    private readonly Action<LogEntry> _dispatch;
    private readonly Thread _thread;
    private volatile bool _stop;
    private readonly ManualResetEventSlim _drained = new ManualResetEventSlim(false);

    /// <summary>
    /// Initializes a new <see cref="BackgroundDrain"/> and starts the drain thread.
    /// </summary>
    /// <param name="queue">The MPSC queue to consume from.</param>
    /// <param name="dispatch">Callback invoked for each dequeued entry.
    /// Exceptions thrown by this callback are swallowed so the drain thread
    /// is never killed by a misbehaving sink.</param>
    public BackgroundDrain(MpscQueue<LogEntry> queue, Action<LogEntry> dispatch)
    {
        _queue = queue;
        _dispatch = dispatch;
        _thread = new Thread(Loop)
        {
            IsBackground = true,
            Name = "RimLogging-Drain",
            Priority = ThreadPriority.BelowNormal,
        };
        _thread.Start();
    }

    /// <summary>Enqueues <paramref name="e"/> and marks the queue as non-empty.</summary>
    public void Enqueue(LogEntry e) { _queue.TryEnqueue(e); _drained.Reset(); }

    /// <summary>
    /// Blocks until the queue is empty or <paramref name="timeoutMs"/> elapses.
    /// </summary>
    /// <returns><c>true</c> if the queue drained within the timeout; <c>false</c> otherwise.</returns>
    public bool WaitForDrain(int timeoutMs)
        => _queue.ApproximateCount == 0 || _drained.Wait(timeoutMs);

    /// <summary>
    /// Signals the drain thread to stop, waits up to 2 s for it to finish the
    /// final-drain pass, then disposes the internal event.
    /// </summary>
    public void Dispose() { _stop = true; _thread.Join(2000); _drained.Dispose(); }

    private void Loop()
    {
        int emptyPolls = 0;
        while (!_stop)
        {
            if (TryDispatchNext()) emptyPolls = 0;
            else Backoff(++emptyPolls);
        }
        FinalDrain();
    }

    private bool TryDispatchNext()
    {
        if (!_queue.TryDequeue(out LogEntry? entry) || entry == null) return false;
        Dispatch(entry);
        if (_queue.ApproximateCount == 0) _drained.Set();
        return true;
    }

    private void Dispatch(LogEntry entry)
    {
        try { _dispatch(entry); }
        catch { /* swallow: a logger crash mustn't kill the drain */ }
    }

    private static void Backoff(int emptyPolls)
    {
        if (emptyPolls < 64) Thread.SpinWait(32);
        else if (emptyPolls < 256) Thread.Sleep(1);
        else Thread.Sleep(5);
    }

    private void FinalDrain()
    {
        while (_queue.TryDequeue(out LogEntry? final) && final != null)
        {
            Dispatch(final);
        }
    }
}
