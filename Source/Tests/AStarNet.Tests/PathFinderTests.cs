namespace AStarNet.Tests;

/// <summary>
/// Tests pathfinding results, cancellation, and validation of provider output.
/// </summary>
public sealed class PathFinderTests
{
    /// <summary>
    /// Verifies that a pathfinder cannot be created without a node map.
    /// </summary>
    [Fact]
    public void Constructor_WhenNodeMapIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new PathFinder(null!));
    }

    /// <summary>
    /// Verifies that omitted optional providers remain absent.
    /// </summary>
    [Fact]
    public void Constructor_WhenOptionalProvidersAreOmitted_StoresNullProviders()
    {
        TestGraph graph = new([0]);
        PathFinder pathFinder = new(graph);

        Assert.Null(pathFinder.HeuristicProvider);
        Assert.Null(pathFinder.TieBreakerProvider);
    }

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
        PathFinder pathFinder = new(graph);

        Path path = pathFinder.FindPath(0, 3, TestContext.Current.CancellationToken);

        Assert.Equal([0, 2, 1, 3], path.Steps.Select(step => step.NodeId));
        Assert.Equal([0, 1, 1, 1], path.Steps.Select(step => step.CostFromPrevious));
        Assert.Equal([0, 1, 2, 3], path.Steps.Select(step => step.CostFromStart));
        Assert.Equal(3, path.Cost);
    }

    /// <summary>
    /// Verifies that parallel connections are evaluated independently and the cheapest one is retained.
    /// </summary>
    [Fact]
    public void FindPath_WhenParallelConnectionsExist_UsesTheCheapestConnection()
    {
        TestGraph graph = new(
            [0, 1],
            (0, 1, 5),
            (0, 1, 1));
        PathFinder pathFinder = new(graph);

        Path path = pathFinder.FindPath(0, 1, TestContext.Current.CancellationToken);

        Assert.Equal([0, 1], path.Steps.Select(step => step.NodeId));
        Assert.Equal(1, path.Cost);
        Assert.Equal(1, path.Steps[1].CostFromPrevious);
    }

    /// <summary>
    /// Verifies that a positive-cost self-loop cannot displace the best known route to its node.
    /// </summary>
    [Fact]
    public void FindPath_WhenGraphContainsPositiveCostSelfLoop_IgnoresTheLoop()
    {
        TestGraph graph = new(
            [0, 1],
            (0, 0, 1),
            (0, 1, 2));
        PathFinder pathFinder = new(graph);

        Path path = pathFinder.FindPath(0, 1, TestContext.Current.CancellationToken);

        Assert.Equal([0, 1], path.Steps.Select(step => step.NodeId));
        Assert.Equal(2, path.Cost);
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
        DelegateHeuristic heuristic = new((fromNodeId, _) => estimates[fromNodeId]);
        PathFinder pathFinder = new(graph, heuristic);

        Path path = pathFinder.FindPath(0, 3, TestContext.Current.CancellationToken);

        Assert.Equal([0, 1, 3], path.Steps.Select(step => step.NodeId));
        Assert.Equal(4, path.Cost);
    }

    /// <summary>
    /// Verifies that a tie-breaker selects between candidates with equal A* scores.
    /// </summary>
    [Fact]
    public void FindPath_WhenCandidateScoresAreEqual_UsesTieBreakerProvider()
    {
        TestGraph graph = new(
            [0, 1, 2, 3],
            (0, 1, 1),
            (0, 2, 1),
            (1, 3, 1),
            (2, 3, 1));
        DelegateTieBreaker tieBreaker = new(
            (startNodeId, destinationNodeId, leftCandidateNodeId, rightCandidateNodeId) =>
            {
                Assert.Equal(0, startNodeId);
                Assert.Equal(3, destinationNodeId);
                return rightCandidateNodeId.CompareTo(leftCandidateNodeId);
            });
        PathFinder pathFinder = new(graph, tieBreakerProvider: tieBreaker);

        Path path = pathFinder.FindPath(0, 3, TestContext.Current.CancellationToken);

        Assert.Equal([0, 2, 3], path.Steps.Select(step => step.NodeId));
    }

    /// <summary>
    /// Verifies that the tie-breaker is not invoked for candidates with different A* scores.
    /// </summary>
    [Fact]
    public void FindPath_WhenCandidateScoresDiffer_DoesNotUseTieBreakerProvider()
    {
        TestGraph graph = new([0, 1, 2], (0, 1, 1), (0, 2, 2));
        DelegateTieBreaker tieBreaker = new(
            (_, _, _, _) => throw new InvalidOperationException("The tie-breaker was invoked."));
        PathFinder pathFinder = new(graph, tieBreakerProvider: tieBreaker);

        Path path = pathFinder.FindPath(0, 1, TestContext.Current.CancellationToken);

        Assert.Equal([0, 1], path.Steps.Select(step => step.NodeId));
    }

    /// <summary>
    /// Verifies that equal-cost parent replacement cannot create a reconstruction cycle.
    /// </summary>
    [Fact]
    public void FindPath_WhenTieBreakerPrefersAZeroCostCycle_PreservesAcyclicParents()
    {
        TestGraph graph = new(
            [0, 1, 2, 3],
            (0, 1, 0),
            (1, 2, 0),
            (2, 1, 0),
            (2, 3, 1));
        DelegateTieBreaker tieBreaker = new((_, _, _, _) => -1);
        PathFinder pathFinder = new(graph, tieBreakerProvider: tieBreaker);

        Path path = pathFinder.FindPath(0, 3, TestContext.Current.CancellationToken);

        Assert.Equal([0, 1, 2, 3], path.Steps.Select(step => step.NodeId));
        Assert.Equal(1, path.Cost);
    }

    /// <summary>
    /// Verifies that equal-score work continues after the destination is first dequeued.
    /// </summary>
    [Fact]
    public void FindPath_WhenDestinationIsDequeuedWithinATiedPlateau_ResolvesEqualCostParent()
    {
        TestGraph graph = new(
            [0, 1, 2, 3],
            (0, 1, 1),
            (0, 2, 1),
            (1, 3, 1),
            (2, 3, 1));
        DelegateHeuristic heuristic = new(
            (fromNodeId, _) => fromNodeId switch
            {
                0 => 2,
                1 => 0.5,
                2 => 1,
                _ => 0
            });
        DelegateTieBreaker tieBreaker = new(
            (_, _, leftCandidateNodeId, rightCandidateNodeId) =>
                rightCandidateNodeId.CompareTo(leftCandidateNodeId));
        PathFinder pathFinder = new(graph, heuristic, tieBreaker);

        Path path = pathFinder.FindPath(0, 3, TestContext.Current.CancellationToken);

        Assert.Equal([0, 2, 3], path.Steps.Select(step => step.NodeId));
    }

    /// <summary>
    /// Verifies that an admissible but inconsistent heuristic can reopen a node with a cheaper route.
    /// </summary>
    [Fact]
    public void FindPath_WhenHeuristicIsInconsistent_ReopensNodeAndReturnsOptimalPath()
    {
        TestGraph graph = new(
            [0, 1, 2, 3],
            (0, 1, 2),
            (0, 2, 1),
            (2, 1, 0.5),
            (1, 3, 1),
            (2, 3, 100));
        Dictionary<int, double> estimates = new()
        {
            [0] = 2.5,
            [1] = 0,
            [2] = 1.5,
            [3] = 0
        };
        DelegateHeuristic heuristic = new((fromNodeId, _) => estimates[fromNodeId]);
        PathFinder pathFinder = new(graph, heuristic);

        Path path = pathFinder.FindPath(0, 3, TestContext.Current.CancellationToken);

        Assert.Equal([0, 2, 1, 3], path.Steps.Select(step => step.NodeId));
        Assert.Equal(2.5, path.Cost);
    }

    /// <summary>
    /// Verifies the single-node path returned when start and destination match.
    /// </summary>
    [Fact]
    public void FindPath_WhenStartEqualsDestination_ReturnsSingleNodePath()
    {
        TestGraph graph = new([4]);
        DelegateHeuristic heuristic = new((_, _) => throw new InvalidOperationException("Heuristic was invoked."));
        PathFinder pathFinder = new(graph, heuristic);

        Path path = pathFinder.FindPath(4, 4, TestContext.Current.CancellationToken);

        Assert.False(path.IsEmpty);
        Assert.Single(path.Steps);
        Assert.Equal(4, path.Steps[0].NodeId);
        Assert.Equal(0, path.Cost);
    }

    /// <summary>
    /// Verifies that child identifiers declared by the map are not checked through the endpoint-existence operation.
    /// </summary>
    [Fact]
    public void FindPath_WhenConnectionsDeclareChildren_ValidatesOnlyRequestedEndpoints()
    {
        List<int> validatedNodeIds = [];
        DelegateNodeMap map = new(
            nodeId =>
            {
                validatedNodeIds.Add(nodeId);
                return nodeId is 0 or 2;
            },
            nodeId => nodeId switch
            {
                0 => [new PathConnection(1, 1)],
                1 => [new PathConnection(2, 1)],
                2 => [],
                _ => null
            });
        PathFinder pathFinder = new(map);

        Path path = pathFinder.FindPath(0, 2, TestContext.Current.CancellationToken);

        Assert.Equal([0, 1, 2], path.Steps.Select(step => step.NodeId));
        Assert.Equal([0, 2], validatedNodeIds);
    }

    /// <summary>
    /// Verifies the empty result used when the destination is unreachable.
    /// </summary>
    [Fact]
    public void FindPath_WhenDestinationIsUnreachable_ReturnsSharedEmptyPath()
    {
        TestGraph graph = new([0, 1, 2], (0, 1, 1));
        PathFinder pathFinder = new(graph);

        Path path = pathFinder.FindPath(0, 2, TestContext.Current.CancellationToken);

        Assert.Same(Path.Empty, path);
    }

    /// <summary>
    /// Verifies that reaching the destination completes the search without requesting its outgoing connections.
    /// </summary>
    [Fact]
    public void FindPath_WhenDestinationIsReached_DoesNotExpandDestination()
    {
        List<int> expandedNodeIds = [];
        DelegateNodeMap map = new(
            nodeId => nodeId is 0 or 1,
            nodeId =>
            {
                expandedNodeIds.Add(nodeId);
                return nodeId switch
                {
                    0 => [new PathConnection(1, 1)],
                    1 => throw new InvalidOperationException("The destination was expanded."),
                    _ => null
                };
            });
        PathFinder pathFinder = new(map);

        Path path = pathFinder.FindPath(0, 1, TestContext.Current.CancellationToken);

        Assert.Equal([0, 1], path.Steps.Select(step => step.NodeId));
        Assert.Equal([0], expandedNodeIds);
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
        PathFinder pathFinder = new(graph);

        Path path = pathFinder.FindPath(0, 2, TestContext.Current.CancellationToken);

        Assert.Equal([0, 1, 2], path.Steps.Select(step => step.NodeId));
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
        PathFinder pathFinder = new(graph);

        Assert.Throws<KeyNotFoundException>(
            () => pathFinder.FindPath(startId, destinationId, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Verifies that a null connection sequence from the node map is rejected.
    /// </summary>
    [Fact]
    public void FindPath_WhenNodeMapReturnsNullConnections_Throws()
    {
        DelegateNodeMap map = new(_ => true, _ => null);
        PathFinder pathFinder = new(map);

        Assert.Throws<InvalidOperationException>(
            () => pathFinder.FindPath(0, 1, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Verifies that a child identifier is rejected when the map later reports it as invalid during expansion.
    /// </summary>
    [Fact]
    public void FindPath_WhenDiscoveredChildCannotBeExpanded_Throws()
    {
        DelegateNodeMap map = new(
            nodeId => nodeId is 0 or 1,
            nodeId => nodeId switch
            {
                0 => [new PathConnection(99, 1)],
                1 => [],
                _ => null
            });
        PathFinder pathFinder = new(map);

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
        PathFinder pathFinder = new(graph, heuristic);

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
        DelegateHeuristic heuristic = new((fromNodeId, _) => fromNodeId == 0 ? 0 : double.NaN);
        PathFinder pathFinder = new(graph, heuristic);

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
        PathFinder pathFinder = new(graph);

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
        DelegateHeuristic heuristic = new((fromNodeId, _) => fromNodeId == 0 ? 0 : double.MaxValue);
        PathFinder pathFinder = new(graph, heuristic);

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
                return true;
            },
            _ => []);
        PathFinder pathFinder = new(map);
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
            _ => true,
            nodeId =>
            {
                cancellationSource.Cancel();
                return [new PathConnection(nodeId + 1, 1)];
            });
        PathFinder pathFinder = new(map);

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
        PathFinder pathFinder = new(graph);
        Task<Path>[] searches = [.. Enumerable.Range(0, 32)
            .Select(_ => Task.Run(
                () => pathFinder.FindPath(0, 2, TestContext.Current.CancellationToken),
                TestContext.Current.CancellationToken))];

        Path[] paths = await Task.WhenAll(searches);

        Assert.All(paths, path => Assert.Equal([0, 1, 2], path.Steps.Select(step => step.NodeId)));
    }
}
