
// Copyright (c) 2021 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Threading;

namespace AStarNet
{
    /// <summary>
    /// Delegate for path found event on async finding.
    /// </summary>
    /// <typeparam name="T">Type of the node content.</typeparam>
    /// <param name="paths">Result path.</param>
    /// <param name="operationID">Identifier of the operation which is searching for a path.</param>
    public delegate void PathsFoundEventHandler<T>(Path<T>[] paths, Guid operationID);

    /// <summary>
    /// Implements function to find a path with the A* algorithm.
    /// </summary>
    /// <typeparam name="T">Type of the node content.</typeparam>
    public class PathFinder<T>
    {
        #region Private data

        /// <summary>
        /// Search state enumeration used to manage the search function.
        /// </summary>
        private enum SearchState
        {
            Searching = 0,
            Found = 1,
            NotFound = 2
        }

        /// <summary>
        /// Structure used to store the find thread parameters.
        /// </summary>
        /// <typeparam name="TContent">Type of the node content.</typeparam>
        private struct FindPathAsynchParams<TContent>
        {
            public Guid OperationID;
            public PathsFoundEventHandler<TContent> PathsFoundEventDelegate;
        }

        /// <summary>
        /// Contains the lists of nodes that could be visited or must be ignored.
        /// </summary>
        /// <typeparam name="TContent">Type of the node content.</typeparam>
        private class OpenClosedNodeCollection<TContent>
        {
            #region Fields

            private readonly Dictionary<Guid, Node<TContent>> openNodeDictionary;     // List of nodes that could be visited
            private readonly Dictionary<Guid, Node<TContent>> closedNodeDictionary;   // List of nodes that must not be considered

            #endregion

            #region Costrunctor

            /// <summary>
            /// Creates a new instance of the <see cref="OpenClosedNodeCollection{T}"/>.
            /// </summary>
            public OpenClosedNodeCollection()
            {
                this.openNodeDictionary = new Dictionary<Guid, Node<TContent>>();
                this.closedNodeDictionary = new Dictionary<Guid, Node<TContent>>();
            }

            #endregion

            #region Public methods

            /// <summary>
            /// Adds a node to the open list (nodes that could be visited).
            /// </summary>
            /// <param name="node">Node to add to the open list.</param>
            public void AddOpen(Node<TContent> node)
            {
                // Skip closed nodes
                if (!this.closedNodeDictionary.ContainsKey(node.ID))
                {
                    // Verifyng in the current path its better than previous
                    if (this.openNodeDictionary.TryGetValue(node.ID, out Node<TContent> openNode))
                    {
                        if (openNode.PathCost >= node.PathCost)
                        {
                            // Current it's better or equal, substitute the node
                            this.openNodeDictionary[node.ID] = node;
                        }
                    }
                    else
                    {
                        // No preovious path, adding the node
                        this.openNodeDictionary.Add(node.ID, node);
                    }
                }
            }

            /// <summary>
            /// Adds a node to the closed list (nodes that must not be considered).
            /// </summary>
            /// <param name="node">Node to add to the close list.</param>
            public void AddClosed(Node<TContent> node)
            {
                // Add the node to the close dictionary and remove it, if exist,
                // from the open dictionary
                if (!this.closedNodeDictionary.ContainsKey(node.ID))
                {
                    this.closedNodeDictionary.Add(node.ID, node);
                    this.openNodeDictionary.Remove(node.ID);
                }
            }

            /// <summary>
            /// Gets the nearest node to the destination.
            /// </summary>
            public Node<TContent> GetNearestNode()
            {
                Node<TContent>[] nodeArray = new Node<TContent>[this.openNodeDictionary.Count];
                Node<TContent> nearestNodeInArray = null;

                // Continue only if there are nodes in the dictionary
                if (nodeArray.Length > 0)
                {
                    this.openNodeDictionary.Values.CopyTo(nodeArray, 0);

                    // Start the search from the first node
                    nearestNodeInArray = nodeArray[0];

                    for (int i = 0; i < nodeArray.Length; i++)
                    {
                        // If there is a node lesser than the current nearest node, this must be the new nearest
                        if (nearestNodeInArray.CompareTo(nodeArray[i]) > 0)
                        {
                            nearestNodeInArray = nodeArray[i];
                        }
                    }
                }

                return nearestNodeInArray;
            }

            #endregion
        }

        /// <summary>
        /// Dictionary used to manage the open threads for multi-threading.
        /// </summary>
        private Dictionary<Guid, Thread> _threadDictionary;

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the asynch path finding function return.
        /// </summary>
        public event PathsFoundEventHandler<T> PathsFound;

        #endregion

        #region Fields

        /// <summary>
        /// Node map where to find the path.
        /// </summary>
        protected readonly INodeMap<T> _nodeMap;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the <see cref="INodeMap{T}"/> where to find the path.
        /// </summary>
        public INodeMap<T> NodeMap
        {
            get
            {
                return this._nodeMap;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new istance of <see cref="PathFinder{T}"/>.
        /// </summary>
        /// <param name="nodeMap">Node map where to find a path.</param>
        public PathFinder(INodeMap<T> nodeMap)
        {
            this._threadDictionary = new Dictionary<Guid, Thread>();
            this._nodeMap = nodeMap;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Function working as a new thread used to launch dhe FindBestPath method asynchronously.
        /// </summary>
        /// <param name="param"><see cref="FindPathAsynchParams{T}"/> casted to <see cref="object"/>.</param>
        private void DoFindBestPathAsynch(object param)
        {
            FindPathAsynchParams<T> findParams = (FindPathAsynchParams<T>)param;
            Path<T> path = this.FindBestPath();

            findParams.PathsFoundEventDelegate?.Invoke(new Path<T>[] { path }, findParams.OperationID);

            this._threadDictionary.Remove(findParams.OperationID);
        }

        /// <summary>
        /// Function working as a new thread used to launch dhe FindAllPaths method asynchronously.
        /// </summary>
        /// <param name="param"><see cref="FindPathAsynchParams{T}"/> casted to <see cref="object"/>.</param>
        private void DoFindAllPathsAsynch(object param)
        {
            FindPathAsynchParams<T> findParams = (FindPathAsynchParams<T>)param;
            Path<T>[] paths = this.FindAllPaths();

            findParams.PathsFoundEventDelegate?.Invoke(paths, findParams.OperationID);

            this._threadDictionary.Remove(findParams.OperationID);
        }

        /// <summary>
        /// Compute a pass to find the path. It must be cycled untill the result is <see cref="SearchState.Found"/>.
        /// </summary>
        /// <param name="destinationNode">Destination <see cref="Node{T}"/>.</param>
        /// <param name="actualNode">Actual visited <see cref="Node{T}"/>.</param>
        /// <param name="nodeCollection">Collection containing the nodes that can be visited or must be ignored.</param>
        /// <returns>The result of the path finding.</returns>
        private SearchState ComputeFindPath(Node<T> destinationNode, ref Node<T> actualNode, OpenClosedNodeCollection<T> nodeCollection)
        {
            SearchState searchResult = SearchState.Searching;       // Flag used to let the function cycle
            Node<T>[] childNodes = null;                            // Array of child nodes
            Node<T> nearestNode = null;                             // Node nearest to the actual node

            // Destination reached, stopping the search
            if (actualNode.Equals(destinationNode))
            {
                searchResult = SearchState.Found;
                return searchResult;
            }

            // Get the child nodes 
            childNodes = this.NodeMap.GetChildNodes(actualNode);

            // Adding actual node to the closed list
            nodeCollection.AddClosed(actualNode);

            // Analizing child nodes
            if (childNodes.Length > 0)
            {
                for (int i = 0; i < childNodes.Length; i++)
                {
                    // Add child node the the open list
                    nodeCollection.AddOpen(childNodes[i]);
                }

                // Getting the nearest node
                nearestNode = nodeCollection.GetNearestNode();

                if (nearestNode != null)
                {
                    actualNode = nearestNode;
                }
                else
                {
                    // No open nodes, going backward
                    actualNode = actualNode.Parent;

                    // No path avalaible, stopping the search
                    if (actualNode == null)
                    {
                        searchResult = SearchState.NotFound;
                    }
                }
            }
            else
            {
                // No child nodes, going backward
                actualNode = actualNode.Parent;

                // No path avalaible, stopping the search
                if (actualNode == null)
                {
                    searchResult = SearchState.NotFound;
                }
            }

            return searchResult;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Find the best path between map start point and destination point.
        /// </summary>
        /// <returns>A <see cref="Path{T}"/> containing all the nodes defines the path, from start to destination.</returns>
        /// <exception cref="ArgumentNullException">No start or destination node are given in the map.</exception>
        public Path<T> FindBestPath()
        {
            Node<T> startNode = null;                               // Start node from which begin the navigation
            Node<T> destinationNode = null;                         // Destination node to be reached
            Node<T> actualNode = null;                              // Actual visited node
            Path<T> resultPath = null;                              // Result path

            SearchState searchResult = SearchState.Searching;       // Flag used to let the function cycle
            OpenClosedNodeCollection<T> nodeCollection = null;      // Collection containing the nodes that can be visited or must be ignored
            List<Node<T>> pathNodeList = null;                      // List of node that defines the path
            double resultPathCost = 0;                              // Cost of the result path


            // Get the start node from the node map
            startNode = this.NodeMap.GetStartNode();

            // Get the destination node from the node map
            destinationNode = this.NodeMap.GetDestinationNode();

            // Throw an exception if there are no starting or destination node
            if (startNode == null)
                throw new ArgumentNullException(nameof(startNode));

            if (destinationNode == null)
                throw new ArgumentNullException(nameof(destinationNode));

            // Initialize che collections
            pathNodeList = new List<Node<T>>();
            nodeCollection = new OpenClosedNodeCollection<T>();

            // Begin from the start node
            actualNode = startNode;

            // Begin the search
            do
            {
                searchResult = this.ComputeFindPath(destinationNode, ref actualNode, nodeCollection);
            }
            while (searchResult == SearchState.Searching);

            // Create the path if destination as been reached
            if (searchResult == SearchState.Found)
            {
                // Destination reached, saving the search
                resultPathCost = actualNode.PathCost;

                // Pupulate the list. Starting node must be the first in the list
                while (actualNode != null)
                {
                    pathNodeList.Insert(0, actualNode);
                    actualNode = actualNode.Parent;
                }
            }

            // Populate and return the result path
            resultPath = new Path<T>(pathNodeList, resultPathCost);

            return resultPath;
        }

        /// <summary>
        /// Find all the paths avalaible between map start point and destination point.
        /// </summary>
        /// <returns>A <see cref="Path{T}"/> array containing all the paths avalaible from start to destination.</returns>
        /// <exception cref="ArgumentNullException">No start or destination node are given in the map.</exception>
        public Path<T>[] FindAllPaths()
        {
            Node<T> startNode = null;                               // Start node from which begin the navigation
            Node<T> destinationNode = null;                         // Destination node to be reached
            Node<T> actualNode = null;                              // Actual visited node
            Path<T> foundPath = null;                               // Path found by a search
            List<Path<T>> resultPathList = new List<Path<T>>();     // List of paths found by all the search cycles

            SearchState searchResult = SearchState.Searching;       // Flag used to let the function cycle
            OpenClosedNodeCollection<T> nodeCollection = null;      // Collection containing the nodes that can be visited or must be ignored
            List<Node<T>> pathNodeList = null;                      // List of node that defines the path
            double foundPathCost = 0;                               // Cost of the found path


            // Get the start node from the node map
            startNode = this.NodeMap.GetStartNode();

            // Get the destination node from the node map
            destinationNode = this.NodeMap.GetDestinationNode();

            // Throw an exception if there are no starting or destination node
            if (startNode == null)
                throw new ArgumentNullException(nameof(startNode));

            if (destinationNode == null)
                throw new ArgumentNullException(nameof(destinationNode));

            // Initialize che collections
            pathNodeList = new List<Node<T>>();
            nodeCollection = new OpenClosedNodeCollection<T>();

            // Begin from the start node
            actualNode = startNode;

            // Begin the search
            do
            {
                searchResult = this.ComputeFindPath(destinationNode, ref actualNode, nodeCollection);

                // Create the path if destination as been reached
                if (searchResult == SearchState.Found)
                {
                    // Destination reached, saving the search
                    foundPathCost = actualNode.PathCost;

                    // Pupulate the list. Starting node must be the first in the list
                    while (actualNode != null)
                    {
                        pathNodeList.Insert(0, actualNode);
                        actualNode = actualNode.Parent;
                    }

                    // Populate the found path
                    foundPath = new Path<T>(pathNodeList, foundPathCost);

                    // Add the found path the path list
                    resultPathList.Add(foundPath);

                    // Reset the search
                    actualNode = startNode;
                    searchResult = SearchState.Searching;
                }
            }
            while (searchResult == SearchState.Searching);

            // Sort all the paths and return them as array
            resultPathList.Sort();

            return resultPathList.ToArray();
        }

        #region Async methods

        /// <summary>
        /// Find the best path between map start point and destination point asynchronously.
        /// </summary>
        /// <returns>Async operation <see cref="Guid"/>.</returns>
        public Guid FindBestPathAsynch()
        {
            Guid operationID = Guid.Empty;
            Thread findThread = new Thread(new ParameterizedThreadStart(this.DoFindBestPathAsynch));
            FindPathAsynchParams<T> findParams = new FindPathAsynchParams<T>();
            operationID = Guid.NewGuid();

            this._threadDictionary.Add(operationID, findThread);

            findParams.OperationID = operationID;
            findParams.PathsFoundEventDelegate = this.PathsFound;

            findThread.Start(findParams);

            return operationID;
        }

        /// <summary>
        /// Find all the paths avalaible between map start point and destination point asynchronously.
        /// </summary>
        /// <returns>Async operation <see cref="Guid"/>.</returns>
        public Guid FindAllPathsAsynch()
        {
            Guid operationID = Guid.Empty;
            Thread findThread = new Thread(new ParameterizedThreadStart(this.DoFindAllPathsAsynch));
            FindPathAsynchParams<T> findParams = new FindPathAsynchParams<T>();
            operationID = Guid.NewGuid();

            this._threadDictionary.Add(operationID, findThread);

            findParams.OperationID = operationID;
            findParams.PathsFoundEventDelegate = this.PathsFound;

            findThread.Start(findParams);

            return operationID;
        }

        /// <summary>
        /// Gets the asynch operation status.
        /// </summary>
        /// <param name="operationID">Asynch operation ID for which get the status.</param>
        /// <returns></returns>
        public ThreadState GetAsynchOperationStatus(Guid operationID)
        {
            Thread thread = null;
            ThreadState state = ThreadState.Unstarted;

            if (this._threadDictionary.ContainsKey(operationID))
            {
                thread = this._threadDictionary[operationID];
                state = thread.ThreadState;
            }

            return state;
        }

        /// <summary>
        /// Stop a specific asynch operation.
        /// </summary>
        /// <param name="operationID">Asynch find istance <see cref="Guid"/>.</param>
        public void StopAsynchOperation(Guid operationID)
        {
            if (this._threadDictionary.ContainsKey(operationID))
            {
                this._threadDictionary[operationID].Abort();
                this._threadDictionary.Remove(operationID);
            }
        }

        /// <summary>
        /// Stop all asynch operations.
        /// </summary>
        public void StopAllAsynchOperations()
        {
            foreach (Thread thread in this._threadDictionary.Values)
            {
                thread.Abort();
            }

            this._threadDictionary.Clear();
        }

        #endregion

        #endregion
    }
}
