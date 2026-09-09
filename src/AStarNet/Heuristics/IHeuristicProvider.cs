using System;

namespace AStarNet.Heuristics;

/// <summary>
/// Provides a heuristic function for pathfinding.
/// </summary>
/// <remarks>
/// To preserve the optimality guarantee of A*, estimates must be admissible for the node map: an estimate must never
/// exceed the actual minimum cost of reaching the destination. Admissibility depends on the graph and its traversal
/// costs and therefore cannot be validated by the pathfinder.
/// During a single execution of <see cref="PathFinder.FindPath"/>, the provider must return the same
/// <see cref="double"/> value for the same pair of source and destination node identifiers. The pathfinder may cache
/// and reuse previously calculated estimates. The number and order of calls to <see cref="GetHeuristic"/> are not
/// part of the contract. Mutable implementations must coordinate changes with active and concurrent searches.
/// </remarks>
public interface IHeuristicProvider
{
    /// <summary>
    /// Computes the heuristic estimate from one node to another.
    /// </summary>
    /// <param name="fromNodeId">The start-node identifier.</param>
    /// <param name="toNodeId">The destination-node identifier.</param>
    /// <returns>The finite, non-negative estimated cost between the nodes.</returns>
    /// <remarks>
    /// Returning a negative value, infinity, or <see cref="double.NaN"/> causes the path search to throw an
    /// <see cref="InvalidOperationException"/>.
    /// </remarks>
    double GetHeuristic(int fromNodeId, int toNodeId);
}
