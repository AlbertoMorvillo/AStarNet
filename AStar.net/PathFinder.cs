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
            Path<TContent> resultPath;                                          // The path found if the destination is reached.
            HashSet<Guid> closedNodes = [];                                     // Closed nodes, they must be avoided when looking for new nodes to reach the destination.
            PriorityQueue<MapNode<TContent>, double> openNodes = new();         // Open nodes - Priority queue ensures the node with the lowest cost is dequeued first.
            MapNode<TContent>? actualNode = startNode;                          // The node from looking for new nodes to reach the destination - Starts from the start node.
            bool pathFound = false;                                             // A flag to tell if the result path must be filled.
            bool keepDequeuingOpenNode;                                         // A flag indicating whether the dequeuing loop for open nodes should continue.
            bool keepSearchingForPath = true;                                   // A flag indicating whether the path searching loop for open nodes should continue.

            // Begin the search
            do
            {
                // If operation canceled, stop the search
                if (cancellationToken.IsCancellationRequested)
                {
                    return Path<TContent>.Empty;
                }

                // If destination reached, stop the search
                if (actualNode!.Equals(destinationNode))
                {
                    pathFound = true;
                    keepSearchingForPath = false;
                }
                else
                {
                    // Get the child nodes 
                    IEnumerable<MapNode<TContent>> childNodes = this.NodeMap.GetChildNodes(actualNode);

                    // Adding actual node to the closed nodes
                    closedNodes.Add(actualNode.Id);

                    // Analyzing child nodes
                    if (childNodes is not null)
                    {
                        foreach (MapNode<TContent> childNode in childNodes)
                        {
                            // Add child node to the open nodes
                            if (!closedNodes.Contains(childNode.Id))
                                openNodes.Enqueue(childNode, childNode.Cost);
                        }
                    }

                    // Getting the nearest open node
                    // The node must be dequeued from the queue to check if it must be ignored
                    keepDequeuingOpenNode = true;

                    do
                    {
                        // Try to dequeue an open node
                        if (openNodes.TryDequeue(out MapNode<TContent>? nearestNode, out _))
                        {
                            if (!closedNodes.Contains(nearestNode.Id))
                            {
                                // Nearest open node found
                                keepDequeuingOpenNode = false;
                                actualNode = nearestNode;
                            }
                        }
                        else
                        {
                            // Nearest open node not found
                            keepDequeuingOpenNode = false;
                            actualNode = actualNode!.Parent;

                            // No parent available (all possible paths tried), stopping the search
                            if (actualNode is null)
                            {
                                keepSearchingForPath = false;
                            }
                        }
                    }
                    while (keepDequeuingOpenNode);
                }
            }
            while (keepSearchingForPath);

            // If a path is found, the nodes must be navigated backward to fill the result path
            if (pathFound)
            {
                // A list will be used to store the nodes
                List<MapNode<TContent>> mapNodeList = [];

                while (actualNode is not null)
                {
                    mapNodeList.Add(actualNode);
                    actualNode = actualNode.Parent;
                }

                // The nodes are stored in reverse order, so they must be reversed
                // Using a list and reverse it is a fast and clean solution
                mapNodeList.Reverse();

                // Populate and return the result path
                resultPath = new Path<TContent>(Guid.NewGuid(), mapNodeList);
            }
            else
            {
                // No path found, so the result path will be an empty one
                resultPath = Path<TContent>.Empty;
            }

            return resultPath;
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
            // Get the start node from the node map
            MapNode<TContent> startNode = this.NodeMap.GetNode(externalStartId)
                ?? throw new KeyNotFoundException($"Start node with external ID '{externalStartId}' was not found.");

            // Get the destination node from the node map
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
            // Get the start node from the node map
            MapNode<TContent> startNode = this.NodeMap.GetNode(startNodeId)
                ?? throw new KeyNotFoundException($"Start node with ID '{startNodeId}' was not found.");

            // Get the destination node from the node map
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
