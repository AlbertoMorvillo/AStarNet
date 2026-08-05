// Copyright (c) 2026 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

namespace AStarNet.Heuristics;

/// <summary>
/// Provides a heuristic that always returns zero, making pathfinding behave like Dijkstra's algorithm.
/// </summary>
/// <typeparam name="TContent">The type of the optional node content.</typeparam>
public sealed class ZeroHeuristic<TContent> : IHeuristicProvider<TContent>
{
    /// <inheritdoc/>
    public double GetHeuristic(PathNode<TContent> from, PathNode<TContent> to)
    {
        return 0;
    }
}
