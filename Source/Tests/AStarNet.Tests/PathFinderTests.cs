// Copyright (c) 2026 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

namespace AStarNet.Tests;

/// <summary>
/// Tests pathfinding results, cancellation, and validation of provider output.
/// </summary>
public sealed class PathFinderTests
{
    /// <summary>
    /// Verifies that a later cheaper route replaces an earlier expensive route.
    /// </summary>
    [Fact]
    public void FindPath_WhenAQueuedNodeReceivesACheaperRoute_ReturnsTheOptimalPath()
    {
        TestGraph graph = new(
            [0, 1, 2, 3],
            (0, 1, 5),
            (0, 2, 1),
            (2, 1, 1),
            (1, 3, 1));
        PathFinder<string> pathFinder = new(graph);

        Path<string> path = pathFinder.FindPath(0, 3, TestContext.Current.CancellationToken);

        Assert.Equal([0, 2, 1, 3], path.Steps.Select(step => step.Node.Id));
        Assert.Equal([0, 1, 1, 1], path.Steps.Select(step => step.CostFromPrevious));
        Assert.Equal([0, 1, 2, 3], path.Steps.Select(step => step.CostFromStart));
        Assert.Equal(3, path.Cost);
    }

    /// <summary>
    /// Verifies that an admissible heuristic preserves the optimal result.
    /// </summary>
    [Fact]
    public void FindPath_WhenHeuristicIsAdmissible_ReturnsTheOptimalPath()
    {
        TestGraph graph = new(
            [0, 1, 2, 3],
            (0, 1, 2),
            (0, 2, 1),
            (1, 3, 2),
            (2, 3, 10));
        Dictionary<int, double> estimates = new()
        {
            [0] = 3,
            [1] = 2,
            [2] = 4,
            [3] = 0
        };
        DelegateHeuristic heuristic = new((from, _) => estimates[from.Id]);
        PathFinder<string> pathFinder = new(graph, heuristic);

        Path<string> path = pathFinder.FindPath(0, 3, TestContext.Current.CancellationToken);

        Assert.Equal([0, 1, 3], path.Steps.Select(step => step.Node.Id));
        Assert.Equal(4, path.Cost);
    }

    /// <summary>
    /// Verifies the single-node path returned when start and destination match.
    /// </summary>
    [Fact]
    public void FindPath_WhenStartEqualsDestination_ReturnsSingleNodePath()
    {
        TestGraph graph = new([4]);
        PathFinder<string> pathFinder = new(graph);

        Path<string> path = pathFinder.FindPath(4, 4, TestContext.Current.CancellationToken);

        Assert.False(path.IsEmpty);
        Assert.Equal(1, path.Count);
        Assert.Equal(4, path[0].Id);
        Assert.Equal(0, path.Cost);
    }

    /// <summary>
    /// Verifies the empty result used when the destination is unreachable.
    /// </summary>
    [Fact]
    public void FindPath_WhenDestinationIsUnreachable_ReturnsSharedEmptyPath()
    {
        TestGraph graph = new([0, 1, 2], (0, 1, 1));
        PathFinder<string> pathFinder = new(graph);

        Path<string> path = pathFinder.FindPath(0, 2, TestContext.Current.CancellationToken);

        Assert.Same(Path<string>.Empty, path);
    }

    /// <summary>
    /// Verifies that zero-cost cycles terminate and do not corrupt the route.
    /// </summary>
    [Fact]
    public void FindPath_WhenGraphContainsZeroCostCycle_TerminatesWithOptimalPath()
    {
        TestGraph graph = new(
            [0, 1, 2],
            (0, 1, 0),
            (1, 0, 0),
            (1, 2, 1));
        PathFinder<string> pathFinder = new(graph);

        Path<string> path = pathFinder.FindPath(0, 2, TestContext.Current.CancellationToken);

        Assert.Equal([0, 1, 2], path.Steps.Select(step => step.Node.Id));
        Assert.Equal(1, path.Cost);
    }

    /// <summary>
    /// Verifies that missing endpoint nodes are reported precisely.
    /// </summary>
    /// <param name="startId">The requested start identifier.</param>
    /// <param name="destinationId">The requested destination identifier.</param>
    [Theory]
    [InlineData(99, 1)]
    [InlineData(0, 99)]
    public void FindPath_WhenAnEndpointDoesNotExist_Throws(int startId, int destinationId)
    {
        TestGraph graph = new([0, 1]);
        PathFinder<string> pathFinder = new(graph);

        Assert.Throws<KeyNotFoundException>(
            () => pathFinder.FindPath(startId, destinationId, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Verifies that endpoint identifiers returned by the node map must match the request.
    /// </summary>
    /// <param name="mismatchedRequest">The request for which the map returns a mismatched node.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void FindPath_WhenNodeMapReturnsMismatchedEndpointId_Throws(int mismatchedRequest)
    {
        DelegateNodeMap map = new(
            id => id == mismatchedRequest ? new PathNode<string>(99) : new PathNode<string>(id),
            _ => []);
        PathFinder<string> pathFinder = new(map);

        Assert.Throws<InvalidOperationException>(
            () => pathFinder.FindPath(0, 1, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Verifies that a null connection sequence from the node map is rejected.
    /// </summary>
    [Fact]
    public void FindPath_WhenNodeMapReturnsNullConnections_Throws()
    {
        DelegateNodeMap map = new(id => new PathNode<string>(id), _ => null!);
        PathFinder<string> pathFinder = new(map);

        Assert.Throws<InvalidOperationException>(
            () => pathFinder.FindPath(0, 1, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Verifies that a default connection with no destination is rejected at the provider boundary.
    /// </summary>
    [Fact]
    public void FindPath_WhenNodeMapReturnsDefaultConnection_Throws()
    {
        DelegateNodeMap map = new(
            id => new PathNode<string>(id),
            node => node.Id == 0 ? [default(PathConnection<string>)] : []);
        PathFinder<string> pathFinder = new(map);

        Assert.Throws<InvalidOperationException>(
            () => pathFinder.FindPath(0, 1, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Verifies validation of heuristic output at the initial node.
    /// </summary>
    /// <param name="estimate">The invalid estimate.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    public void FindPath_WhenInitialHeuristicIsInvalid_Throws(double estimate)
    {
        TestGraph graph = new([0, 1], (0, 1, 1));
        DelegateHeuristic heuristic = new((_, _) => estimate);
        PathFinder<string> pathFinder = new(graph, heuristic);

        Assert.Throws<InvalidOperationException>(
            () => pathFinder.FindPath(0, 1, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Verifies that heuristic output is validated throughout the search, not only at startup.
    /// </summary>
    [Fact]
    public void FindPath_WhenLaterHeuristicIsInvalid_Throws()
    {
        TestGraph graph = new([0, 1, 2], (0, 1, 1), (1, 2, 1));
        DelegateHeuristic heuristic = new((from, _) => from.Id == 0 ? 0 : double.NaN);
        PathFinder<string> pathFinder = new(graph, heuristic);

        Assert.Throws<InvalidOperationException>(
            () => pathFinder.FindPath(0, 2, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Verifies that overflow of the accumulated traversal cost is rejected.
    /// </summary>
    [Fact]
    public void FindPath_WhenAccumulatedCostOverflows_Throws()
    {
        TestGraph graph = new(
            [0, 1, 2],
            (0, 1, double.MaxValue),
            (1, 2, double.MaxValue));
        PathFinder<string> pathFinder = new(graph);

        Assert.Throws<InvalidOperationException>(
            () => pathFinder.FindPath(0, 2, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Verifies that overflow of cost plus heuristic is rejected.
    /// </summary>
    [Fact]
    public void FindPath_WhenPriorityScoreOverflows_Throws()
    {
        TestGraph graph = new([0, 1, 2], (0, 1, double.MaxValue));
        DelegateHeuristic heuristic = new((from, _) => from.Id == 0 ? 0 : double.MaxValue);
        PathFinder<string> pathFinder = new(graph, heuristic);

        Assert.Throws<InvalidOperationException>(
            () => pathFinder.FindPath(0, 2, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Verifies cancellation before any provider is invoked.
    /// </summary>
    [Fact]
    public void FindPath_WhenAlreadyCanceled_ThrowsBeforeReadingTheMap()
    {
        int calls = 0;
        DelegateNodeMap map = new(
            id =>
            {
                calls++;
                return new PathNode<string>(id);
            },
            _ => []);
        PathFinder<string> pathFinder = new(map);
        using CancellationTokenSource cancellationSource = new();
        cancellationSource.Cancel();

        Assert.Throws<OperationCanceledException>(
            () => pathFinder.FindPath(0, 1, cancellationSource.Token));
        Assert.Equal(0, calls);
    }

    /// <summary>
    /// Verifies cooperative cancellation after search work has begun.
    /// </summary>
    [Fact]
    public void FindPath_WhenCanceledDuringSearch_StopsBeforeProcessingConnections()
    {
        using CancellationTokenSource cancellationSource = new();
        DelegateNodeMap map = new(
            id => new PathNode<string>(id),
            node =>
            {
                cancellationSource.Cancel();
                return [new PathConnection<string>(new PathNode<string>(node.Id + 1), 1)];
            });
        PathFinder<string> pathFinder = new(map);

        Assert.Throws<OperationCanceledException>(
            () => pathFinder.FindPath(0, 2, cancellationSource.Token));
    }

    /// <summary>
    /// Verifies safe concurrent searches when the configured providers are safe for concurrent reads.
    /// </summary>
    [Fact]
    public async Task FindPath_WhenCalledConcurrently_DoesNotShareMutableSearchState()
    {
        TestGraph graph = new([0, 1, 2], (0, 1, 1), (1, 2, 1));
        PathFinder<string> pathFinder = new(graph);
        Task<Path<string>>[] searches = [.. Enumerable.Range(0, 32)
            .Select(_ => Task.Run(
                () => pathFinder.FindPath(0, 2, TestContext.Current.CancellationToken),
                TestContext.Current.CancellationToken))];

        Path<string>[] paths = await Task.WhenAll(searches);

        Assert.All(paths, path => Assert.Equal([0, 1, 2], path.Steps.Select(step => step.Node.Id)));
    }
}
