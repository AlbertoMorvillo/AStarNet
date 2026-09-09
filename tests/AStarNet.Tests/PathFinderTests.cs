namespace AStarNet.Tests;

/// <summary>
/// Tests pathfinding results, cancellation, and validation of provider output.
/// </summary>
public sealed class PathFinderTests
{
    /// <summary>
    /// Verifies that reconstructed costs follow the final parent chain when rounded scores tie.
    /// </summary>
    /// <param name="useTieBreaker">Whether to use the tie-breaking search loop.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FindPath_WhenRoundedScoresDelayCostPropagation_RecalculatesPathCosts(bool useTieBreaker)
    {
        const double finalConnectionCost = 36028797018963968;
        TestGraph graph = new(
            [0, 1, 2, 3, 4, 5, 6],
            (0, 1, 3),
            (0, 2, 1),
            (1, 3, 0),
            (2, 5, 0),
            (3, 4, 0),
            (4, 6, finalConnectionCost),
            (5, 1, 1));
        DelegateHeuristic heuristic = new(
            (fromNodeId, toNodeId) => fromNodeId == toNodeId ? 0 : finalConnectionCost);
        DelegateTieBreaker? tieBreaker = useTieBreaker
            ? new((_, _, leftNodeId, rightNodeId) => leftNodeId.CompareTo(rightNodeId))
            : null;
        PathFinder pathFinder = new(graph, heuristic, tieBreaker);

        Path path = pathFinder.FindPath(0, 6, TestContext.Current.CancellationToken);

        Assert.Equal([0, 2, 5, 1, 3, 4, 6], path.Steps.Select(step => step.NodeId));
        Assert.Equal([0, 1, 0, 1, 0, 0, finalConnectionCost],
            path.Steps.Select(step => step.CostFromPrevious));
        Assert.Equal([0, 1, 1, 2, 2, 2, finalConnectionCost],
            path.Steps.Select(step => step.CostFromStart));
        Assert.Equal(finalConnectionCost, path.Cost);
    }

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
    /// Verifies that nodes with the same score are processed after the destination is reached.
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
    /// <param name="useTieBreaker">Whether to exercise the search with a tie-breaker.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FindPath_WhenHeuristicIsInconsistent_ReopensNodeAndReturnsOptimalPath(bool useTieBreaker)
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
        Dictionary<int, int> heuristicCallCounts = new();
        DelegateHeuristic heuristic = new((fromNodeId, _) =>
        {
            heuristicCallCounts.TryGetValue(fromNodeId, out int callCount);
            heuristicCallCounts[fromNodeId] = callCount + 1;
            return estimates[fromNodeId];
        });
        DelegateTieBreaker? tieBreaker = useTieBreaker
            ? new((_, _, leftNodeId, rightNodeId) => leftNodeId.CompareTo(rightNodeId))
            : null;
        PathFinder pathFinder = new(graph, heuristic, tieBreaker);

        Path path = pathFinder.FindPath(0, 3, TestContext.Current.CancellationToken);

        Assert.Equal([0, 2, 1, 3], path.Steps.Select(step => step.NodeId));
        Assert.Equal(2.5, path.Cost);
        Assert.Equal(4, heuristicCallCounts.Count);
        Assert.All(heuristicCallCounts.Values, callCount => Assert.Equal(1, callCount));
    }

    /// <summary>
    /// Verifies that a cheaper route preserves an estimate lost to rounding in the previous score.
    /// </summary>
    /// <param name="useTieBreaker">Whether to exercise the search with a tie-breaker.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FindPath_WhenPreviousScoreRoundsAwayHeuristic_PreservesExpansionOrder(bool useTieBreaker)
    {
        const double expensiveConnectionCost = 9007199254740992;
        List<int> expandedNodeIds = [];
        DelegateNodeMap map = new(
            nodeId => nodeId is >= 0 and <= 3,
            nodeId =>
            {
                expandedNodeIds.Add(nodeId);
                return nodeId == 0
                    ? [new PathConnection(1, expensiveConnectionCost), new PathConnection(1, 1),
                        new PathConnection(2, 1.5)]
                    : [];
            });
        DelegateHeuristic heuristic = new((fromNodeId, _) => fromNodeId == 1 ? 1 : 0);
        DelegateTieBreaker? tieBreaker = useTieBreaker
            ? new((_, _, leftNodeId, rightNodeId) => leftNodeId.CompareTo(rightNodeId))
            : null;
        PathFinder pathFinder = new(map, heuristic, tieBreaker);

        Path path = pathFinder.FindPath(0, 3, TestContext.Current.CancellationToken);

        Assert.Same(Path.Empty, path);
        Assert.Equal([0, 2, 1], expandedNodeIds);
    }

    /// <summary>
    /// Verifies that equal-cost parent replacement preserves the estimate for a subsequent cheaper route.
    /// </summary>
    [Fact]
    public void FindPath_WhenEqualCostParentChangesBeforeCheaperRoute_PreservesHeuristic()
    {
        TestGraph graph = new(
            [0, 1, 2, 3, 4, 5, 6],
            (0, 3, 1),
            (0, 2, 1),
            (0, 4, 0.5),
            (0, 5, 3.75),
            (3, 1, 1),
            (2, 1, 1),
            (4, 1, 1));
        List<int> expandedNodeIds = [];
        DelegateNodeMap map = new(graph.ContainsNode, nodeId =>
        {
            expandedNodeIds.Add(nodeId);
            return graph.GetConnections(nodeId);
        });
        DelegateHeuristic heuristic = new((fromNodeId, _) => fromNodeId switch
        {
            1 => 2,
            2 => 1,
            4 => 2.5,
            _ => 0
        });
        DelegateTieBreaker tieBreaker = new(
            (_, _, leftNodeId, rightNodeId) => leftNodeId.CompareTo(rightNodeId));
        PathFinder pathFinder = new(map, heuristic, tieBreaker);

        Path path = pathFinder.FindPath(0, 6, TestContext.Current.CancellationToken);

        Assert.Same(Path.Empty, path);
        Assert.Equal([0, 3, 2, 4, 1, 5], expandedNodeIds);
    }

    /// <summary>
    /// Verifies that procedural map changes and new estimates are observed between separate searches.
    /// </summary>
    /// <param name="useTieBreaker">Whether to exercise the search with a tie-breaker.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FindPath_WhenProvidersChangeBetweenSearches_UsesCurrentMapAndEstimates(bool useTieBreaker)
    {
        double connectionCost = 1;
        double remainingCostEstimate = 1;
        List<double> observedEstimates = [];
        DelegateNodeMap map = new(
            nodeId => nodeId is 0 or 1,
            nodeId => nodeId == 0 ? [new PathConnection(1, connectionCost)] : []);
        DelegateHeuristic heuristic = new((fromNodeId, _) =>
        {
            double estimate = fromNodeId == 0 ? remainingCostEstimate : 0;
            observedEstimates.Add(estimate);
            return estimate;
        });
        DelegateTieBreaker? tieBreaker = useTieBreaker
            ? new((_, _, leftNodeId, rightNodeId) => leftNodeId.CompareTo(rightNodeId))
            : null;
        PathFinder pathFinder = new(map, heuristic, tieBreaker);

        Path firstPath = pathFinder.FindPath(0, 1, TestContext.Current.CancellationToken);
        connectionCost = 2;
        remainingCostEstimate = 2;
        observedEstimates.Clear();
        Path secondPath = pathFinder.FindPath(0, 1, TestContext.Current.CancellationToken);

        Assert.Equal(1, firstPath.Cost);
        Assert.Equal(2, secondPath.Cost);
        Assert.Contains(2.0, observedEstimates);
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
    /// Verifies that ContainsNode is not called for connection destinations.
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
