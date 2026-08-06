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

/// <summary>
/// Provides delegates for simulating valid and invalid node-map implementations.
/// </summary>
internal sealed class DelegateNodeMap : INodeMap
{
    private readonly Func<int, bool> _containsNode;
    private readonly Func<int, IEnumerable<PathConnection>?> _getConnections;

    /// <summary>
    /// Initializes a delegate-backed node map.
    /// </summary>
    /// <param name="containsNode">The node-existence operation.</param>
    /// <param name="getConnections">The connection lookup operation.</param>
    internal DelegateNodeMap(
        Func<int, bool> containsNode,
        Func<int, IEnumerable<PathConnection>?> getConnections)
    {
        this._containsNode = containsNode;
        this._getConnections = getConnections;
    }

    /// <inheritdoc/>
    public bool ContainsNode(int nodeId)
    {
        return this._containsNode(nodeId);
    }

    /// <inheritdoc/>
    public IEnumerable<PathConnection>? GetConnections(int nodeId)
    {
        return this._getConnections(nodeId);
    }
}

/// <summary>
/// Provides a delegate-backed heuristic for focused tests.
/// </summary>
internal sealed class DelegateHeuristic : Heuristics.IHeuristicProvider
{
    private readonly Func<int, int, double> _getHeuristic;

    /// <summary>
    /// Initializes a delegate-backed heuristic.
    /// </summary>
    /// <param name="getHeuristic">The heuristic calculation.</param>
    internal DelegateHeuristic(Func<int, int, double> getHeuristic)
    {
        this._getHeuristic = getHeuristic;
    }

    /// <inheritdoc/>
    public double GetHeuristic(int fromNodeId, int toNodeId)
    {
        return this._getHeuristic(fromNodeId, toNodeId);
    }
}

/// <summary>
/// Creates paths directly for testing path behavior independently from pathfinding.
/// </summary>
internal static class TestPathFactory
{
    /// <summary>
    /// Creates a path from a start identifier and ordered destination-cost pairs.
    /// </summary>
    /// <param name="startId">The start-node identifier.</param>
    /// <param name="connections">The ordered destination identifiers and costs.</param>
    /// <returns>The created path.</returns>
    internal static Path Create(int startId, params (int DestinationId, double Cost)[] connections)
    {
        int[] nodeIds = [startId, .. connections.Select(connection => connection.DestinationId)];
        (int From, int To, double Cost)[] edges = new (int From, int To, double Cost)[connections.Length];
        int currentNodeId = startId;

        for (int index = 0; index < connections.Length; index++)
        {
            (int destinationId, double cost) = connections[index];
            edges[index] = (currentNodeId, destinationId, cost);
            currentNodeId = destinationId;
        }

        TestGraph graph = new(nodeIds.Distinct(), edges);
        PathFinder pathFinder = new(graph);
        return pathFinder.FindPath(startId, currentNodeId);
    }
}
