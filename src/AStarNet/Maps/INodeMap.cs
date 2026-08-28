using System.Collections.Generic;

namespace AStarNet.Maps;

/// <summary>
/// Represents a navigable map containing nodes used by the pathfinding algorithm.
/// </summary>
/// <remarks>
/// Implementations define which node identifiers exist and the directed connections originating from each node.
/// </remarks>
public interface INodeMap
{
    /// <summary>
    /// Determines whether a node identifier exists in the map.
    /// </summary>
    /// <param name="nodeId">The node identifier.</param>
    /// <returns><see langword="true"/> when the node exists; otherwise, <see langword="false"/>.</returns>
    bool ContainsNode(int nodeId);

    /// <summary>
    /// Retrieves the outgoing connections of a specified node.
    /// </summary>
    /// <param name="nodeId">The identifier of the node whose connections are requested.</param>
    /// <returns>
    /// The outgoing connections of the node, an empty sequence when the node has no outgoing connections, or
    /// <see langword="null"/> only when <paramref name="nodeId"/> does not identify an existing node.
    /// </returns>
    IEnumerable<PathConnection>? GetConnections(int nodeId);
}
