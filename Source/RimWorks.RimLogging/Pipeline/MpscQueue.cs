using System;
using System.Threading;

namespace RimWorks.RimLogging.Pipeline;

/// <summary>
/// Bounded multi-producer, single-consumer lock-free ring-buffer queue.
/// Overflow policy: drop the new entry and increment <see cref="DroppedCount"/>.
/// </summary>
internal sealed class MpscQueue<T> where T : class
{
    private readonly T?[] _buffer;
    private readonly int _mask;
    private long _producerHead;
    private long _consumerTail;
    private long _dropped;

    public MpscQueue(int capacityPow2)
    {
        if (capacityPow2 <= 0 || (capacityPow2 & (capacityPow2 - 1)) != 0)
            throw new ArgumentException("capacity must be a positive power of two", nameof(capacityPow2));

        _buffer = new T?[capacityPow2];
        _mask = capacityPow2 - 1;
    }

    public int DroppedCount => (int)Interlocked.Read(ref _dropped);

    public int ApproximateCount
        => (int)Math.Max(0, Volatile.Read(ref _producerHead) - Volatile.Read(ref _consumerTail));

    public bool TryEnqueue(T item)
    {
        long head = Interlocked.Increment(ref _producerHead) - 1;
        long tail = Volatile.Read(ref _consumerTail);
        if (head - tail >= _buffer.Length)
        {
            Interlocked.Increment(ref _dropped);
            return false;
        }
        Volatile.Write(ref _buffer[head & _mask], item);
        return true;
    }

    // SINGLE CONSUMER ONLY
    public bool TryDequeue(out T? item)
    {
        long tail = _consumerTail;
        long head = Volatile.Read(ref _producerHead);
        if (tail >= head)
        {
            item = null;
            return false;
        }
        T? slot = Volatile.Read(ref _buffer[tail & _mask]);
        if (slot == null)
        {
            // producer reserved the slot but has not published yet
            item = null;
            return false;
        }
        _buffer[tail & _mask] = null;
        _consumerTail = tail + 1;
        item = slot;
        return true;
    }
}
