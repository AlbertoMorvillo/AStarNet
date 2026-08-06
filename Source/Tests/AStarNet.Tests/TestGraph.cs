using AStarNet.Maps;

namespace AStarNet.Tests;

/// <summary>
/// Provides a small immutable graph tailored to pathfinding tests.
/// </summary>
internal sealed class TestGraph : INodeMap<string>
{
    private readonly Dictionary<int, PathNode<string>> _nodes;
    private readonly Dictionary<int, PathConnection<string>[]> _connections;

    /// <summary>
    /// Initializes a graph from node identifiers and directed weighted edges.
    /// </summary>
    /// <param name="nodeIds">The identifiers of all nodes in the graph.</param>
    /// <param name="edges">The directed edges represented by source, destination, and cost.</param>
    internal TestGraph(IEnumerable<int> nodeIds, params (int From, int To, double Cost)[] edges)
    {
        Dictionary<int, PathNode<string>> nodes = nodeIds.ToDictionary(
            id => id,
            id => new PathNode<string>(id, $"Node {id}"));
        Dictionary<int, List<PathConnection<string>>> connections = nodes.Keys.ToDictionary(
            id => id,
            _ => new List<PathConnection<string>>());

        foreach ((int from, int to, double cost) in edges)
        {
            connections[from].Add(new PathConnection<string>(nodes[to], cost));
        }

        this._nodes = nodes;
        this._connections = connections.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
    }

    /// <inheritdoc/>
    public PathNode<string>? GetNode(int id)
    {
        this._nodes.TryGetValue(id, out PathNode<string>? node);
        return node;
    }

    /// <inheritdoc/>
    public IEnumerable<PathConnection<string>> GetConnections(PathNode<string> node)
    {
        return this._connections[node.Id];
    }
}

/// <summary>
/// Provides delegates for simulating valid and invalid node-map implementations.
/// </summary>
internal sealed class DelegateNodeMap : INodeMap<string>
{
    private readonly Func<int, PathNode<string>?> _getNode;
    private readonly Func<PathNode<string>, IEnumerable<PathConnection<string>>> _getConnections;

    /// <summary>
    /// Initializes a delegate-backed node map.
    /// </summary>
    /// <param name="getNode">The node lookup operation.</param>
    /// <param name="getConnections">The connection lookup operation.</param>
    internal DelegateNodeMap(
        Func<int, PathNode<string>?> getNode,
        Func<PathNode<string>, IEnumerable<PathConnection<string>>> getConnections)
    {
        this._getNode = getNode;
        this._getConnections = getConnections;
    }

    /// <inheritdoc/>
    public PathNode<string>? GetNode(int id)
    {
        return this._getNode(id);
    }

    /// <inheritdoc/>
    public IEnumerable<PathConnection<string>> GetConnections(PathNode<string> node)
    {
        return this._getConnections(node);
    }
}

/// <summary>
/// Provides a delegate-backed heuristic for focused tests.
/// </summary>
internal sealed class DelegateHeuristic : Heuristics.IHeuristicProvider<string>
{
    private readonly Func<PathNode<string>, PathNode<string>, double> _getHeuristic;

    /// <summary>
    /// Initializes a delegate-backed heuristic.
    /// </summary>
    /// <param name="getHeuristic">The heuristic calculation.</param>
    internal DelegateHeuristic(Func<PathNode<string>, PathNode<string>, double> getHeuristic)
    {
        this._getHeuristic = getHeuristic;
    }

    /// <inheritdoc/>
    public double GetHeuristic(PathNode<string> from, PathNode<string> to)
    {
        return this._getHeuristic(from, to);
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
    internal static Path<string> Create(int startId, params (int DestinationId, double Cost)[] connections)
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
        PathFinder<string> pathFinder = new(graph);
        return pathFinder.FindPath(startId, currentNodeId);
    }
}
