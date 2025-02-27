// Copyright (c) 2025 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

namespace AStarNet.Heuristics
{
    /// <summary>
    /// Represents an heuristic provider that always returns zero as the estimated cost between any two nodes.
    /// This effectively disables the heuristic component, making the A* algorithm behave like Dijkstra's algorithm.
    /// </summary>
    /// <typeparam name="TId">The type of the node identifier.</typeparam>
    public class ZeroHeuristic<TId> : IHeuristicProvider<TId> where TId : notnull
    {
        /// <inheritdoc/>
        public double GetHeuristic(IPathNode<TId> from, IPathNode<TId> to) => 0;
    }
}
