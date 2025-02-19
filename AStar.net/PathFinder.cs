// Copyright (c) 2025 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AStarNet
{
    /// <summary>
    /// Provides functionality to find an optimal path using the A* algorithm.
    /// </summary>
    /// <typeparam name="TExternalId">The type of the external identifier representing a node in the specific domain (e.g., coordinates, room names, graph keys).</typeparam>
    /// <typeparam name="TContent">The type of content associated with the path nodes.</typeparam>
    public class PathFinder<TExternalId, TContent>
    {
        #region Properties

        /// <summary>
        /// Gets the <see cref="INodeMap{TExternalId, TContent}"/> where to find the path.
        /// </summary>
        public INodeMap<TExternalId, TContent> NodeMap { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PathFinder{TExternalId, TContent}"/> class.
        /// </summary>
        /// <param name="nodeMap">The node map where to find a path.</param>
        public PathFinder(INodeMap<TExternalId, TContent> nodeMap)
        {
            ArgumentNullException.ThrowIfNull(nodeMap);

            this.NodeMap = nodeMap;
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Finds the optimal path between the specified start and destination nodes in the map.
        /// </summary>
        /// <param name="startNode">The start <see cref="MapNode{TContent}"/>.</param>
        /// <param name="destinationNode">The destination <see cref="MapNode{TContent}"/>.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. If triggered, the search process will be interrupted and the task will return early.</param>
        /// <returns>A <see cref="Path{TContent}"/> representing the optimal path from the start node to the destination node. Returns <see cref="Path{TContent}.Empty"/> if no path is found.</returns>
        protected Path<TContent> FindOptimalPath(MapNode<TContent> startNode, MapNode<TContent> destinationNode, CancellationToken cancellationToken)
        {
            // A set to keep track of explored nodes.
            HashSet<Guid> closedNodes = [];

            // A priority queue for nodes to explore, ordered by Score (accumulated cost + heuristic).
            PriorityQueue<MapNode<TContent>, double> openNodes = new();

            // Enqueue the start node.
            openNodes.Enqueue(startNode, startNode.Score);

            while (!cancellationToken.IsCancellationRequested && openNodes.Count > 0)
            {
                // Dequeue the node with the lowest score.
                var currentNode = openNodes.Dequeue();

                // If we reached the destination, reconstruct and return the path.
                if (currentNode.Equals(destinationNode))
                {
                    var pathNodes = new List<MapNode<TContent>>();

                    while (currentNode is not null)
                    {
                        pathNodes.Add(currentNode);
                        currentNode = currentNode.Parent;
                    }

                    pathNodes.Reverse();

                    // Return the path.
                    return new Path<TContent>(Guid.NewGuid(), pathNodes);
                }

                // Mark the current node as explored.
                closedNodes.Add(currentNode.Id);

                // Explore the child nodes.
                foreach (var childNode in this.NodeMap.GetChildNodes(currentNode))
                {
                    // Skip already explored nodes.
                    if (closedNodes.Contains(childNode.Id))
                        continue;

                    // Enqueue the child node with its score.
                    openNodes.Enqueue(childNode, childNode.Score);
                }
            }

            // If no path was found, return an empty path.
            return Path<TContent>.Empty;
        }

        #endregion

        #region Public methods

        #region Sync

        /// <summary>
        /// Finds the optimal path between the specified start and destination nodes in the map, identified by external identifiers.
        /// </summary>
        /// <param name="externalStartId">The external identifier of the start node, of type <typeparamref name="TExternalId"/>.</param>
        /// <param name="externalDestinationId">The external identifier of the destination node, of type <typeparamref name="TExternalId"/>.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. If triggered, the search process will be interrupted and the task will return early.</param>
        /// <returns>A <see cref="Path{TContent}"/> representing the optimal path from the start node to the destination node. Returns <see cref="Path{TContent}.Empty"/> if no path is found.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the start or destination node could not be found in the map.</exception>
        public Path<TContent> FindOptimalPath(TExternalId externalStartId, TExternalId externalDestinationId, CancellationToken cancellationToken = default)
        {
            // Get the start node from the node map.
            MapNode<TContent> startNode = this.NodeMap.GetNode(externalStartId)
                ?? throw new KeyNotFoundException($"Start node with external ID '{externalStartId}' was not found.");

            // Get the destination node from the node map.
            MapNode<TContent> destinationNode = this.NodeMap.GetNode(externalDestinationId)
                ?? throw new KeyNotFoundException($"Destination node with external ID '{externalDestinationId}' was not found.");

            return this.FindOptimalPath(startNode, destinationNode, cancellationToken);
        }

        /// <summary>
        /// Finds the optimal path between the specified start and destination nodes in the map.
        /// </summary>
        /// <param name="startNodeId">The unique identifier of the start node, represented by a <see cref="Guid"/>.</param>
        /// <param name="destinationNodeId">The unique identifier of the destination node, represented by a <see cref="Guid"/>.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. If triggered, the search process will be interrupted and the task will return early.</param>
        /// <returns>A <see cref="Path{TContent}"/> representing the optimal path from the start node to the destination node. Returns <see cref="Path{TContent}.Empty"/> if no path is found.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the start or destination node could not be found in the map.</exception>
        public Path<TContent> FindOptimalPath(Guid startNodeId, Guid destinationNodeId, CancellationToken cancellationToken = default)
        {
            // Get the start node from the node map.
            MapNode<TContent> startNode = this.NodeMap.GetNode(startNodeId)
                ?? throw new KeyNotFoundException($"Start node with ID '{startNodeId}' was not found.");

            // Get the destination node from the node map.
            MapNode<TContent> destinationNode = this.NodeMap.GetNode(destinationNodeId)
                ?? throw new KeyNotFoundException($"Destination node with ID '{destinationNodeId}' was not found.");

            return this.FindOptimalPath(startNode, destinationNode, cancellationToken);
        }

        #endregion

        #region Async

        /// <summary>
        /// Asynchronously finds the optimal path between the specified start and destination nodes in the map, identified by external identifiers.
        /// </summary>
        /// <param name="externalStartId">The external identifier of the start node, of type <typeparamref name="TExternalId"/>.</param>
        /// <param name="externalDestinationId">The external identifier of the destination node, of type <typeparamref name="TExternalId"/>.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. If triggered, the search process will be interrupted and the task will complete early.</param>
        /// <returns>A <see cref="Path{TContent}"/> representing the optimal path from the start node to the destination node. Returns <see cref="Path{TContent}.Empty"/> if no path is found.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the start or destination node could not be found in the map.</exception>
        public Task<Path<TContent>> FindOptimalPathAsync(TExternalId externalStartId, TExternalId externalDestinationId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.FindOptimalPath(externalStartId, externalDestinationId, cancellationToken));
        }

        /// <summary>
        /// Asynchronously finds the optimal path between the specified start and destination nodes in the map.
        /// </summary>
        /// <param name="startNodeId">The unique identifier of the start node, represented by a <see cref="Guid"/>.</param>
        /// <param name="destinationNodeId">The unique identifier of the destination node, represented by a <see cref="Guid"/>.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. If triggered, the search process will be interrupted and the task will complete early.</param>
        /// <returns>A <see cref="Path{TContent}"/> representing the optimal path from the start node to the destination node. Returns <see cref="Path{TContent}.Empty"/> if no path is found.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the start or destination node could not be found in the map.</exception>
        public Task<Path<TContent>> FindOptimalPathAsync(Guid startNodeId, Guid destinationNodeId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.FindOptimalPath(startNodeId, destinationNodeId, cancellationToken));
        }

        #endregion

        #endregion
    }
}
