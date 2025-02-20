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
    public class PathFinder<TId> where TId : notnull
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
            public IPathNode<TId> Source { get; }

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
            /// <param name="node">The <see cref="IPathNode{TId}"/> representing the base node.</param>
            /// <param name="parent">The parent <see cref="SearchNode"/> that led to this node, or <see langword="null"/> if this is the starting node.</param>
            /// <param name="heuristicDistance">The heuristic estimated cost from this node to the goal (H).</param>
            /// <exception cref="ArgumentNullException"><paramref name="node"/> is <see langword="null"/>.</exception>
            public SearchNode(IPathNode<TId> node, SearchNode? parent, double heuristicDistance)
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
        /// Gets the <see cref="INodeMap{TId}"/> where to find the path.
        /// </summary>
        public INodeMap<TId> NodeMap { get; }

        /// <summary>
        /// Gets the heuristic provider used to estimate the cost between nodes.
        /// </summary>
        public IHeuristicProvider<TId> HeuristicProvider { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PathFinder{TId}"/> class.
        /// </summary>
        /// <param name="nodeMap">The node map where to find a path.</param>
        /// <param name="heuristicProvider">
        /// Optional. The heuristic provider used to estimate the cost between nodes. 
        /// If <see langword="null"/>, the pathfinding will behave like Dijkstra's algorithm, using a zero heuristic.
        /// </param>
        public PathFinder(INodeMap<TId> nodeMap, IHeuristicProvider<TId>? heuristicProvider = null)
        {
            ArgumentNullException.ThrowIfNull(nodeMap);

            this.NodeMap = nodeMap;
            this.HeuristicProvider = heuristicProvider ?? new ZeroHeuristic<TId>();
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Finds the optimal path between the specified start and destination nodes in the map.
        /// </summary>
        /// <param name="startNode">The start <see cref="IPathNode{TId}"/>.</param>
        /// <param name="destinationNode">The destination <see cref="IPathNode{TId}"/>.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. If triggered, the search process will be interrupted and the task will return early.</param>
        /// <returns>A <see cref="Path{TId}"/> representing the optimal path from the start node to the destination node. Returns <see cref="Path{TId}.Empty"/> if no path is found.</returns>
        protected Path<TId> FindOptimalPath(IPathNode<TId> startNode, IPathNode<TId> destinationNode, CancellationToken cancellationToken)
        {
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
                    List<IPathNode<TId>> pathNodes = [];

                    while (currentNode is not null)
                    {
                        pathNodes.Add(currentNode.Source);
                        currentNode = currentNode.Parent;
                    }

                    pathNodes.Reverse();

                    // Return the path.
                    return new Path<TId>(Guid.NewGuid(), pathNodes);
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
            return Path<TId>.Empty;
        }

        #endregion

        #region Public methods

        #region Sync

        /// <summary>
        /// Finds the optimal path between the specified start and destination nodes in the map.
        /// </summary>
        /// <param name="startNodeId">The identifier of the start node.</param>
        /// <param name="destinationNodeId">The identifier of the destination node.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. If triggered, the search process will be interrupted and the task will return early.</param>
        /// <returns>A <see cref="Path{TId}"/> representing the optimal path from the start node to the destination node. Returns <see cref="Path{TId}.Empty"/> if no path is found.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the start or destination node could not be found in the map.</exception>
        public Path<TId> FindOptimalPath(TId startNodeId, TId destinationNodeId, CancellationToken cancellationToken = default)
        {
            // Get the start node from the node map.
            IPathNode<TId> startNode = this.NodeMap.GetNode(startNodeId)
                ?? throw new KeyNotFoundException($"Start node with ID '{startNodeId}' was not found.");

            // Get the destination node from the node map.
            IPathNode<TId> destinationNode = this.NodeMap.GetNode(destinationNodeId)
                ?? throw new KeyNotFoundException($"Destination node with ID '{destinationNodeId}' was not found.");

            return this.FindOptimalPath(startNode, destinationNode, cancellationToken);
        }

        #endregion

        #region Async

        /// <summary>
        /// Asynchronously finds the optimal path between the specified start and destination nodes in the map.
        /// </summary>
        /// <param name="startNodeId">The identifier of the start node.</param>
        /// <param name="destinationNodeId">The identifier of the destination node.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. If triggered, the search process will be interrupted and the task will complete early.</param>
        /// <returns>
        /// A task representing the asynchronous operation, with a <see cref="Path{TId}"/> representing the optimal path from the start node to the destination node.
        /// Returns <see cref="Path{TId}.Empty"/> if no path is found.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if the start or destination node could not be found in the map.
        /// </exception>
        public Task<Path<TId>> FindOptimalPathAsync(TId startNodeId, TId destinationNodeId, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => FindOptimalPath(startNodeId, destinationNodeId, cancellationToken), cancellationToken);
        }

        #endregion

        #endregion
    }
}
