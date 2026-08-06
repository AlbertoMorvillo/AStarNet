using System.Collections.Generic;

namespace AStarNet.Maps;

/// <summary>
/// Represents a navigable map containing nodes used by the pathfinding algorithm.
/// </summary>
/// <remarks>
/// Implementations must return a node whose identifier matches the identifier requested by <see cref="GetNode"/> and
/// must return a non-null connection sequence from <see cref="GetConnections"/>. The pathfinder validates these
/// requirements and rejects invalid results.
/// </remarks>
/// <typeparam name="TContent">The type of the optional node content.</typeparam>
public interface INodeMap<TContent>
{
    /// <summary>
    /// Retrieves the node associated with the specified identifier.
    /// </summary>
    /// <param name="id">The identifier of the node to retrieve.</param>
    /// <returns>The matching node with the requested identifier, or <see langword="null"/> when no node exists.</returns>
    PathNode<TContent>? GetNode(int id);

    /// <summary>
    /// Retrieves the outgoing connections of a specified node.
    /// </summary>
    /// <param name="node">The node whose connections are requested.</param>
    /// <returns>The non-null sequence of outgoing connections of <paramref name="node"/>.</returns>
    IEnumerable<PathConnection<TContent>> GetConnections(PathNode<TContent> node);
}
