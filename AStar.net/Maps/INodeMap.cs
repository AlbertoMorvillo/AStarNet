// Copyright (c) 2025 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System.Collections.Generic;

namespace AStarNet.Maps
{
    /// <summary>
    /// Represents a map with nodes navigable using the A* algorithm.
    /// </summary>
    /// <typeparam name="TId">The type of the node identifier.</typeparam>
    public interface INodeMap<TId> where TId : notnull
    {
        /// <summary>
        /// Gets the <see cref="IPathNode{TId}"/> in the map that has the specified identifier.
        /// </summary>
        /// <param name="id">The identifier of the node to retrieve.</param>
        /// <returns>The <see cref="IPathNode{TId}"/> with the specified identifier, or <see langword="null"/> if no node is found with the given identifier.</returns>
        IPathNode<TId>? GetNode(TId id);

        /// <summary>
        /// Gets the child nodes of a specific <see cref="IPathNode{TId}"/> in the map.
        /// </summary>
        /// <param name="node">The <see cref="IPathNode{TId}"/> for which to retrieve the child nodes.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the child nodes of the specified <see cref="IPathNode{TId}"/>.</returns>
        IEnumerable<IPathNode<TId>> GetChildNodes(IPathNode<TId> node);

    }
}
