// Copyright (c) 2026 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System;

namespace AStarNet.Heuristics;

/// <summary>
/// Provides a heuristic function for pathfinding.
/// </summary>
/// <remarks>
/// To preserve the optimality guarantee of A*, estimates must be admissible for the node map: an estimate must never
/// exceed the actual minimum cost of reaching the destination. Admissibility depends on the graph and its traversal
/// costs and therefore cannot be validated by the pathfinder.
/// </remarks>
/// <typeparam name="TContent">The type of the optional node content.</typeparam>
public interface IHeuristicProvider<TContent>
{
    /// <summary>
    /// Computes the heuristic estimate from one node to another.
    /// </summary>
    /// <param name="from">The start node.</param>
    /// <param name="to">The destination node.</param>
    /// <returns>
    /// The finite, non-negative estimated cost from <paramref name="from"/> to <paramref name="to"/>.
    /// </returns>
    /// <remarks>
    /// Returning a negative value, infinity, or <see cref="double.NaN"/> causes the path search to throw an
    /// <see cref="InvalidOperationException"/>.
    /// </remarks>
    double GetHeuristic(PathNode<TContent> from, PathNode<TContent> to);
}
