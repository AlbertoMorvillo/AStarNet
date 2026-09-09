using System.Collections.Generic;

namespace AStarNet.Maps;

/// <summary>
/// Defines the nodes and directed connections available to the pathfinder.
/// </summary>
/// <remarks>
/// Maps may generate connections on demand and change freely between searches. During each
/// <see cref="PathFinder.FindPath"/> call, the topology, connections, and costs seen by the pathfinder must stay
/// consistent and stable, including during enumeration. Changing them while A* is running is unsupported and may
/// produce invalid or nonoptimal results. Wait for active searches to finish before making changes, or give each
/// search a stable snapshot.
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
