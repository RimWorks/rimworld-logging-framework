using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RimWorks.RimLogging.Pipeline;
using Xunit;

namespace RimWorks.RimLogging.Tests.Pipeline;

// Value tuples cannot satisfy `where T : class`; wrap them.
file sealed class Entry
{
    public Entry(int producer, int seq) { Producer = producer; Seq = seq; }
    public int Producer { get; }
    public int Seq { get; }
}

public class MpscQueueTests
{
    [Fact]
    public void SingleProducerSingleConsumer_DequeuesInFifoOrder()
    {
        MpscQueue<string> queue = new MpscQueue<string>(16);

        for (int i = 1; i <= 10; i++)
            Assert.True(queue.TryEnqueue(i.ToString()));

        for (int i = 1; i <= 10; i++)
        {
            bool dequeued = queue.TryDequeue(out string? item);
            Assert.True(dequeued);
            Assert.Equal(i.ToString(), item);
        }
    }

    [Fact]
    public void MultipleProducers_EachProducersItemsInOrder()
    {
        const int producerCount = 4;
        const int itemsPerProducer = 200;
        MpscQueue<Entry> queue = new MpscQueue<Entry>(1024);

        Thread[] producers = new Thread[producerCount];
        Barrier barrier = new Barrier(producerCount);

        for (int p = 0; p < producerCount; p++)
        {
            int producerId = p;
            producers[p] = new Thread(() =>
            {
                barrier.SignalAndWait();
                for (int seq = 0; seq < itemsPerProducer; seq++)
                    queue.TryEnqueue(new Entry(producerId, seq));
            });
            producers[p].IsBackground = true;
            producers[p].Start();
        }

        List<Entry> dequeued = new List<Entry>();
        int expectedTotal = producerCount * itemsPerProducer;
        DateTime deadline = DateTime.UtcNow.AddSeconds(5);

        while (dequeued.Count < expectedTotal && DateTime.UtcNow < deadline)
        {
            while (queue.TryDequeue(out Entry? item))
                dequeued.Add(item!);
            Thread.SpinWait(100);
        }

        foreach (Thread t in producers)
            t.Join(TimeSpan.FromSeconds(5));

        // Drain any remaining items after producers finish
        while (queue.TryDequeue(out Entry? item))
            dequeued.Add(item!);

        Assert.Equal(expectedTotal, dequeued.Count);

        // Each producer's items must appear in the order that producer enqueued them
        for (int p = 0; p < producerCount; p++)
        {
            List<int> seqs = dequeued
                .Where(x => x.Producer == p)
                .Select(x => x.Seq)
                .ToList();

            Assert.Equal(itemsPerProducer, seqs.Count);

            for (int i = 0; i < seqs.Count - 1; i++)
                Assert.True(seqs[i] < seqs[i + 1],
                    $"Producer {p}: seq {seqs[i]} appeared before {seqs[i + 1]} but should be after");
        }
    }

    [Fact]
    public void Overflow_DropsNewEntryAndIncrementsDroppedCount()
    {
        MpscQueue<string> queue = new MpscQueue<string>(4);

        for (int i = 0; i < 4; i++)
            Assert.True(queue.TryEnqueue("item"));

        bool result = queue.TryEnqueue("overflow");

        Assert.False(result);
        Assert.Equal(1, queue.DroppedCount);
    }

    [Fact]
    public void TryEnqueue_ReturnsFalseOnOverflow()
    {
        MpscQueue<string> queue = new MpscQueue<string>(2);

        queue.TryEnqueue("a");
        queue.TryEnqueue("b");

        bool overflow = queue.TryEnqueue("c");

        Assert.False(overflow);
    }

    [Fact]
    public void TryDequeue_ReturnsFalseOnEmpty_AndItemIsNull()
    {
        MpscQueue<string> queue = new MpscQueue<string>(8);

        bool result = queue.TryDequeue(out string? item);

        Assert.False(result);
        Assert.Null(item);
    }

    [Fact]
    public void Constructor_ThrowsForNonPowerOfTwo()
    {
        Assert.Throws<ArgumentException>(() => new MpscQueue<string>(3));
        Assert.Throws<ArgumentException>(() => new MpscQueue<string>(0));
        Assert.Throws<ArgumentException>(() => new MpscQueue<string>(-1));
    }

    [Fact]
    public void ConcurrentStress_EightProducers_NoLoss()
    {
        const int producerCount = 8;
        const int itemsPerProducer = 10_000;
        const int capacity = 131072; // next power of 2 above 80000
        MpscQueue<Entry> queue = new MpscQueue<Entry>(capacity);

        Thread[] producers = new Thread[producerCount];
        Barrier barrier = new Barrier(producerCount + 1);

        for (int p = 0; p < producerCount; p++)
        {
            int producerId = p;
            producers[p] = new Thread(() =>
            {
                barrier.SignalAndWait();
                for (int seq = 0; seq < itemsPerProducer; seq++)
                    queue.TryEnqueue(new Entry(producerId, seq));
            });
            producers[p].IsBackground = true;
            producers[p].Start();
        }

        barrier.SignalAndWait(); // release all producers simultaneously

        foreach (Thread t in producers)
            t.Join(TimeSpan.FromSeconds(8));

        List<Entry> dequeued = new List<Entry>(producerCount * itemsPerProducer);
        while (queue.TryDequeue(out Entry? item))
            dequeued.Add(item!);

        Assert.Equal(producerCount * itemsPerProducer, dequeued.Count);
        Assert.Equal(0, queue.DroppedCount);

        // Each producer's items must be in their own order
        for (int p = 0; p < producerCount; p++)
        {
            List<int> seqs = dequeued
                .Where(x => x.Producer == p)
                .Select(x => x.Seq)
                .ToList();

            Assert.Equal(itemsPerProducer, seqs.Count);

            for (int i = 0; i < seqs.Count - 1; i++)
                Assert.True(seqs[i] < seqs[i + 1],
                    $"Producer {p}: out-of-order at index {i}: {seqs[i]} then {seqs[i + 1]}");
        }
    }
}
