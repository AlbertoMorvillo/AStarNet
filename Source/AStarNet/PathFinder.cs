// Copyright (c) 2026 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using AStarNet.Heuristics;
using AStarNet.Maps;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace AStarNet;

/// <summary>
/// Provides functionality to find a path using the A* algorithm.
/// </summary>
/// <remarks>
/// The returned path is guaranteed to be optimal when the configured heuristic is admissible for the node map.
/// The default zero heuristic is admissible and makes the search behave like Dijkstra's algorithm.
/// Each search keeps its mutable state local to the invocation, so concurrent calls are safe when
/// <see cref="NodeMap"/> and <see cref="HeuristicProvider"/> are themselves safe for concurrent use. The default
/// zero heuristic is stateless and safe for concurrent use.
/// </remarks>
/// <typeparam name="TContent">The type of the optional node content.</typeparam>
public sealed class PathFinder<TContent>
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="PathFinder{TContent}"/> class.
    /// </summary>
    /// <param name="nodeMap">The node map used for pathfinding.</param>
    /// <param name="heuristicProvider">The optional heuristic provider. When omitted, Dijkstra's algorithm is used.</param>
    public PathFinder(INodeMap<TContent> nodeMap, IHeuristicProvider<TContent>? heuristicProvider = null)
    {
        ArgumentNullException.ThrowIfNull(nodeMap);

        this.NodeMap = nodeMap;
        this.HeuristicProvider = heuristicProvider ?? new ZeroHeuristic<TContent>();
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the node map used for pathfinding.
    /// </summary>
    public INodeMap<TContent> NodeMap { get; }

    /// <summary>
    /// Gets the heuristic provider used to estimate remaining costs.
    /// </summary>
    public IHeuristicProvider<TContent> HeuristicProvider { get; }

    #endregion

    #region Public methods

    /// <summary>
    /// Finds a path between the specified nodes.
    /// </summary>
    /// <param name="startNodeId">The identifier of the start node.</param>
    /// <param name="destinationNodeId">The identifier of the destination node.</param>
    /// <param name="cancellationToken">A token used to cancel the search.</param>
    /// <returns>
    /// The path found, or <see cref="Path{TContent}.Empty"/> when no path exists. The path is guaranteed to be optimal
    /// when the configured heuristic is admissible for the node map.
    /// </returns>
    /// <exception cref="KeyNotFoundException">The start or destination node does not exist.</exception>
    /// <exception cref="InvalidOperationException">
    /// The node map or heuristic provider returns an invalid value, or an accumulated cost is not finite.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
    public Path<TContent> FindPath(int startNodeId, int destinationNodeId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        PathNode<TContent> startNode = this.NodeMap.GetNode(startNodeId)
            ?? throw new KeyNotFoundException($"Start node with ID '{startNodeId}' was not found.");

        if (startNode.Id != startNodeId)
        {
            throw new InvalidOperationException(
                $"The node map returned node '{startNode.Id}' for requested start node '{startNodeId}'.");
        }

        PathNode<TContent> destinationNode = this.NodeMap.GetNode(destinationNodeId)
            ?? throw new KeyNotFoundException($"Destination node with ID '{destinationNodeId}' was not found.");

        if (destinationNode.Id != destinationNodeId)
        {
            throw new InvalidOperationException(
                $"The node map returned node '{destinationNode.Id}' for requested destination node " +
                $"'{destinationNodeId}'.");
        }

        return this.FindPathCore(startNode, destinationNode, cancellationToken);
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Finds a path between two resolved nodes.
    /// </summary>
    /// <param name="startNode">The start node.</param>
    /// <param name="destinationNode">The destination node.</param>
    /// <param name="cancellationToken">A token used to cancel the search.</param>
    /// <returns>
    /// The path found, or <see cref="Path{TContent}.Empty"/> when no path exists. The path is guaranteed to be optimal
    /// when the configured heuristic is admissible for the node map.
    /// </returns>
    /// <exception cref="InvalidOperationException">The heuristic provider returns a negative or non-finite value.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
    private Path<TContent> FindPathCore(PathNode<TContent> startNode, PathNode<TContent> destinationNode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Dictionary<int, SearchState> searchStates = [];
        PriorityQueue<int, double> openNodeIds = new();

        double startHeuristic = this.GetValidatedHeuristic(startNode, destinationNode);
        SearchState startState = new(startNode, null, 0, 0, startHeuristic);

        searchStates.Add(startNode.Id, startState);
        openNodeIds.Enqueue(startNode.Id, startState.Score);

        while (openNodeIds.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            openNodeIds.TryDequeue(out int currentNodeId, out double queuedPriority);
            SearchState currentState = searchStates[currentNodeId];

            if (queuedPriority > currentState.Score)
                continue;

            if (currentNodeId == destinationNode.Id)
                return PathFinder<TContent>.BuildPath(currentNodeId, searchStates);

            IEnumerable<PathConnection<TContent>> connections = this.NodeMap.GetConnections(currentState.Node)
                ?? throw new InvalidOperationException(
                    $"The node map returned a null connection sequence for node '{currentState.Node.Id}'.");

            foreach (PathConnection<TContent> connection in connections)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (connection.Destination is null)
                {
                    throw new InvalidOperationException(
                        $"The node map returned a connection with no destination for node '{currentState.Node.Id}'.");
                }

                if (!double.IsFinite(connection.Cost) || connection.Cost < 0)
                {
                    throw new InvalidOperationException(
                        $"The node map returned an invalid connection cost '{connection.Cost}' from node " +
                        $"'{currentState.Node.Id}' to node '{connection.Destination.Id}'. Connection costs must be " +
                        "finite and non-negative.");
                }

                PathNode<TContent> childNode = connection.Destination;
                double candidateCost = currentState.CostFromStart + connection.Cost;

                if (!double.IsFinite(candidateCost))
                {
                    throw new InvalidOperationException(
                        $"The accumulated cost from node '{currentState.Node.Id}' to node '{childNode.Id}' is not " +
                        "finite.");
                }

                bool hasKnownState = searchStates.TryGetValue(childNode.Id, out SearchState knownState);

                if (hasKnownState && candidateCost >= knownState.CostFromStart)
                    continue;

                double heuristicDistance = this.GetValidatedHeuristic(childNode, destinationNode);
                double score = candidateCost + heuristicDistance;

                if (!double.IsFinite(score))
                {
                    throw new InvalidOperationException(
                        $"The search score for node '{childNode.Id}' is not finite.");
                }

                SearchState childState = new(
                    childNode,
                    currentNodeId,
                    connection.Cost,
                    candidateCost,
                    score);

                searchStates[childNode.Id] = childState;
                openNodeIds.Enqueue(childNode.Id, childState.Score);
            }
        }

        return Path<TContent>.Empty;
    }

    /// <summary>
    /// Gets and validates the heuristic estimate between two nodes.
    /// </summary>
    /// <param name="from">The node from which the cost is estimated.</param>
    /// <param name="to">The destination node.</param>
    /// <returns>The finite, non-negative heuristic estimate.</returns>
    /// <exception cref="InvalidOperationException">The heuristic provider returns a negative or non-finite value.</exception>
    private double GetValidatedHeuristic(PathNode<TContent> from, PathNode<TContent> to)
    {
        double heuristic = this.HeuristicProvider.GetHeuristic(from, to);

        if (!double.IsFinite(heuristic) || heuristic < 0)
        {
            throw new InvalidOperationException(
                $"The heuristic provider returned an invalid value '{heuristic}' for the estimate from node " +
                $"'{from.Id}' to node '{to.Id}'. Heuristic values must be finite and non-negative.");
        }

        return heuristic;
    }

    /// <summary>
    /// Reconstructs a path from the best known search states.
    /// </summary>
    /// <param name="destinationNodeId">The destination node identifier.</param>
    /// <param name="searchStates">The best known state for every discovered node.</param>
    /// <returns>The reconstructed path.</returns>
    private static Path<TContent> BuildPath(int destinationNodeId, Dictionary<int, SearchState> searchStates)
    {
        int stepCount = 0;
        int? currentNodeId = destinationNodeId;

        while (currentNodeId.HasValue)
        {
            SearchState currentState = searchStates[currentNodeId.Value];
            stepCount++;
            currentNodeId = currentState.ParentId;
        }

        ImmutableArray<PathStep<TContent>>.Builder stepBuilder = ImmutableArray.CreateBuilder<PathStep<TContent>>(stepCount);
        stepBuilder.Count = stepCount;
        currentNodeId = destinationNodeId;

        for (int index = stepCount - 1; index >= 0; index--)
        {
            SearchState state = searchStates[currentNodeId!.Value];

            stepBuilder[index] = new PathStep<TContent>(
                state.Node,
                state.CostFromPrevious,
                state.CostFromStart);

            currentNodeId = state.ParentId;
        }

        return new Path<TContent>(stepBuilder.MoveToImmutable());
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
        /// <param name="node">The source node.</param>
        /// <param name="parentId">The preceding node identifier, or <see langword="null"/> for the start node.</param>
        /// <param name="costFromPrevious">The traversal cost from the preceding node.</param>
        /// <param name="costFromStart">The accumulated cost from the start node.</param>
        /// <param name="score">The total estimated score used as the queue priority.</param>
        public SearchState(
            PathNode<TContent> node,
            int? parentId,
            double costFromPrevious,
            double costFromStart,
            double score)
        {
            ArgumentNullException.ThrowIfNull(node);

            this.Node = node;
            this.ParentId = parentId;
            this.CostFromPrevious = costFromPrevious;
            this.CostFromStart = costFromStart;
            this.Score = score;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the node represented by this search state.
        /// </summary>
        public PathNode<TContent> Node { get; }

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
