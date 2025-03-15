// Copyright (c) 2025 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System;

namespace AStarNet.Heuristics
{
    /// <summary>
    /// Provides a heuristic function for pathfinding.
    /// </summary>
    /// <typeparam name="TId">The type of the identifier for the nodes.</typeparam>
    /// <typeparam name="TNode">The type of nodes, implementing <see cref="IPathNode{TId}"/>.</typeparam>
    public interface IHeuristicProvider<TId, TNode>
        where TId : notnull, IEquatable<TId>
        where TNode : IPathNode<TId>
    {
        /// <summary>
        /// Computes the heuristic estimate from one node to another.
        /// </summary>
        /// <param name="from">The start node.</param>
        /// <param name="to">The destination node.</param>
        /// <returns>The estimated cost from <paramref name="from"/> to <paramref name="to"/>.</returns>
        double GetHeuristic(TNode from, TNode to);
    }
}
