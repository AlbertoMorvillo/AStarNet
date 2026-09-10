using System;

namespace AStarNet.Heuristics;

/// <summary>
/// Provides a heuristic function for pathfinding.
/// </summary>
/// <remarks>
/// To guarantee an optimal path, estimates must be admissible: they must not exceed the minimum remaining cost.
/// The pathfinder cannot check this because it depends on the graph and its costs.
/// During each <see cref="PathFinder.FindPath"/>, the provider must return the same
/// <see cref="double"/> value for the same pair of source and destination node identifiers. The pathfinder may cache
/// and reuse estimates. The number and order of calls to <see cref="GetHeuristic"/> are not
/// part of the contract. Changes to provider data must preserve these rules for all active searches.
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
