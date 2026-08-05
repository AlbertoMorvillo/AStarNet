// Copyright (c) 2026 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System.Diagnostics;

namespace AStarNet.QueueBenchmarks;

/// <summary>
/// Compares lazy and indexed priority queue strategies.
/// </summary>
internal static class Program
{
    private const int MeasuredRuns = 7;

    /// <summary>
    /// Runs all benchmark scenarios.
    /// </summary>
    private static void Main()
    {
        Scenario[] scenarios =
        [
            new Scenario(10_000, 0),
            new Scenario(100_000, 0),
            new Scenario(50_000, 12_500),
            new Scenario(50_000, 50_000),
            new Scenario(50_000, 200_000)
        ];

        QueueFactory[] factories =
        [
            new QueueFactory("BCL lazy", capacity => new LazyNodePriorityQueue(capacity)),
            new QueueFactory("Indexed binary", capacity => new IndexedNodePriorityQueue(capacity, 2)),
            new QueueFactory("Indexed quaternary", capacity => new IndexedNodePriorityQueue(capacity, 4))
        ];

        Console.WriteLine("Runtime: " + Environment.Version);
        Console.WriteLine("Processor count: " + Environment.ProcessorCount);
        Console.WriteLine();
        Console.WriteLine("| Nodes | Updates | Queue | Median ms | Allocated MB |");
        Console.WriteLine("|---:|---:|:---|---:|---:|");

        foreach (Scenario scenario in scenarios)
        {
            Workload workload = Workload.Create(scenario.NodeCount, scenario.UpdateCount);

            foreach (QueueFactory factory in factories)
            {
                _ = Program.Run(factory, workload);
                Measurement[] measurements = new Measurement[Program.MeasuredRuns];

                for (int run = 0; run < measurements.Length; run++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    measurements[run] = Program.Run(factory, workload);
                }

                double medianMilliseconds = measurements
                    .Select(measurement => measurement.Elapsed.TotalMilliseconds)
                    .Order()
                    .ElementAt(measurements.Length / 2);
                long medianAllocatedBytes = measurements
                    .Select(measurement => measurement.AllocatedBytes)
                    .Order()
                    .ElementAt(measurements.Length / 2);

                Console.WriteLine(
                    $"| {scenario.NodeCount:N0} | {scenario.UpdateCount:N0} | {factory.Name} | " +
                    $"{medianMilliseconds:F2} | {medianAllocatedBytes / 1_048_576.0:F2} |");
            }
        }

        Program.RunGridBenchmarks(factories);
    }

    /// <summary>
    /// Runs an end-to-end Dijkstra workload on a weighted grid.
    /// </summary>
    /// <param name="factories">The queue factories to compare.</param>
    private static void RunGridBenchmarks(QueueFactory[] factories)
    {
        const int width = 300;
        const int height = 300;
        const int measuredRuns = 5;

        Console.WriteLine();
        Console.WriteLine($"Weighted grid: {width} x {height}");
        Console.WriteLine();
        Console.WriteLine("| Queue | Median ms | Allocated MB | Improved open nodes | Path cost |");
        Console.WriteLine("|:---|---:|---:|---:|---:|");

        double? expectedCost = null;

        foreach (QueueFactory factory in factories)
        {
            _ = Program.RunGrid(factory, width, height);
            GridMeasurement[] measurements = new GridMeasurement[measuredRuns];

            for (int run = 0; run < measuredRuns; run++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                measurements[run] = Program.RunGrid(factory, width, height);
            }

            GridMeasurement middle = measurements
                .OrderBy(measurement => measurement.Elapsed)
                .ElementAt(measurements.Length / 2);

            expectedCost ??= middle.PathCost;
            if (middle.PathCost != expectedCost.Value)
                throw new InvalidOperationException($"Queue '{factory.Name}' produced a different grid path cost.");

            Console.WriteLine(
                $"| {factory.Name} | {middle.Elapsed.TotalMilliseconds:F2} | " +
                $"{middle.AllocatedBytes / 1_048_576.0:F2} | {middle.ImprovedNodeCount:N0} | {middle.PathCost:F2} |");
        }
    }

    /// <summary>
    /// Executes one measured queue workload.
    /// </summary>
    /// <param name="factory">The queue factory.</param>
    /// <param name="workload">The immutable workload.</param>
    /// <returns>The elapsed time and allocated bytes.</returns>
    private static Measurement Run(QueueFactory factory, Workload workload)
    {
        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        Stopwatch stopwatch = Stopwatch.StartNew();
        INodePriorityQueue queue = factory.Create(workload.NodeCount);

        for (int nodeId = 0; nodeId < workload.NodeCount; nodeId++)
        {
            queue.EnqueueOrUpdate(nodeId, workload.InitialPriorities[nodeId]);
        }

        foreach (PriorityUpdate update in workload.Updates)
        {
            queue.EnqueueOrUpdate(update.NodeId, update.Priority);
        }

        bool[] visited = new bool[workload.NodeCount];
        int validCount = 0;
        long checksum = 0;

        while (queue.TryDequeue(out int nodeId, out double priority))
        {
            if (visited[nodeId] || priority != workload.FinalPriorities[nodeId])
                continue;

            visited[nodeId] = true;
            validCount++;
            checksum += nodeId;
        }

        stopwatch.Stop();
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        long expectedChecksum = ((long)workload.NodeCount * (workload.NodeCount - 1)) / 2;

        if (validCount != workload.NodeCount || checksum != expectedChecksum)
            throw new InvalidOperationException($"Queue '{factory.Name}' produced an invalid result.");

        return new Measurement(stopwatch.Elapsed, allocatedBytes);
    }

    /// <summary>
    /// Executes Dijkstra's algorithm on a deterministic weighted grid.
    /// </summary>
    /// <param name="factory">The queue factory.</param>
    /// <param name="width">The grid width.</param>
    /// <param name="height">The grid height.</param>
    /// <returns>The grid-search measurement.</returns>
    private static GridMeasurement RunGrid(QueueFactory factory, int width, int height)
    {
        int nodeCount = checked(width * height);
        int destinationId = nodeCount - 1;
        double[] bestCosts = new double[nodeCount];
        Array.Fill(bestCosts, double.PositiveInfinity);

        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        Stopwatch stopwatch = Stopwatch.StartNew();
        INodePriorityQueue queue = factory.Create(nodeCount);
        int improvedNodeCount = 0;

        bestCosts[0] = 0;
        queue.EnqueueOrUpdate(0, 0);

        while (queue.TryDequeue(out int nodeId, out double queuedCost))
        {
            if (queuedCost > bestCosts[nodeId])
                continue;

            if (nodeId == destinationId)
                break;

            int x = nodeId % width;
            int y = nodeId / width;

            if (x > 0)
                Program.RelaxGridNode(nodeId, nodeId - 1, queuedCost, bestCosts, queue, ref improvedNodeCount);
            if (x + 1 < width)
                Program.RelaxGridNode(nodeId, nodeId + 1, queuedCost, bestCosts, queue, ref improvedNodeCount);
            if (y > 0)
                Program.RelaxGridNode(nodeId, nodeId - width, queuedCost, bestCosts, queue, ref improvedNodeCount);
            if (y + 1 < height)
                Program.RelaxGridNode(nodeId, nodeId + width, queuedCost, bestCosts, queue, ref improvedNodeCount);
        }

        stopwatch.Stop();
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        return new GridMeasurement(
            stopwatch.Elapsed,
            allocatedBytes,
            improvedNodeCount,
            bestCosts[destinationId]);
    }

    /// <summary>
    /// Relaxes one weighted grid connection.
    /// </summary>
    /// <param name="sourceId">The current node identifier.</param>
    /// <param name="destinationId">The neighboring node identifier.</param>
    /// <param name="currentCost">The cost at the current node.</param>
    /// <param name="bestCosts">The best known costs.</param>
    /// <param name="queue">The open-node queue.</param>
    /// <param name="improvedNodeCount">The number of improvements to already discovered nodes.</param>
    private static void RelaxGridNode(
        int sourceId,
        int destinationId,
        double currentCost,
        double[] bestCosts,
        INodePriorityQueue queue,
        ref int improvedNodeCount)
    {
        uint edgeHash = unchecked(((uint)sourceId * 2_654_435_761u) ^ ((uint)destinationId * 2_246_822_519u));
        double connectionCost = 1 + (edgeHash % 100) * 0.1;
        double candidateCost = currentCost + connectionCost;
        double knownCost = bestCosts[destinationId];

        if (candidateCost >= knownCost)
            return;

        if (double.IsFinite(knownCost))
            improvedNodeCount++;

        bestCosts[destinationId] = candidateCost;
        queue.EnqueueOrUpdate(destinationId, candidateCost);
    }

    /// <summary>
    /// Describes a benchmark scenario.
    /// </summary>
    private readonly struct Scenario
    {
        /// <summary>
        /// Initializes a benchmark scenario.
        /// </summary>
        /// <param name="nodeCount">The number of unique node identifiers.</param>
        /// <param name="updateCount">The number of priority improvements.</param>
        public Scenario(int nodeCount, int updateCount)
        {
            this.NodeCount = nodeCount;
            this.UpdateCount = updateCount;
        }

        /// <summary>
        /// Gets the number of unique node identifiers.
        /// </summary>
        public int NodeCount { get; }

        /// <summary>
        /// Gets the number of priority improvements.
        /// </summary>
        public int UpdateCount { get; }
    }

    /// <summary>
    /// Describes one queue implementation factory.
    /// </summary>
    private readonly struct QueueFactory
    {
        /// <summary>
        /// Initializes a queue factory.
        /// </summary>
        /// <param name="name">The queue name.</param>
        /// <param name="create">The queue construction delegate.</param>
        public QueueFactory(string name, Func<int, INodePriorityQueue> create)
        {
            this.Name = name;
            this.Create = create;
        }

        /// <summary>
        /// Gets the queue name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the queue construction delegate.
        /// </summary>
        public Func<int, INodePriorityQueue> Create { get; }
    }

    /// <summary>
    /// Describes one benchmark measurement.
    /// </summary>
    private readonly struct Measurement
    {
        /// <summary>
        /// Initializes a benchmark measurement.
        /// </summary>
        /// <param name="elapsed">The elapsed time.</param>
        /// <param name="allocatedBytes">The bytes allocated by the current thread.</param>
        public Measurement(TimeSpan elapsed, long allocatedBytes)
        {
            this.Elapsed = elapsed;
            this.AllocatedBytes = allocatedBytes;
        }

        /// <summary>
        /// Gets the elapsed time.
        /// </summary>
        public TimeSpan Elapsed { get; }

        /// <summary>
        /// Gets the allocated bytes.</summary>
        public long AllocatedBytes { get; }
    }

    /// <summary>
    /// Describes one end-to-end grid measurement.
    /// </summary>
    private readonly struct GridMeasurement
    {
        /// <summary>
        /// Initializes a grid measurement.
        /// </summary>
        /// <param name="elapsed">The elapsed time.</param>
        /// <param name="allocatedBytes">The allocated bytes.</param>
        /// <param name="improvedNodeCount">The number of improvements to already discovered nodes.</param>
        /// <param name="pathCost">The resulting path cost.</param>
        public GridMeasurement(
            TimeSpan elapsed,
            long allocatedBytes,
            int improvedNodeCount,
            double pathCost)
        {
            this.Elapsed = elapsed;
            this.AllocatedBytes = allocatedBytes;
            this.ImprovedNodeCount = improvedNodeCount;
            this.PathCost = pathCost;
        }

        /// <summary>
        /// Gets the elapsed time.
        /// </summary>
        public TimeSpan Elapsed { get; }

        /// <summary>
        /// Gets the allocated bytes.
        /// </summary>
        public long AllocatedBytes { get; }

        /// <summary>
        /// Gets the number of improvements to already discovered nodes.
        /// </summary>
        public int ImprovedNodeCount { get; }

        /// <summary>
        /// Gets the resulting path cost.
        /// </summary>
        public double PathCost { get; }
    }
}

/// <summary>
/// Defines the operations shared by benchmark queue strategies.
/// </summary>
internal interface INodePriorityQueue
{
    /// <summary>
    /// Inserts a node or improves its priority.
    /// </summary>
    /// <param name="nodeId">The node identifier.</param>
    /// <param name="priority">The queue priority.</param>
    void EnqueueOrUpdate(int nodeId, double priority);

    /// <summary>
    /// Removes the entry with the lowest priority.
    /// </summary>
    /// <param name="nodeId">The removed node identifier.</param>
    /// <param name="priority">The removed priority.</param>
    /// <returns><see langword="true"/> when an entry was removed; otherwise, <see langword="false"/>.</returns>
    bool TryDequeue(out int nodeId, out double priority);
}

/// <summary>
/// Adapts the BCL priority queue using lazy deletion.
/// </summary>
internal sealed class LazyNodePriorityQueue : INodePriorityQueue
{
    private readonly PriorityQueue<int, double> _queue;

    /// <summary>
    /// Initializes a new queue with the expected node capacity.
    /// </summary>
    /// <param name="capacity">The expected unique node count.</param>
    public LazyNodePriorityQueue(int capacity)
    {
        this._queue = new PriorityQueue<int, double>(capacity);
    }

    /// <inheritdoc/>
    public void EnqueueOrUpdate(int nodeId, double priority)
    {
        this._queue.Enqueue(nodeId, priority);
    }

    /// <inheritdoc/>
    public bool TryDequeue(out int nodeId, out double priority)
    {
        return this._queue.TryDequeue(out nodeId, out priority);
    }
}

/// <summary>
/// Implements an indexed d-ary min-heap specialized for integer node identifiers.
/// </summary>
internal sealed class IndexedNodePriorityQueue : INodePriorityQueue
{
    private readonly int _arity;
    private readonly List<Entry> _heap;
    private readonly Dictionary<int, int> _positions;
    private long _nextSequence;

    /// <summary>
    /// Initializes a new indexed queue.
    /// </summary>
    /// <param name="capacity">The expected unique node count.</param>
    /// <param name="arity">The number of children per heap entry.</param>
    public IndexedNodePriorityQueue(int capacity, int arity)
    {
        if (arity < 2)
            throw new ArgumentOutOfRangeException(nameof(arity));

        this._arity = arity;
        this._heap = new List<Entry>(capacity);
        this._positions = new Dictionary<int, int>(capacity);
    }

    /// <inheritdoc/>
    public void EnqueueOrUpdate(int nodeId, double priority)
    {
        if (this._positions.TryGetValue(nodeId, out int index))
        {
            Entry current = this._heap[index];

            if (priority == current.Priority)
                return;

            this._heap[index] = new Entry(nodeId, priority, current.Sequence);

            if (priority < current.Priority)
                this.MoveUp(index);
            else
                this.MoveDown(index);

            return;
        }

        int newIndex = this._heap.Count;
        this._heap.Add(new Entry(nodeId, priority, this._nextSequence++));
        this._positions.Add(nodeId, newIndex);
        this.MoveUp(newIndex);
    }

    /// <inheritdoc/>
    public bool TryDequeue(out int nodeId, out double priority)
    {
        if (this._heap.Count == 0)
        {
            nodeId = default;
            priority = default;
            return false;
        }

        Entry minimum = this._heap[0];
        int lastIndex = this._heap.Count - 1;
        Entry last = this._heap[lastIndex];

        this._heap.RemoveAt(lastIndex);
        this._positions.Remove(minimum.NodeId);

        if (lastIndex > 0)
        {
            this._heap[0] = last;
            this._positions[last.NodeId] = 0;
            this.MoveDown(0);
        }

        nodeId = minimum.NodeId;
        priority = minimum.Priority;
        return true;
    }

    /// <summary>
    /// Restores the heap invariant toward the root.
    /// </summary>
    /// <param name="index">The entry index.</param>
    private void MoveUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) / this._arity;

            if (this.Compare(index, parentIndex) >= 0)
                return;

            this.Swap(index, parentIndex);
            index = parentIndex;
        }
    }

    /// <summary>
    /// Restores the heap invariant toward the leaves.
    /// </summary>
    /// <param name="index">The entry index.</param>
    private void MoveDown(int index)
    {
        while (true)
        {
            int firstChildIndex = (index * this._arity) + 1;

            if (firstChildIndex >= this._heap.Count)
                return;

            int bestChildIndex = firstChildIndex;
            int childLimit = Math.Min(firstChildIndex + this._arity, this._heap.Count);

            for (int childIndex = firstChildIndex + 1; childIndex < childLimit; childIndex++)
            {
                if (this.Compare(childIndex, bestChildIndex) < 0)
                    bestChildIndex = childIndex;
            }

            if (this.Compare(index, bestChildIndex) <= 0)
                return;

            this.Swap(index, bestChildIndex);
            index = bestChildIndex;
        }
    }

    /// <summary>
    /// Compares two heap entries.
    /// </summary>
    /// <param name="leftIndex">The first entry index.</param>
    /// <param name="rightIndex">The second entry index.</param>
    /// <returns>The relative ordering of the entries.</returns>
    private int Compare(int leftIndex, int rightIndex)
    {
        Entry left = this._heap[leftIndex];
        Entry right = this._heap[rightIndex];
        int priorityComparison = left.Priority.CompareTo(right.Priority);

        return priorityComparison != 0
            ? priorityComparison
            : left.Sequence.CompareTo(right.Sequence);
    }

    /// <summary>
    /// Swaps two entries and their indexed positions.
    /// </summary>
    /// <param name="firstIndex">The first entry index.</param>
    /// <param name="secondIndex">The second entry index.</param>
    private void Swap(int firstIndex, int secondIndex)
    {
        Entry first = this._heap[firstIndex];
        Entry second = this._heap[secondIndex];

        this._heap[firstIndex] = second;
        this._heap[secondIndex] = first;
        this._positions[first.NodeId] = secondIndex;
        this._positions[second.NodeId] = firstIndex;
    }

    /// <summary>
    /// Represents one heap entry.
    /// </summary>
    private readonly struct Entry
    {
        /// <summary>
        /// Initializes a new heap entry.
        /// </summary>
        /// <param name="nodeId">The node identifier.</param>
        /// <param name="priority">The queue priority.</param>
        /// <param name="sequence">The stable insertion sequence.</param>
        public Entry(int nodeId, double priority, long sequence)
        {
            this.NodeId = nodeId;
            this.Priority = priority;
            this.Sequence = sequence;
        }

        /// <summary>
        /// Gets the node identifier.
        /// </summary>
        public int NodeId { get; }

        /// <summary>
        /// Gets the queue priority.
        /// </summary>
        public double Priority { get; }

        /// <summary>
        /// Gets the stable insertion sequence.
        /// </summary>
        public long Sequence { get; }
    }
}

/// <summary>
/// Stores an immutable generated workload.
/// </summary>
internal sealed class Workload
{
    /// <summary>
    /// Initializes a generated workload.
    /// </summary>
    private Workload(
        double[] initialPriorities,
        PriorityUpdate[] updates,
        double[] finalPriorities)
    {
        this.InitialPriorities = initialPriorities;
        this.Updates = updates;
        this.FinalPriorities = finalPriorities;
    }

    /// <summary>
    /// Gets the number of unique nodes.
    /// </summary>
    public int NodeCount => this.InitialPriorities.Length;

    /// <summary>
    /// Gets the initial priorities.
    /// </summary>
    public double[] InitialPriorities { get; }

    /// <summary>
    /// Gets the priority improvements.
    /// </summary>
    public PriorityUpdate[] Updates { get; }

    /// <summary>
    /// Gets the final priority for each node.
    /// </summary>
    public double[] FinalPriorities { get; }

    /// <summary>
    /// Creates a deterministic workload.
    /// </summary>
    /// <param name="nodeCount">The unique node count.</param>
    /// <param name="updateCount">The priority improvement count.</param>
    /// <returns>The generated workload.</returns>
    public static Workload Create(int nodeCount, int updateCount)
    {
        Random random = new(42 + updateCount);
        double[] initialPriorities = new double[nodeCount];
        double[] finalPriorities = new double[nodeCount];

        for (int nodeId = 0; nodeId < nodeCount; nodeId++)
        {
            double priority = 1_000_000 + random.NextDouble() * 1_000_000;
            initialPriorities[nodeId] = priority;
            finalPriorities[nodeId] = priority;
        }

        PriorityUpdate[] updates = new PriorityUpdate[updateCount];

        for (int updateIndex = 0; updateIndex < updateCount; updateIndex++)
        {
            int nodeId = random.Next(nodeCount);
            double priority = finalPriorities[nodeId] - (1 + random.NextDouble() * 100);
            finalPriorities[nodeId] = priority;
            updates[updateIndex] = new PriorityUpdate(nodeId, priority);
        }

        return new Workload(initialPriorities, updates, finalPriorities);
    }
}

/// <summary>
/// Represents one deterministic priority improvement.
/// </summary>
internal readonly struct PriorityUpdate
{
    /// <summary>
    /// Initializes a priority improvement.
    /// </summary>
    /// <param name="nodeId">The node identifier.</param>
    /// <param name="priority">The improved priority.</param>
    public PriorityUpdate(int nodeId, double priority)
    {
        this.NodeId = nodeId;
        this.Priority = priority;
    }

    /// <summary>
    /// Gets the node identifier.
    /// </summary>
    public int NodeId { get; }

    /// <summary>
    /// Gets the improved priority.</summary>
    public double Priority { get; }
}
