using AStarNet.Maps;

namespace AStarNet.Tests;

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
