// Copyright (c) 2025 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;

namespace AStarNet.Maps
{
    /// <summary>
    /// Represents a navigable map containing nodes that can be used with the A* algorithm.
    /// </summary>
    /// <typeparam name="TId">The type of the identifier for the nodes in the map.</typeparam>
    /// <typeparam name="TNode">The type of nodes in the map, implementing <see cref="IPathNode{TId}"/>.</typeparam>
    public interface INodeMap<TId, TNode>
        where TId : notnull, IEquatable<TId>
        where TNode : IPathNode<TId>
    {
        /// <summary>
        /// Retrieves the node associated with the specified identifier.
        /// </summary>
        /// <param name="id">The identifier of the node to retrieve.</param>
        /// <returns>
        /// The <typeparamref name="TNode"/> instance with the given identifier, or <see langword="null"/> if no matching node is found.
        /// </returns>
        TNode? GetNode(TId id);

        /// <summary>
        /// Determines whether a given node has any child nodes.
        /// </summary>
        /// <param name="node">The node to check for child nodes.</param>
        /// <returns>
        /// <see langword="true"/> if the node has at least one child node; otherwise, <see langword="false"/>.
        /// </returns>
        bool HasChildNodes(TNode node);

        /// <summary>
        /// Retrieves the child nodes of a specified node in the map.
        /// </summary>
        /// <param name="node">The node for which to retrieve child nodes.</param>
        /// <returns>
        /// An <see cref="IEnumerable{TNode}"/> containing the child nodes of the specified node.
        /// </returns>
        IEnumerable<TNode> GetChildNodes(TNode node);
    }
}
