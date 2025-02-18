// Copyright (c) 2025 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;

namespace AStarNet
{
    /// <summary>
    /// Represents a map with nodes navigable using the A algorithm.
    /// </summary>
    /// <typeparam name="TExternalId">The type of the external identifier representing a node in the specific domain (e.g., coordinates, room names, graph keys).</typeparam>
    /// <typeparam name="TContent">The type of the node content.</typeparam>
    public interface INodeMap<TExternalId, TContent>
    {
        /// <summary>
        /// Gets the <see cref="MapNode{TContent}"/> in the map that has the specified unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the node, represented by a <see cref="Guid"/>.</param>
        /// <returns>The <see cref="MapNode{TContent}"/> with the specified unique identifier, or <see langword="null"/> if no matching node is found.</returns>
        MapNode<TContent> GetNode(Guid id);

        /// <summary>
        /// Gets the <see cref="MapNode{TContent}"/> in the map associated with the specified external identifier.
        /// </summary>
        /// <param name="externalId">The external identifier representing the node, of type <typeparamref name="TExternalId"/>.</param>
        /// <returns>The <see cref="MapNode{TContent}"/> associated with the specified external identifier, or <see langword="null"/> if no matching node is found.</returns>
        MapNode<TContent> GetNode(TExternalId externalId);

        /// <summary>
        /// Get the childs of a specific <see cref="MapNode{TContent}"/> in the map.
        /// </summary>
        /// <param name="currentNode">The <see cref="MapNode{TContent}"/> from which get the childs.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the child nodes of the specified <see cref="MapNode{TContent}"/>.</returns>
        IEnumerable<MapNode<TContent>> GetChildNodes(MapNode<TContent> currentNode);
    }
}
