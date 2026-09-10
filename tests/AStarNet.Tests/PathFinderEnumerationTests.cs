using System.Collections;

namespace AStarNet.Tests;

/// <summary>
/// Tests searches with collections and custom enumerators.
/// </summary>
public sealed class PathFinderEnumerationTests
{
    /// <summary>
    /// Verifies that a cheaper route replaces the previous route for arrays, lists, and iterators.
    /// </summary>
    /// <param name="representation">The connection representation to return.</param>
    /// <param name="useTieBreaker">Whether to use the tie-breaking search loop.</param>
    [Theory]
    [InlineData("array", false)]
    [InlineData("array", true)]
    [InlineData("list", false)]
    [InlineData("list", true)]
    [InlineData("yield", false)]
    [InlineData("yield", true)]
    public void FindPath_WithDifferentRepresentations_PreservesSearch(string representation, bool useTieBreaker)
    {
        TestGraph graph = new([0, 1, 2, 3], (0, 1, 5), (0, 2, 1), (2, 1, 1), (1, 3, 1));
        List<int> expandedNodeIds = [];
        DelegateNodeMap map = new(graph.ContainsNode, nodeId =>
        {
            expandedNodeIds.Add(nodeId);
            return PathFinderEnumerationTests.Represent(graph.GetConnections(nodeId)!.ToArray(), representation);
        });
        DelegateTieBreaker? tieBreaker = useTieBreaker
            ? new((_, _, leftNodeId, rightNodeId) => leftNodeId.CompareTo(rightNodeId))
            : null;
        PathFinder pathFinder = new(map, tieBreakerProvider: tieBreaker);

        Path path = pathFinder.FindPath(0, 3, TestContext.Current.CancellationToken);

        Assert.Equal([0, 2, 1, 3], path.Steps.Select(step => step.NodeId));
        Assert.Equal([0, 1, 1, 1], path.Steps.Select(step => step.CostFromPrevious));
        Assert.Equal([0, 1, 2, 3], path.Steps.Select(step => step.CostFromStart));
        Assert.Equal(3, path.Cost);
        Assert.Equal([0, 2, 1], expandedNodeIds);
    }

    /// <summary>
    /// Verifies that a list subclass retains its reimplemented interface enumeration.
    /// </summary>
    /// <param name="useTieBreaker">Whether to use the tie-breaking search loop.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FindPath_WithDerivedList_UsesCustomEnumeration(bool useTieBreaker)
    {
        List<int> estimatedNodeIds = [];
        ReversedConnections connections = new([new PathConnection(1, 1), new PathConnection(2, 2)]);
        DelegateNodeMap map = new(_ => true, nodeId => nodeId == 0 ? connections : []);
        DelegateHeuristic heuristic = new((fromNodeId, _) =>
        {
            estimatedNodeIds.Add(fromNodeId);
            return 0;
        });
        DelegateTieBreaker? tieBreaker = useTieBreaker
            ? new((_, _, leftNodeId, rightNodeId) => leftNodeId.CompareTo(rightNodeId))
            : null;
        PathFinder pathFinder = new(map, heuristic, tieBreaker);

        Path path = pathFinder.FindPath(0, 3, TestContext.Current.CancellationToken);

        Assert.Same(Path.Empty, path);
        Assert.Equal([0, 2, 1], estimatedNodeIds);
    }

    /// <summary>
    /// Verifies that a read-only list's enumeration exception reaches the caller.
    /// </summary>
    /// <param name="useTieBreaker">Whether to use the tie-breaking search loop.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FindPath_WithReadOnlyList_PropagatesEnumerationException(bool useTieBreaker)
    {
        InvalidOperationException failure = new("Enumeration failed.");
        DelegateNodeMap map = new(_ => true, _ => new ThrowingReadOnlyConnections(failure));
        DelegateTieBreaker? tieBreaker = useTieBreaker
            ? new((_, _, leftNodeId, rightNodeId) => leftNodeId.CompareTo(rightNodeId))
            : null;
        PathFinder pathFinder = new(map, tieBreakerProvider: tieBreaker);

        InvalidOperationException actual = Assert.Throws<InvalidOperationException>(
            () => pathFinder.FindPath(0, 1, TestContext.Current.CancellationToken));

        Assert.Same(failure, actual);
    }

    /// <summary>
    /// Verifies that cancellation stops lazy enumeration and disposes the active iterator.
    /// </summary>
    /// <param name="useTieBreaker">Whether to use the tie-breaking search loop.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FindPath_WhenLazyEnumerationCancels_DisposesIterator(bool useTieBreaker)
    {
        using CancellationTokenSource cancellation = new();
        bool disposed = false;
        DelegateNodeMap map = new(_ => true, _ => PathFinderEnumerationTests.CancelDuringEnumeration(
            cancellation, () => disposed = true));
        DelegateTieBreaker? tieBreaker = useTieBreaker
            ? new((_, _, leftNodeId, rightNodeId) => leftNodeId.CompareTo(rightNodeId))
            : null;
        PathFinder pathFinder = new(map, tieBreakerProvider: tieBreaker);

        Assert.Throws<OperationCanceledException>(() => pathFinder.FindPath(0, 1, cancellation.Token));
        Assert.True(disposed);
    }

    /// <summary>
    /// Returns connections in the requested representation.
    /// </summary>
    /// <param name="connections">The ordered connections.</param>
    /// <param name="representation">The requested representation.</param>
    /// <returns>The connection sequence.</returns>
    private static IEnumerable<PathConnection> Represent(PathConnection[] connections, string representation)
    {
        return representation switch
        {
            "array" => connections,
            "list" => new List<PathConnection>(connections),
            "yield" => PathFinderEnumerationTests.Enumerate(connections),
            _ => throw new ArgumentOutOfRangeException(nameof(representation))
        };
    }

    /// <summary>
    /// Yields connections without materializing another collection.
    /// </summary>
    /// <param name="connections">The ordered connections.</param>
    /// <returns>The lazy sequence.</returns>
    private static IEnumerable<PathConnection> Enumerate(PathConnection[] connections)
    {
        foreach (PathConnection connection in connections)
            yield return connection;
    }

    /// <summary>
    /// Cancels the search as the first connection is yielded and records iterator disposal.
    /// </summary>
    /// <param name="cancellation">The search cancellation source.</param>
    /// <param name="onDisposed">The disposal callback.</param>
    /// <returns>The canceling sequence.</returns>
    private static IEnumerable<PathConnection> CancelDuringEnumeration(
        CancellationTokenSource cancellation, Action onDisposed)
    {
        try
        {
            cancellation.Cancel();
            yield return new PathConnection(1, 1);
            throw new InvalidOperationException("Enumeration continued after cancellation.");
        }
        finally
        {
            onDisposed();
        }
    }

    /// <summary>
    /// Reimplements list interface enumeration in reverse order.
    /// </summary>
    private sealed class ReversedConnections : List<PathConnection>, IEnumerable<PathConnection>
    {
        /// <summary>
        /// Initializes the list from connections in indexed order.
        /// </summary>
        /// <param name="connections">The connections to store.</param>
        public ReversedConnections(IEnumerable<PathConnection> connections) : base(connections)
        {
        }

        /// <inheritdoc/>
        IEnumerator<PathConnection> IEnumerable<PathConnection>.GetEnumerator()
        {
            for (int index = this.Count - 1; index >= 0; index--)
                yield return this[index];
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<PathConnection>)this).GetEnumerator();
    }

    /// <summary>
    /// Provides indexed connections while failing during interface enumeration.
    /// </summary>
    private sealed class ThrowingReadOnlyConnections : IReadOnlyList<PathConnection>
    {
        private readonly InvalidOperationException _failure;

        /// <summary>
        /// Initializes the enumerator failure.
        /// </summary>
        /// <param name="failure">The exception to propagate.</param>
        public ThrowingReadOnlyConnections(InvalidOperationException failure)
        {
            this._failure = failure;
        }

        /// <inheritdoc/>
        public int Count => 1;

        /// <inheritdoc/>
        public PathConnection this[int index] => index == 0
            ? new PathConnection(1, 1)
            : throw new ArgumentOutOfRangeException(nameof(index));

        /// <inheritdoc/>
        public IEnumerator<PathConnection> GetEnumerator() => throw this._failure;

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
