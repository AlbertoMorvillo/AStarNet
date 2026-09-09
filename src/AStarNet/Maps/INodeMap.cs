using System.Collections.Generic;

namespace AStarNet.Maps;

/// <summary>
/// Represents a navigable map containing nodes used by the pathfinding algorithm.
/// </summary>
/// <remarks>
/// Implementations define which node identifiers exist and the directed connections originating from each node.
/// Maps may be procedural or dynamic and generate connections on demand. They may change freely between separate
/// searches. During a single execution of <see cref="PathFinder.FindPath"/>, however, the topology, connections, and
/// traversal costs observable by the pathfinder must remain consistent and stable, including during enumeration.
/// Changing the observable map while a search is running is not supported by the current A* algorithm and may
/// produce invalid or nonoptimal results. Mutable implementations must coordinate changes with active and
/// concurrent searches or provide a stable snapshot for each search.
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
