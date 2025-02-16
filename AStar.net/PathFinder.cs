// Copyright (c) 2025 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using AStarNet.Collections;
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
    /// <typeparam name="TContent">The type of the node content.</typeparam>
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
        /// Finds the path.
        /// </summary>
        /// <param name="destinationNode">Destination <see cref="MapNode{T}"/>.</param>
        /// <param name="actualNode">Actual visited <see cref="MapNode{T}"/>.</param>
        /// <param name="nodeCollection">Collection containing the nodes that can be visited or must be ignored.</param>
        /// <param name="cancellationToken">A token that can be used to request the cancellation of the operation.</param>
        /// <returns>A <see cref="Path{T}"/> containing all the nodes defines the path, from start to destination.</returns>
        protected Path<TContent> FindPath(MapNode<TContent> destinationNode, ref MapNode<TContent> actualNode, OpenClosedNodeCollection<TContent> nodeCollection, CancellationToken cancellationToken)
        {
            Path<TContent> resultPath;
            bool pathFound = false;
            bool keepSarching = true;

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

                    // Adding actual node to the closed list
                    nodeCollection.AddClosed(actualNode);

                    // Analizing child nodes
                    if (childNodes.Length > 0)
                    {
                        foreach(MapNode<TContent> childNode in childNodes)
                        {
                            // Add child node the the open list
                            nodeCollection.AddOpen(childNode);
                        }

                        // Getting the nearest node
                        MapNode<TContent> nearestNode = nodeCollection.GetNearestNode();

                        if (nearestNode is not null)
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
                    }
                    else
                    {
                        // No child nodes (dead end), going backward
                        actualNode = actualNode.Parent;

                        // No parent avalaible (all possible paths tried), stopping the search
                        if (actualNode is null)
                        {
                            keepSarching = false;
                        }
                    }
                }
            }
            while (keepSarching);

            if (pathFound)
            {
                // Destination reached, saving the search
                List<MapNode<TContent>> mapNodeList = [];

                // Pupulate the list. Starting node must be the first in the list
                while (actualNode is not null)
                {
                    mapNodeList.Add(actualNode);
                    actualNode = actualNode.Parent;
                }

                // Adding and reversing is faster than other solutions
                mapNodeList.Reverse();

                // Populate and return the result path
                resultPath = new Path<TContent>(Guid.NewGuid(), mapNodeList);
            }
            else
            {
                resultPath = Path<TContent>.Empty;
            }

            return resultPath;
        }

        /// <summary>
        /// Finds the optimal path between the specified start and destination nodes in the map.
        /// </summary>
        /// <param name="startNode">The start <see cref="MapNode{TContent}"/>.</param>
        /// <param name="destinationNode">The destination <see cref="MapNode{TContent}"/>.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. If triggered, the search process will be interrupted and the task will return early.</param>
        /// <returns>A <see cref="Path{TContent}"/> representing the optimal path from the start node to the destination node. Returns <see cref="Path{TContent}.Empty"/> if no path is found.</returns>
        protected Path<TContent> FindBestPath(MapNode<TContent> startNode, MapNode<TContent> destinationNode, CancellationToken cancellationToken)
        {
            // Initialize che collections
            OpenClosedNodeCollection<TContent> nodeCollection = new();

            // Begin from the start node
            MapNode<TContent> actualNode = startNode;

            // Perform the search
            Path<TContent> resultPath = this.FindPath(destinationNode, ref actualNode, nodeCollection, cancellationToken);

            return resultPath;
        }

        /// <summary>
        /// Finds all possible paths between the specified start and destination nodes in the map.
        /// </summary>
        /// <param name="startNode">The start <see cref="MapNode{TContent}"/>.</param>
        /// <param name="destinationNode">The destination <see cref="MapNode{TContent}"/>.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. If triggered, the search process will be interrupted and the task will return early.</param>
        /// <returns>An array of <see cref="Path{TContent}"/> instances representing all valid paths from the start node to the destination node. Returns an empty array if no paths are found.</returns>
        protected Path<TContent>[] FindAllPaths(MapNode<TContent> startNode, MapNode<TContent> destinationNode, CancellationToken cancellationToken)
        {
            // Initialize che collections
            OpenClosedNodeCollection<TContent> nodeCollection = new();
            List<Path<TContent>> resultPathList = [];

            // Begin from the start node
            MapNode<TContent> actualNode = startNode;
            bool pathFound;

            // Begin the search
            do
            {
                Path<TContent> foundPath = this.FindPath(destinationNode, ref actualNode, nodeCollection, cancellationToken);

                if (foundPath.Nodes.Count > 0)
                {
                    // Add the found path the path list
                    resultPathList.Add(foundPath);

                    // Reset the search
                    actualNode = startNode;
                    pathFound = true;
                }
                else
                {
                    pathFound = false;
                }
            }
            while (pathFound);

            // Sort all the paths and return them as array
            resultPathList.Sort();

            return [.. resultPathList];
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

        /// <summary>
        /// Finds all possible paths between the specified start and destination nodes in the map, identified by external identifiers.
        /// </summary>
        /// <param name="externalStartId">The external identifier of the start node, of type <typeparamref name="TExternalId"/>.</param>
        /// <param name="externalDestinationId">The external identifier of the destination node, of type <typeparamref name="TExternalId"/>.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. If triggered, the search process will be interrupted and the task will return early.</param>
        /// <returns>An array of <see cref="Path{TContent}"/> instances representing all valid paths from the start node to the destination node. Returns an empty array if no paths are found.</returns>
        /// <exception cref="NullReferenceException">Thrown if the start or destination node is not present in the map.</exception>
        public Path<TContent>[] FindAllPaths(TExternalId externalStartId, TExternalId externalDestinationId, CancellationToken cancellationToken = default)
        {
            // Get the start node from the node map
            MapNode<TContent> startNode = this.NodeMap.GetNode(externalStartId) ?? throw new NullReferenceException("No start node.");

            // Get the destination node from the node map
            MapNode<TContent> destinationNode = this.NodeMap.GetNode(externalDestinationId) ?? throw new NullReferenceException("No destination node.");

            return this.FindAllPaths(startNode, destinationNode, cancellationToken);
        }

        /// <summary>
        /// Finds all possible paths between the specified start and destination nodes in the map.
        /// </summary>
        /// <param name="startNodeId">The unique identifier of the start node, represented by a <see cref="Guid"/>.</param>
        /// <param name="destinationNodeId">The unique identifier of the destination node, represented by a <see cref="Guid"/>.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. If triggered, the search process will be interrupted and the task will return early.</param>
        /// <returns>An array of <see cref="Path{TContent}"/> instances representing all valid paths from the start node to the destination node. Returns an empty array if no paths are found.</returns>
        /// <exception cref="NullReferenceException">Thrown if the start or destination node is not present in the map.</exception>
        public Path<TContent>[] FindAllPaths(Guid startNodeId, Guid destinationNodeId, CancellationToken cancellationToken = default)
        {
            // Get the start node from the node map
            MapNode<TContent> startNode = this.NodeMap.GetNode(startNodeId) ?? throw new NullReferenceException("No start node.");

            // Get the destination node from the node map
            MapNode<TContent> destinationNode = this.NodeMap.GetNode(destinationNodeId) ?? throw new NullReferenceException("No destination node.");

            return this.FindAllPaths(startNode, destinationNode, cancellationToken);
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

        /// <summary>
        /// Asynchronously finds all possible paths between the specified start and destination nodes in the map, identified by external identifiers.
        /// </summary>
        /// <param name="externalStartId">The external identifier of the start node, of type <typeparamref name="TExternalId"/>.</param>
        /// <param name="externalDestinationId">The external identifier of the destination node, of type <typeparamref name="TExternalId"/>.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. If triggered, the search process will be interrupted and the task will complete early.</param>
        /// <returns>An array of <see cref="Path{TContent}"/> instances representing all valid paths from the start node to the destination node. Returns an empty array if no paths are found.</returns>
        /// <exception cref="NullReferenceException">Thrown if the start or destination node is not present in the map.</exception>
        public Task<Path<TContent>[]> FindAllPathsAsync(TExternalId externalStartId, TExternalId externalDestinationId, CancellationToken cancellationToken = default)
        {
            Task<Path<TContent>[]> findTask = Task.Run(() =>
            {
                Path<TContent>[] resultPaths = this.FindAllPaths(externalStartId, externalDestinationId, cancellationToken);
                return resultPaths;
            });

            return findTask;
        }

        /// <summary>
        /// Asynchronously finds all possible paths between the specified start and destination nodes in the map.
        /// </summary>
        /// <param name="startNodeId">The unique identifier of the start node, represented by a <see cref="Guid"/>.</param>
        /// <param name="destinationNodeId">The unique identifier of the destination node, represented by a <see cref="Guid"/>.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. If triggered, the search process will be interrupted and the task will complete early.</param>
        /// <returns>An array of <see cref="Path{TContent}"/> instances representing all valid paths from the start node to the destination node. Returns an empty array if no paths are found.</returns>
        /// <exception cref="NullReferenceException">Thrown if the start or destination node is not present in the map.</exception>
        public Task<Path<TContent>[]> FindAllPathsAsync(Guid startNodeId, Guid destinationNodeId, CancellationToken cancellationToken = default)
        {
            Task<Path<TContent>[]> findTask = Task.Run(() =>
            {
                Path<TContent>[] resultPaths = this.FindAllPaths(startNodeId, destinationNodeId, cancellationToken);
                return resultPaths;
            });

            return findTask;
        }

        #endregion

        #endregion
    }
}
