using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using AStarNet.Heuristics;
using AStarNet.Maps;
using AStarNet.TieBreakers;

namespace AStarNet;

/// <summary>
/// Finds least-cost paths through a node map using the A* algorithm.
/// </summary>
/// <remarks>
/// When no heuristic provider is supplied, the pathfinder uses an estimate of zero and behaves like Dijkstra's
/// algorithm. When no tie-breaker provider is supplied, equal-score candidates and equal-cost parent alternatives are
/// not ordered explicitly.
/// Each instance retains the node map and optional providers supplied at construction for its entire lifetime.
/// Concurrent calls are safe when <see cref="NodeMap"/>, <see cref="HeuristicProvider"/>, and
/// <see cref="TieBreakerProvider"/> are themselves safe for concurrent use. Each search keeps all mutable state local
/// to the invocation.
/// </remarks>
public sealed class PathFinder
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="PathFinder"/> class.
    /// </summary>
    /// <param name="nodeMap">The node map used for pathfinding.</param>
    /// <param name="heuristicProvider">
    /// The optional heuristic provider. When omitted, every heuristic estimate is zero and the search behaves like
    /// Dijkstra's algorithm.
    /// </param>
    /// <param name="tieBreakerProvider">
    /// The optional provider used to order equal-score candidates and equal-cost parent alternatives.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="nodeMap"/> is <see langword="null"/>.</exception>
    public PathFinder(
        INodeMap nodeMap,
        IHeuristicProvider? heuristicProvider = null,
        ITieBreakerProvider? tieBreakerProvider = null)
    {
        ArgumentNullException.ThrowIfNull(nodeMap);

        this.NodeMap = nodeMap;
        this.HeuristicProvider = heuristicProvider;
        this.TieBreakerProvider = tieBreakerProvider;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the node map used for pathfinding.
    /// </summary>
    public INodeMap NodeMap { get; }

    /// <summary>
    /// Gets the heuristic provider used to estimate remaining costs, or <see langword="null"/> when every estimate is
    /// zero.
    /// </summary>
    public IHeuristicProvider? HeuristicProvider { get; }

    /// <summary>
    /// Gets the provider used to order equal-score candidates and equal-cost parent alternatives, or
    /// <see langword="null"/> when ties are not resolved explicitly.
    /// </summary>
    public ITieBreakerProvider? TieBreakerProvider { get; }

    #endregion

    #region Public methods

    /// <summary>
    /// Finds a path between the specified nodes.
    /// </summary>
    /// <param name="startNodeId">The identifier of the start node.</param>
    /// <param name="destinationNodeId">The identifier of the destination node.</param>
    /// <param name="cancellationToken">A token used to cancel the search.</param>
    /// <returns>
    /// The path found, or <see cref="Path.Empty"/> when no path exists. The path is guaranteed to be optimal when the
    /// configured heuristic is admissible for the node map.
    /// </returns>
    /// <exception cref="KeyNotFoundException">The start or destination node does not exist.</exception>
    /// <exception cref="InvalidOperationException">
    /// The node map returns an invalid connection sequence, the heuristic provider returns an invalid value, or an
    /// accumulated cost or search score is not finite.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
    public Path FindPath(int startNodeId, int destinationNodeId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!this.NodeMap.ContainsNode(startNodeId))
            throw new KeyNotFoundException($"Start node with ID '{startNodeId}' was not found.");

        if (startNodeId == destinationNodeId)
            return new Path([new PathStep(startNodeId, 0, 0)]);

        if (!this.NodeMap.ContainsNode(destinationNodeId))
            throw new KeyNotFoundException($"Destination node with ID '{destinationNodeId}' was not found.");

        ITieBreakerProvider? tieBreakerProvider = this.TieBreakerProvider;

        return tieBreakerProvider is null
            ? this.FindPathWithoutTieBreaker(startNodeId, destinationNodeId, cancellationToken)
            : this.FindPathWithTieBreaker(
                startNodeId,
                destinationNodeId,
                tieBreakerProvider,
                cancellationToken);
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Finds a path using the minimal queue representation when no tie-breaker is configured.
    /// </summary>
    /// <param name="startNodeId">The identifier of the validated start node.</param>
    /// <param name="destinationNodeId">The identifier of the validated destination node.</param>
    /// <param name="cancellationToken">A token used to cancel the search.</param>
    /// <returns>The path found, or <see cref="Path.Empty"/> when no path exists.</returns>
    private Path FindPathWithoutTieBreaker(
        int startNodeId,
        int destinationNodeId,
        CancellationToken cancellationToken)
    {
        Dictionary<int, SearchState> searchStates = [];
        PriorityQueue<int, double> openNodeIds = new();

        double startHeuristic = this.GetValidatedHeuristic(startNodeId, destinationNodeId);
        SearchState startState = new(null, 0, 0, startHeuristic);

        searchStates.Add(startNodeId, startState);
        openNodeIds.Enqueue(startNodeId, startState.Score);

        while (openNodeIds.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            openNodeIds.TryDequeue(out int currentNodeId, out double queuedPriority);
            SearchState currentState = searchStates[currentNodeId];

            // A better route may leave an older entry in the non-indexed priority queue.
            if (queuedPriority > currentState.Score)
                continue;

            if (currentNodeId == destinationNodeId)
                return PathFinder.BuildPath(currentNodeId, searchStates);

            IEnumerable<PathConnection> connections = this.NodeMap.GetConnections(currentNodeId)
                ?? throw new InvalidOperationException(
                    $"The node map returned a null connection sequence for node '{currentNodeId}'.");

            foreach (PathConnection connection in connections)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Connection destinations belong to the provider's graph and are consumed as declared.
                int childNodeId = connection.DestinationNodeId;
                double candidateCost = currentState.CostFromStart + connection.Cost;

                if (!double.IsFinite(candidateCost))
                {
                    throw new InvalidOperationException(
                        $"The accumulated cost from node '{currentNodeId}' to node '{childNodeId}' is not finite.");
                }

                bool hasKnownState = searchStates.TryGetValue(childNodeId, out SearchState knownState);

                if (hasKnownState && candidateCost >= knownState.CostFromStart)
                    continue;

                double heuristicDistance = this.GetValidatedHeuristic(childNodeId, destinationNodeId);
                double score = candidateCost + heuristicDistance;

                if (!double.IsFinite(score))
                    throw new InvalidOperationException($"The search score for node '{childNodeId}' is not finite.");

                SearchState childState = new(currentNodeId, connection.Cost, candidateCost, score);

                searchStates[childNodeId] = childState;
                openNodeIds.Enqueue(childNodeId, childState.Score);
            }
        }

        return Path.Empty;
    }

    /// <summary>
    /// Finds a path while resolving equal-score candidates and equal-cost parents through a tie-breaker provider.
    /// </summary>
    /// <param name="startNodeId">The identifier of the validated start node.</param>
    /// <param name="destinationNodeId">The identifier of the validated destination node.</param>
    /// <param name="tieBreakerProvider">The provider used to resolve candidate and parent ties.</param>
    /// <param name="cancellationToken">A token used to cancel the search.</param>
    /// <returns>The path found, or <see cref="Path.Empty"/> when no path exists.</returns>
    private Path FindPathWithTieBreaker(
        int startNodeId,
        int destinationNodeId,
        ITieBreakerProvider tieBreakerProvider,
        CancellationToken cancellationToken)
    {
        Dictionary<int, SearchState> searchStates = [];
        SearchPriorityComparer priorityComparer = new(startNodeId, destinationNodeId, tieBreakerProvider);
        PriorityQueue<int, SearchPriority> openNodeIds = new(priorityComparer);
        double? destinationScore = null;

        double startHeuristic = this.GetValidatedHeuristic(startNodeId, destinationNodeId);
        SearchState startState = new(null, 0, 0, startHeuristic);

        searchStates.Add(startNodeId, startState);
        openNodeIds.Enqueue(startNodeId, new SearchPriority(startNodeId, startState.Score));

        while (openNodeIds.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            openNodeIds.TryDequeue(out int currentNodeId, out SearchPriority queuedPriority);

            if (destinationScore.HasValue && queuedPriority.Score > destinationScore.Value)
                break;

            SearchState currentState = searchStates[currentNodeId];

            // A better route may leave an older entry in the non-indexed priority queue.
            if (queuedPriority.Score > currentState.Score)
                continue;

            if (currentNodeId == destinationNodeId)
            {
                destinationScore = currentState.Score;
                continue;
            }

            IEnumerable<PathConnection> connections = this.NodeMap.GetConnections(currentNodeId)
                ?? throw new InvalidOperationException(
                    $"The node map returned a null connection sequence for node '{currentNodeId}'.");

            foreach (PathConnection connection in connections)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Connection destinations belong to the provider's graph and are consumed as declared.
                int childNodeId = connection.DestinationNodeId;
                double candidateCost = currentState.CostFromStart + connection.Cost;

                if (!double.IsFinite(candidateCost))
                {
                    throw new InvalidOperationException(
                        $"The accumulated cost from node '{currentNodeId}' to node '{childNodeId}' is not finite.");
                }

                bool hasKnownState = searchStates.TryGetValue(childNodeId, out SearchState knownState);

                if (hasKnownState && candidateCost > knownState.CostFromStart)
                    continue;

                if (hasKnownState && candidateCost == knownState.CostFromStart)
                {
                    // The start state is the only state without a parent and remains canonical through zero-cost cycles.
                    if (childNodeId == startNodeId)
                        continue;

                    int knownParentId = knownState.ParentId!.Value;
                    int parentComparison = tieBreakerProvider.BreakTie(
                        startNodeId,
                        destinationNodeId,
                        currentNodeId,
                        knownParentId);

                    if (parentComparison >= 0)
                        continue;

                    if (connection.Cost == 0 &&
                        PathFinder.WouldCreateParentCycle(childNodeId, currentNodeId, searchStates))
                    {
                        continue;
                    }

                    searchStates[childNodeId] = new SearchState(
                        currentNodeId,
                        connection.Cost,
                        candidateCost,
                        knownState.Score);
                    continue;
                }

                double heuristicDistance = this.GetValidatedHeuristic(childNodeId, destinationNodeId);
                double score = candidateCost + heuristicDistance;

                if (!double.IsFinite(score))
                    throw new InvalidOperationException($"The search score for node '{childNodeId}' is not finite.");

                SearchState childState = new(currentNodeId, connection.Cost, candidateCost, score);

                searchStates[childNodeId] = childState;
                openNodeIds.Enqueue(childNodeId, new SearchPriority(childNodeId, childState.Score));
            }
        }

        return destinationScore.HasValue
            ? PathFinder.BuildPath(destinationNodeId, searchStates)
            : Path.Empty;
    }

    /// <summary>
    /// Determines whether assigning a candidate parent would create a cycle in the path-reconstruction chain.
    /// </summary>
    /// <param name="childNodeId">The node whose parent would be replaced.</param>
    /// <param name="candidateParentNodeId">The proposed parent node.</param>
    /// <param name="searchStates">The best known state for every discovered node.</param>
    /// <returns><see langword="true"/> when the assignment would create a cycle; otherwise, <see langword="false"/>.</returns>
    private static bool WouldCreateParentCycle(
        int childNodeId,
        int candidateParentNodeId,
        Dictionary<int, SearchState> searchStates)
    {
        int? ancestorNodeId = candidateParentNodeId;

        while (ancestorNodeId.HasValue)
        {
            if (ancestorNodeId.Value == childNodeId)
                return true;

            ancestorNodeId = searchStates[ancestorNodeId.Value].ParentId;
        }

        return false;
    }

    /// <summary>
    /// Gets and validates the heuristic estimate between two nodes.
    /// </summary>
    /// <param name="fromNodeId">The node from which the cost is estimated.</param>
    /// <param name="toNodeId">The destination node.</param>
    /// <returns>The finite, non-negative heuristic estimate.</returns>
    private double GetValidatedHeuristic(int fromNodeId, int toNodeId)
    {
        IHeuristicProvider? heuristicProvider = this.HeuristicProvider;

        if (heuristicProvider is null)
            return 0;

        double heuristic = heuristicProvider.GetHeuristic(fromNodeId, toNodeId);

        if (!double.IsFinite(heuristic) || heuristic < 0)
        {
            throw new InvalidOperationException(
                $"The heuristic provider returned an invalid value '{heuristic}' for the estimate from node " +
                $"'{fromNodeId}' to node '{toNodeId}'. Heuristic values must be finite and non-negative.");
        }

        return heuristic;
    }

    /// <summary>
    /// Reconstructs a path from the best known search states.
    /// </summary>
    /// <param name="destinationNodeId">The destination-node identifier.</param>
    /// <param name="searchStates">The best known state for every discovered node.</param>
    /// <returns>The reconstructed path.</returns>
    private static Path BuildPath(int destinationNodeId, Dictionary<int, SearchState> searchStates)
    {
        int stepCount = 0;
        int? currentNodeId = destinationNodeId;

        while (currentNodeId.HasValue)
        {
            SearchState currentState = searchStates[currentNodeId.Value];
            stepCount++;
            currentNodeId = currentState.ParentId;
        }

        ImmutableArray<PathStep>.Builder stepBuilder = ImmutableArray.CreateBuilder<PathStep>(stepCount);
        stepBuilder.Count = stepCount;
        currentNodeId = destinationNodeId;

        for (int index = stepCount - 1; index >= 0; index--)
        {
            SearchState state = searchStates[currentNodeId!.Value];
            stepBuilder[index] = new PathStep(currentNodeId.Value, state.CostFromPrevious, state.CostFromStart);
            currentNodeId = state.ParentId;
        }

        return new Path(stepBuilder.MoveToImmutable());
    }

    #endregion

    #region Nested types

    /// <summary>
    /// Compares search priorities and delegates equal-score candidates to the configured tie-breaker provider.
    /// </summary>
    private sealed class SearchPriorityComparer : IComparer<SearchPriority>
    {
        #region Fields

        private readonly int _startNodeId;
        private readonly int _destinationNodeId;
        private readonly ITieBreakerProvider _tieBreakerProvider;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchPriorityComparer"/> class.
        /// </summary>
        /// <param name="startNodeId">The identifier of the search's start node.</param>
        /// <param name="destinationNodeId">The identifier of the search's destination node.</param>
        /// <param name="tieBreakerProvider">The provider used to resolve equal scores.</param>
        public SearchPriorityComparer(
            int startNodeId,
            int destinationNodeId,
            ITieBreakerProvider tieBreakerProvider)
        {
            this._startNodeId = startNodeId;
            this._destinationNodeId = destinationNodeId;
            this._tieBreakerProvider = tieBreakerProvider;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Compares two search priorities.
        /// </summary>
        /// <param name="left">The first priority.</param>
        /// <param name="right">The second priority.</param>
        /// <returns>A value indicating the relative ordering of the priorities.</returns>
        public int Compare(SearchPriority left, SearchPriority right)
        {
            int scoreComparison = left.Score.CompareTo(right.Score);

            if (scoreComparison != 0)
                return scoreComparison;

            return this._tieBreakerProvider.BreakTie(
                this._startNodeId,
                this._destinationNodeId,
                left.NodeId,
                right.NodeId);
        }

        #endregion
    }

    /// <summary>
    /// Associates a node identifier with its A* score for priority-queue ordering.
    /// </summary>
    private readonly struct SearchPriority
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchPriority"/> struct.
        /// </summary>
        /// <param name="nodeId">The candidate-node identifier.</param>
        /// <param name="score">The candidate's A* score.</param>
        public SearchPriority(int nodeId, double score)
        {
            this.NodeId = nodeId;
            this.Score = score;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the candidate-node identifier.
        /// </summary>
        public int NodeId { get; }

        /// <summary>
        /// Gets the candidate's A* score.
        /// </summary>
        public double Score { get; }

        #endregion
    }

    /// <summary>
    /// Represents the best known search state for a node identifier.
    /// </summary>
    private readonly struct SearchState
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchState"/> struct.
        /// </summary>
        /// <param name="parentId">The preceding node identifier, or <see langword="null"/> for the start node.</param>
        /// <param name="costFromPrevious">The traversal cost from the preceding node.</param>
        /// <param name="costFromStart">The accumulated cost from the start node.</param>
        /// <param name="score">The total estimated score used as the queue priority.</param>
        public SearchState(int? parentId, double costFromPrevious, double costFromStart, double score)
        {
            this.ParentId = parentId;
            this.CostFromPrevious = costFromPrevious;
            this.CostFromStart = costFromStart;
            this.Score = score;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the preceding node identifier, or <see langword="null"/> for the start node.
        /// </summary>
        public int? ParentId { get; }

        /// <summary>
        /// Gets the traversal cost from the preceding search node.
        /// </summary>
        public double CostFromPrevious { get; }

        /// <summary>
        /// Gets the accumulated cost from the start node.
        /// </summary>
        public double CostFromStart { get; }

        /// <summary>
        /// Gets the queue priority associated with this state.
        /// </summary>
        public double Score { get; }

        #endregion
    }

    #endregion
}
