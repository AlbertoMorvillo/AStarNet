using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using AStarNet.Heuristics;
using AStarNet.Maps;

namespace AStarNet;

/// <summary>
/// Finds least-cost paths through a node map using the A* algorithm.
/// </summary>
/// <remarks>
/// The default zero heuristic is admissible and makes the search behave like Dijkstra's algorithm.
/// Concurrent calls are safe when <see cref="NodeMap"/> and <see cref="HeuristicProvider"/> are themselves safe for
/// concurrent use. Each search keeps all mutable state local to the invocation.
/// </remarks>
public sealed class PathFinder
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="PathFinder"/> class.
    /// </summary>
    /// <param name="nodeMap">The node map used for pathfinding.</param>
    /// <param name="heuristicProvider">The optional heuristic provider. When omitted, Dijkstra's algorithm is used.</param>
    /// <exception cref="ArgumentNullException"><paramref name="nodeMap"/> is <see langword="null"/>.</exception>
    public PathFinder(INodeMap nodeMap, IHeuristicProvider? heuristicProvider = null)
    {
        ArgumentNullException.ThrowIfNull(nodeMap);

        this.NodeMap = nodeMap;
        this.HeuristicProvider = heuristicProvider ?? new ZeroHeuristic();
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the node map used for pathfinding.
    /// </summary>
    public INodeMap NodeMap { get; }

    /// <summary>
    /// Gets the heuristic provider used to estimate remaining costs.
    /// </summary>
    public IHeuristicProvider HeuristicProvider { get; }

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
    /// The node map or heuristic provider returns an invalid value, or an accumulated cost is not finite.
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

    #endregion

    #region Private methods

    /// <summary>
    /// Gets and validates the heuristic estimate between two nodes.
    /// </summary>
    /// <param name="fromNodeId">The node from which the cost is estimated.</param>
    /// <param name="toNodeId">The destination node.</param>
    /// <returns>The finite, non-negative heuristic estimate.</returns>
    private double GetValidatedHeuristic(int fromNodeId, int toNodeId)
    {
        double heuristic = this.HeuristicProvider.GetHeuristic(fromNodeId, toNodeId);

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
