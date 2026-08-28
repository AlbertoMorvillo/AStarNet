using AStarNet.Maps;
namespace AStarNet.Tests;

/// <summary>
/// Provides a small immutable graph tailored to pathfinding tests.
/// </summary>
internal sealed class TestGraph : INodeMap
{
    private readonly HashSet<int> _nodeIds;
    private readonly Dictionary<int, PathConnection[]> _connections;

    /// <summary>
    /// Initializes a graph from node identifiers and directed weighted edges.
    /// </summary>
    /// <param name="nodeIds">The identifiers of all nodes in the graph.</param>
    /// <param name="edges">The directed edges represented by source, destination, and cost.</param>
    internal TestGraph(IEnumerable<int> nodeIds, params (int From, int To, double Cost)[] edges)
    {
        HashSet<int> identifiers = nodeIds.ToHashSet();
        Dictionary<int, List<PathConnection>> connections = identifiers.ToDictionary(
            id => id,
            _ => new List<PathConnection>());

        foreach ((int from, int to, double cost) in edges)
        {
            connections[from].Add(new PathConnection(to, cost));
        }

        this._nodeIds = identifiers;
        this._connections = connections.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
    }

    /// <inheritdoc/>
    public bool ContainsNode(int nodeId)
    {
        return this._nodeIds.Contains(nodeId);
    }

    /// <inheritdoc/>
    public IEnumerable<PathConnection>? GetConnections(int nodeId)
    {
        this._connections.TryGetValue(nodeId, out PathConnection[]? connections);
        return connections;
    }
}
