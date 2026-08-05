// Copyright (c) 2026 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

namespace AStarNet.Heuristics;

/// <summary>
/// Provides a heuristic function for pathfinding.
/// </summary>
/// <typeparam name="TContent">The type of the optional node content.</typeparam>
public interface IHeuristicProvider<TContent>
{
    /// <summary>
    /// Computes the heuristic estimate from one node to another.
    /// </summary>
    /// <param name="from">The start node.</param>
    /// <param name="to">The destination node.</param>
    /// <returns>The estimated cost from <paramref name="from"/> to <paramref name="to"/>.</returns>
    double GetHeuristic(PathNode<TContent> from, PathNode<TContent> to);
}
