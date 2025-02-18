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
    /// Implements function to find a path with the A* algorithm.
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
        protected Path<TContent> FindBestPath(MapNode<TContent> startNode, MapNode<TContent> destinationNode, CancellationToken cancellationToken)
        {
            Path<TContent> resultPath;                                          // The path found if the destination is reached
            HashSet<Guid> visitedNodes = [];                                    // Visited nodes, they must be avoided when looking for new nodes to reach the destination
            PriorityQueue<MapNode<TContent>, double> visitableNodes = new();    // Visitable nodes - Using the priority queue will ensure that the node with the lesser cost will be the first
            MapNode<TContent> actualNode = startNode;                           // The node from looking for new nodes to reach the destination - Begin from the start node
            bool pathFound = false;                                             // A flag to tell if the result path must be filled
            bool keepSarching = true;                                           // A flag to tell if the searching loop must continue

            // Begin the search
            do
            {
                // If operation canceled, stop the search
                if (cancellationToken.IsCancellationRequested)
                {
                    return Path<TContent>.Empty;
                }

                // If destination reached, stop the search
                if (actualNode.Equals(destinationNode))
                {
                    pathFound = true;
                    keepSarching = false;
                }
                else
                {
                    // Get the child nodes 
                    MapNode<TContent>[] childNodes = this.NodeMap.GetChildNodes(actualNode);

                    // Adding actual node to the visited nodes
                    visitedNodes.Add(actualNode.Id);

                    // Analizing child nodes
                    if (childNodes is not null && childNodes.Length > 0)
                    {
                        foreach (MapNode<TContent> childNode in childNodes)
                        {
                            // Add child node the the visitable nodes
                            if (!visitedNodes.Contains(childNode.Id))
                                visitableNodes.Enqueue(childNode, childNode.Cost);
                        }
                    }

                    // Getting the nearest node
                    if (visitableNodes.TryPeek(out MapNode<TContent> nearestNode, out _))
                    {
                        actualNode = nearestNode;
                    }
                    else
                    {
                        // No open nodes (dead end), going backward
                        actualNode = actualNode.Parent;

                        // No parent avalaible (all possible paths tried), stopping the search
                        if (actualNode is null)
                        {
                            keepSarching = false;
                        }
                    }

                    // Clearing the visitable nodes for the next loop
                    visitableNodes.Clear();
                }
            }
            while (keepSarching);

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

                // To nodes are stored in reverse order, so they must be reversed
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
        /// <exception cref="ArgumentNullException">Thrown if the start or destination node is not present in the map.</exception>
        public Path<TContent> FindBestPath(TExternalId externalStartId, TExternalId externalDestinationId, CancellationToken cancellationToken = default)
        {
            // Get the start node from the node map
            MapNode<TContent> startNode = this.NodeMap.GetNode(externalStartId) ?? throw new NullReferenceException("No start node.");

            // Get the destination node from the node map
            MapNode<TContent> destinationNode = this.NodeMap.GetNode(externalDestinationId) ?? throw new NullReferenceException("No destination node.");

            return this.FindBestPath(startNode, destinationNode, cancellationToken);
        }

        /// <summary>
        /// Finds the optimal path between the specified start and destination nodes in the map.
        /// </summary>
        /// <param name="startNodeId">The unique identifier of the start node, represented by a <see cref="Guid"/>.</param>
        /// <param name="destinationNodeId">The unique identifier of the destination node, represented by a <see cref="Guid"/>.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. If triggered, the search process will be interrupted and the task will return early.</param>
        /// <returns>A <see cref="Path{TContent}"/> representing the optimal path from the start node to the destination node. Returns <see cref="Path{TContent}.Empty"/> if no path is found.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the start or destination node is not present in the map.</exception>
        public Path<TContent> FindBestPath(Guid startNodeId, Guid destinationNodeId, CancellationToken cancellationToken = default)
        {
            // Get the start node from the node map
            MapNode<TContent> startNode = this.NodeMap.GetNode(startNodeId) ?? throw new NullReferenceException("No start node.");

            // Get the destination node from the node map
            MapNode<TContent> destinationNode = this.NodeMap.GetNode(destinationNodeId) ?? throw new NullReferenceException("No destination node.");

            return this.FindBestPath(startNode, destinationNode, cancellationToken);
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
        /// <exception cref="ArgumentNullException">Thrown if the start or destination node is not present in the map.</exception>
        public Task<Path<TContent>> FindBestPathAsync(TExternalId externalStartId, TExternalId externalDestinationId, CancellationToken cancellationToken = default)
        {
            Task<Path<TContent>> findTask = Task.Run(() =>
            {
                Path<TContent> resultPath = this.FindBestPath(externalStartId, externalDestinationId, cancellationToken);
                return resultPath;
            });

            return findTask;
        }

        /// <summary>
        /// Asynchronously finds the optimal path between the specified start and destination nodes in the map.
        /// </summary>
        /// <param name="startNodeId">The unique identifier of the start node, represented by a <see cref="Guid"/>.</param>
        /// <param name="destinationNodeId">The unique identifier of the destination node, represented by a <see cref="Guid"/>.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. If triggered, the search process will be interrupted and the task will complete early.</param>
        /// <returns>A <see cref="Path{TContent}"/> representing the optimal path from the start node to the destination node. Returns <see cref="Path{TContent}.Empty"/> if no path is found.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the start or destination node is not present in the map.</exception>
        public Task<Path<TContent>> FindBestPathAsync(Guid startNodeId, Guid destinationNodeId, CancellationToken cancellationToken = default)
        {
            Task<Path<TContent>> findTask = Task.Run(() =>
            {
                Path<TContent> resultPath = this.FindBestPath(startNodeId, destinationNodeId, cancellationToken);
                return resultPath;
            });

            return findTask;
        }

        #endregion

        #endregion
    }
}
