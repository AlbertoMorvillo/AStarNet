// Copyright (c) 2025 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using AStarNet.Heuristics;
using AStarNet.Maps;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AStarNet
{
    /// <summary>
    /// Provides functionality to find an optimal path using the A* algorithm.
    /// </summary>
    /// <typeparam name="TId">The type of the identifier for the nodes in the path.</typeparam>
    /// <typeparam name="TNode">The type of the nodes in the path, implementing <see cref="IPathNode{TId}"/>.</typeparam>
    public class PathFinder<TId, TNode>
        where TId : notnull, IEquatable<TId>
        where TNode : IPathNode<TId>
    {
        #region Classes

        /// <summary>
        /// Represents a node used internally in the A* algorithm during the path search process.
        /// </summary>
        protected class SearchNode
        {
            #region Properties

            /// <summary>
            /// Gets the source node represented by this search node.
            /// </summary>
            public TNode Source { get; }

            /// <summary>
            /// Gets the parent <see cref="SearchNode"/> from which this node was reached during the search process.
            /// </summary>
            public SearchNode? Parent { get; }

            /// <summary>
            /// Gets the accumulated cost from the start node to this node along the path (G).
            /// </summary>
            public double CostFromStart { get; }

            /// <summary>
            /// Gets the heuristic estimated cost from this node to the goal node (H).
            /// </summary>
            public double HeuristicDistance { get; }

            /// <summary>
            /// Gets the total estimated cost from the start node to the goal through this node (F = G + H).
            /// </summary>
            public double Score { get; }

            #endregion

            #region Constructors

            /// <summary>
            /// Initializes a new instance of the <see cref="SearchNode"/> class.
            /// </summary>
            /// <param name="node">The node representing the base node.</param>
            /// <param name="parent">The parent <see cref="SearchNode"/> that led to this node, or <see langword="null"/> if this is the starting node.</param>
            /// <param name="heuristicDistance">The heuristic estimated cost from this node to the goal (H).</param>
            /// <exception cref="ArgumentNullException"><paramref name="node"/> is <see langword="null"/>.</exception>
            public SearchNode(TNode node, SearchNode? parent, double heuristicDistance)
            {
                ArgumentNullException.ThrowIfNull(node);

                this.Source = node;
                this.Parent = parent;
                this.CostFromStart = node.Cost + (parent is null ? 0 : parent.CostFromStart);
                this.HeuristicDistance = heuristicDistance;
                this.Score = this.CostFromStart + heuristicDistance;
            }

            #endregion
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the node map that provides the nodes used for pathfinding.
        /// </summary>
        public INodeMap<TId, TNode> NodeMap { get; }

        /// <summary>
        /// Gets the heuristic provider used to estimate the cost between nodes.
        /// If no provider is specified in the constructor, a default <see cref="ZeroHeuristic{TId, TNode}"/> will be used, making the search behave like Dijkstra's algorithm.
        /// </summary>
        public IHeuristicProvider<TId, TNode> HeuristicProvider { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PathFinder{TId, TNode}"/> class.
        /// </summary>
        /// <param name="nodeMap">The node map that provides the nodes used for pathfinding.</param>
        /// <param name="heuristicProvider">
        /// Optional. The heuristic provider used to estimate the cost between nodes. 
        /// If <see langword="null"/>, the pathfinding will behave like Dijkstra's algorithm, using a default <see cref="ZeroHeuristic{TId, TNode}"/>.
        /// </param>
        public PathFinder(INodeMap<TId, TNode> nodeMap, IHeuristicProvider<TId, TNode>? heuristicProvider = null)
        {
            ArgumentNullException.ThrowIfNull(nodeMap);

            this.NodeMap = nodeMap;
            this.HeuristicProvider = heuristicProvider ?? new ZeroHeuristic<TId, TNode>();
        }

        #endregion

        #region Public methods

        #region Sync

        /// <summary>
        /// Finds a path between the specified start and destination nodes in the map.
        /// </summary>
        /// <param name="startNodeId">The identifier of the start node.</param>
        /// <param name="destinationNodeId">The identifier of the destination node.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. If triggered, the search process will be interrupted and the task will return early.</param>
        /// <returns>
        /// A <see cref="Path{TId, TNode}"/> representing the computed path from the start node to the destination node.
        /// The path consists of nodes generated from the source node map.
        /// Returns <see cref="Path{TId, TNode}.Empty"/> if no path is found.
        /// </returns>
        /// <exception cref="KeyNotFoundException">Thrown if the start or destination node could not be found in the map.</exception>
        public Path<TId, TNode> FindPath(TId startNodeId, TId destinationNodeId, CancellationToken cancellationToken = default)
        {
            // Get the start node from the node map.
            TNode startNode = this.NodeMap.GetNode(startNodeId)
                ?? throw new KeyNotFoundException($"Start node with ID '{startNodeId}' was not found.");

            // Get the destination node from the node map.
            TNode destinationNode = this.NodeMap.GetNode(destinationNodeId)
                ?? throw new KeyNotFoundException($"Destination node with ID '{destinationNodeId}' was not found.");

            return this.FindPath(startNode, destinationNode, cancellationToken);
        }

        #endregion

        #region Async

        /// <summary>
        /// Asynchronously finds a path between the specified start and destination nodes in the map.
        /// </summary>
        /// <param name="startNodeId">The identifier of the start node.</param>
        /// <param name="destinationNodeId">The identifier of the destination node.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. If triggered, the search process will be interrupted and the task will complete early.</param>
        /// <returns>
        /// A task representing the asynchronous operation. When completed, the task result is a <see cref="Path{TId, TNode}"/> representing the computed path from the start node to the destination node.
        /// The path consists of nodes generated from the source node map.
        /// If no path is found, the task result is <see cref="Path{TId, TNode}.Empty"/>.
        /// </returns>
        /// <exception cref="KeyNotFoundException">Thrown when the task is awaited, if the start or destination node could not be found in the map.</exception>
        public Task<Path<TId, TNode>> FindPathAsync(TId startNodeId, TId destinationNodeId, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => this.FindPath(startNodeId, destinationNodeId, cancellationToken), cancellationToken);
        }

        #endregion

        #endregion

        #region Protected methods

        /// <summary>
        /// Finds a path between the specified start and destination nodes in the map.
        /// </summary>
        /// <param name="startNode">The start node.</param>
        /// <param name="destinationNode">The destinationnode.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. If triggered, the search process will be interrupted and the task will return early.</param>
        /// <returns>A <see cref="Path{TId, TNode}"/> representing the computed path from the start node to the destination node. Returns <see cref="Path{TId, TNode}.Empty"/> if no path is found.</returns>
        protected Path<TId, TNode> FindPath(TNode startNode, TNode destinationNode, CancellationToken cancellationToken)
        {
            // If the destination node is isolated (i.e., has no child nodes),
            // return an empty path immediately to avoid fruitless pathfinding attempts.
            if (!this.NodeMap.HasChildNodes(destinationNode))
                return Path<TId, TNode>.Empty;

            // A set to keep track of explored nodes.
            HashSet<TId> closedNodes = [];

            // A priority queue for nodes to explore, ordered by score (F, accumulated cost + heuristic).
            PriorityQueue<SearchNode, double> openNodes = new();

            // The current node, starting from the start node.
            SearchNode? currentNode = new(startNode, null, this.HeuristicProvider.GetHeuristic(startNode, destinationNode));

            // Enqueue the start node.
            openNodes.Enqueue(currentNode, currentNode.Score);

            while (!cancellationToken.IsCancellationRequested && openNodes.Count > 0)
            {
                // Dequeue the node with the lowest score.
                currentNode = openNodes.Dequeue();

                // If we reached the destination, reconstruct and return the path.
                if (currentNode.Source.Equals(destinationNode))
                {
                    List<TNode> pathNodes = [];

                    while (currentNode is not null)
                    {
                        pathNodes.Add(currentNode.Source);
                        currentNode = currentNode.Parent;
                    }

                    pathNodes.Reverse();

                    // Return the path.
                    return new Path<TId, TNode>(Guid.NewGuid(), pathNodes);
                }

                // Mark the current node as explored.
                closedNodes.Add(currentNode.Source.Id);

                // Explore the child nodes.
                foreach (var childNode in this.NodeMap.GetChildNodes(currentNode.Source))
                {
                    // Skip already explored nodes.
                    if (closedNodes.Contains(childNode.Id))
                        continue;

                    // Convert to a search node and get the score.
                    SearchNode searchChildNode = new(childNode, currentNode, this.HeuristicProvider.GetHeuristic(childNode, destinationNode));

                    // Enqueue the child node with its score.
                    openNodes.Enqueue(searchChildNode, searchChildNode.Score);
                }
            }

            // If no path was found, return an empty path.
            return Path<TId, TNode>.Empty;
        }

        #endregion
    }
}
