// Copyright (c) 2025 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

namespace AStarNet.Heuristics
{
    /// <summary>
    /// Provides a heuristic function for pathfinding.
    /// </summary>
    /// <typeparam name="TId">The type of the node identifier.</typeparam>
    public interface IHeuristicProvider<TId> where TId : notnull
    {
        /// <summary>
        /// Computes the heuristic estimate from one node to another.
        /// </summary>
        /// <param name="from">The start <see cref="IPathNode{TId}"/>.</param>
        /// <param name="to">The destination <see cref="IPathNode{TId}"/>.</param>
        /// <returns>The estimated cost from <paramref name="from"/> to <paramref name="to"/>.</returns>
        double GetHeuristic(IPathNode<TId> from, IPathNode<TId> to);
    }
}
