// Copyright (c) 2026 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using AStarNet.Heuristics;
using AStarNet.Maps;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AStarNet;

/// <summary>
/// Provides functionality to find an optimal path using the A* algorithm.
/// </summary>
/// <typeparam name="TContent">The type of the optional node content.</typeparam>
public class PathFinder<TContent>
{
    #region Nested types

    /// <summary>
    /// Represents the best known search state for a node identifier.
    /// </summary>
    protected readonly struct SearchState
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
    /// <returns>The optimal path, or <see cref="Path{TContent}.Empty"/> when no path exists.</returns>
    /// <exception cref="KeyNotFoundException">The start or destination node does not exist.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
    public Path<TContent> FindPath(int startNodeId, int destinationNodeId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        PathNode<TContent> startNode = this.NodeMap.GetNode(startNodeId)
            ?? throw new KeyNotFoundException($"Start node with ID '{startNodeId}' was not found.");

        PathNode<TContent> destinationNode = this.NodeMap.GetNode(destinationNodeId)
            ?? throw new KeyNotFoundException($"Destination node with ID '{destinationNodeId}' was not found.");

        return this.FindPath(startNode, destinationNode, cancellationToken);
    }

    /// <summary>
    /// Asynchronously finds a path between the specified nodes.
    /// </summary>
    /// <param name="startNodeId">The identifier of the start node.</param>
    /// <param name="destinationNodeId">The identifier of the destination node.</param>
    /// <param name="cancellationToken">A token used to cancel the search.</param>
    /// <returns>A task containing the optimal path, or <see cref="Path{TContent}.Empty"/> when no path exists.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when awaited if the start or destination node does not exist.</exception>
    /// <exception cref="OperationCanceledException">Thrown when awaited if <paramref name="cancellationToken"/> was canceled.</exception>
    public Task<Path<TContent>> FindPathAsync(int startNodeId, int destinationNodeId, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => this.FindPath(startNodeId, destinationNodeId, cancellationToken), cancellationToken);
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Finds a path between two resolved nodes.
    /// </summary>
    /// <param name="startNode">The start node.</param>
    /// <param name="destinationNode">The destination node.</param>
    /// <param name="cancellationToken">A token used to cancel the search.</param>
    /// <returns>The optimal path, or <see cref="Path{TContent}.Empty"/> when no path exists.</returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
    protected Path<TContent> FindPath(
        PathNode<TContent> startNode,
        PathNode<TContent> destinationNode,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Dictionary<int, SearchState> searchStates = [];
        PriorityQueue<int, double> openNodeIds = new();

        double startHeuristic = this.HeuristicProvider.GetHeuristic(startNode, destinationNode);
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

            foreach (PathConnection<TContent> connection in this.NodeMap.GetConnections(currentState.Node))
            {
                cancellationToken.ThrowIfCancellationRequested();

                ArgumentNullException.ThrowIfNull(connection.Destination);

                PathNode<TContent> childNode = connection.Destination;
                double candidateCost = currentState.CostFromStart + connection.Cost;

                bool hasKnownState = searchStates.TryGetValue(childNode.Id, out SearchState knownState);
                if (hasKnownState && candidateCost >= knownState.CostFromStart)
                    continue;

                double heuristicDistance = this.HeuristicProvider.GetHeuristic(childNode, destinationNode);
                double score = candidateCost + heuristicDistance;
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

    #endregion

    #region Private methods

    /// <summary>
    /// Reconstructs a path from the best known search states.
    /// </summary>
    /// <param name="destinationNodeId">The destination node identifier.</param>
    /// <param name="searchStates">The best known state for every discovered node.</param>
    /// <returns>The reconstructed path.</returns>
    private static Path<TContent> BuildPath(
        int destinationNodeId,
        IReadOnlyDictionary<int, SearchState> searchStates)
    {
        List<SearchState> pathStates = [];
        int? currentNodeId = destinationNodeId;

        while (currentNodeId.HasValue)
        {
            SearchState currentState = searchStates[currentNodeId.Value];
            pathStates.Add(currentState);
            currentNodeId = currentState.ParentId;
        }

        pathStates.Reverse();

        PathNode<TContent> startNode = pathStates[0].Node;
        List<PathConnection<TContent>> connections = new(pathStates.Count - 1);

        for (int i = 1; i < pathStates.Count; i++)
        {
            SearchState state = pathStates[i];
            connections.Add(new PathConnection<TContent>(state.Node, state.CostFromPrevious));
        }

        return new Path<TContent>(Guid.NewGuid(), startNode, connections);
    }

    #endregion
}
