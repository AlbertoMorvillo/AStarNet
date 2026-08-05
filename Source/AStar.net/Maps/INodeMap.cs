// Copyright (c) 2026 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System.Collections.Generic;

namespace AStarNet.Maps;

/// <summary>
/// Represents a navigable map containing nodes used by the pathfinding algorithm.
/// </summary>
/// <typeparam name="TContent">The type of the optional node content.</typeparam>
public interface INodeMap<TContent>
{
    /// <summary>
    /// Retrieves the node associated with the specified identifier.
    /// </summary>
    /// <param name="id">The identifier of the node to retrieve.</param>
    /// <returns>The matching node, or <see langword="null"/> when no node exists.</returns>
    PathNode<TContent>? GetNode(int id);

    /// <summary>
    /// Retrieves the outgoing connections of a specified node.
    /// </summary>
    /// <param name="node">The node whose connections are requested.</param>
    /// <returns>The outgoing connections of <paramref name="node"/>.</returns>
    IEnumerable<PathConnection<TContent>> GetConnections(PathNode<TContent> node);
}
