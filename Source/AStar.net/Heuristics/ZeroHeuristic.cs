// Copyright (c) 2025 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System;

namespace AStarNet.Heuristics
{
    /// <summary>
    /// Represents an heuristic provider that always returns zero as the estimated cost between any two nodes.
    /// This effectively disables the heuristic component, making the A* algorithm behave like Dijkstra's algorithm.
    /// </summary>
    /// <typeparam name="TId">The type of the identifier for the nodes.</typeparam>
    /// <typeparam name="TNode">The type of nodes, implementing <see cref="IPathNode{TId}"/>.</typeparam>
    public class ZeroHeuristic<TId, TNode> : IHeuristicProvider<TId, TNode>
        where TId : notnull, IEquatable<TId>
        where TNode : IPathNode<TId>
    {
        /// <inheritdoc/>
        public double GetHeuristic(TNode from, TNode to) => 0;
    }
}
