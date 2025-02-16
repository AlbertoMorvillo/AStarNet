// Copyright (c) 2025 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;

namespace AStarNet.Collections
{
    /// <summary>
    /// Contains the lists of nodes that could be visited or must be ignored.
    /// </summary>
    /// <typeparam name="TContent">The type of the node content.</typeparam>
    public class OpenClosedNodeCollection<TContent>
    {
        #region Fields

        /// <summary>
        /// Dictionary of nodes that could be visitated.
        /// </summary>
        protected readonly Dictionary<Guid, MapNode<TContent>> _openNodeDictionary;

        /// <summary>
        /// Dictionary of nodes that must be ignored.
        /// </summary>
        protected readonly Dictionary<Guid, MapNode<TContent>> _closedNodeDictionary;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the number of open nodes.
        /// </summary>
        public int OpenCount => this._openNodeDictionary.Count;

        /// <summary>
        /// Gets the number of open nodes.
        /// </summary>
        public int ClosedCount => this._closedNodeDictionary.Count;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenClosedNodeCollection{T}"/> class.
        /// </summary>
        public OpenClosedNodeCollection()
        {
            this._openNodeDictionary = [];
            this._closedNodeDictionary = [];
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Adds a node to the open list (nodes that could be visited).
        /// </summary>
        /// <param name="node">Node to add to the open list.</param>
        public void AddOpen(MapNode<TContent> node)
        {
            // Skip closed nodes
            if (this._closedNodeDictionary.ContainsKey(node.Id))
                return;

            // Verifyng if the node in the current path its better than the same node
            // in a previous path
            if (this._openNodeDictionary.TryGetValue(node.Id, out MapNode<TContent> openNode))
            {
                if (openNode.CostFromStart >= node.CostFromStart)
                {
                    // Current it's better or equal, replacing the node
                    this._openNodeDictionary[node.Id] = node;
                }
            }
            else
            {
                // No preovious path, adding the node
                this._openNodeDictionary.Add(node.Id, node);
            }
        }

        /// <summary>
        /// Adds a node to the closed list (nodes that must not be considered).
        /// </summary>
        /// <param name="node">Node to add to the close list.</param>
        public void AddClosed(MapNode<TContent> node)
        {
            // Add the node to the close dictionary and, if it isn't already
            // closed, try to remove it from the open dictionary
            if (this._closedNodeDictionary.TryAdd(node.Id, node))
            {
                this._openNodeDictionary.Remove(node.Id);
            }
        }

        /// <summary>
        /// Determines whether the open set contains a node with the specified unique identifier.
        /// </summary>
        /// <param name="nodeId">The unique identifier of the node, represented by a <see cref="Guid"/>.</param>
        /// <returns><see langword="true"/> if the open set contains a node with the specified identifier; otherwise, <see langword="false"/>.</returns>
        public bool ContainsOpen(Guid nodeId)
        {
            bool result = this._openNodeDictionary.ContainsKey(nodeId);
            return result;
        }

        /// <summary>
        /// Determines whether the closed set contains a node with the specified unique identifier.
        /// </summary>
        /// <param name="nodeId">The unique identifier of the node, represented by a <see cref="Guid"/>.</param>
        /// <returns><see langword="true"/> if the closed set contains a node with the specified identifier; otherwise, <see langword="false"/>.</returns>
        public bool ContainsClosed(Guid nodeId)
        {
            bool result = this._closedNodeDictionary.ContainsKey(nodeId);
            return result;
        }

        /// <summary>
        /// Gets the nearest node to the destination.
        /// </summary>
        public MapNode<TContent> GetNearestNode()
        {
            // Continue only if there are open nodes
            if (this._openNodeDictionary.Count == 0)
                return null;

            MapNode<TContent> nearestNode = null;

            foreach (MapNode<TContent> node in this._openNodeDictionary.Values)
            {
                // If there is a node lesser than the current nearest node, this must be the new nearest
                if (nearestNode == null || node.CompareTo(nearestNode) < 0)
                {
                    nearestNode = node;
                }
            }

            return nearestNode;
        }

        /// <summary>
        /// Clear the open and closed node lists.
        /// </summary>
        public void Clear()
        {
            this._openNodeDictionary.Clear();
            this._closedNodeDictionary.Clear();
        }

        #endregion
    }
}
