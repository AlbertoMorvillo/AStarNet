
// Copyright (c) 2019 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System;

namespace AStarNet
{
    /// <summary>
    /// Contains data to manage the nodes for the A* algorithm.
    /// </summary>
    public interface INode<T>
    {
        #region Properties

        /// <summary>
        /// Gets the identifier for this node.
        /// </summary>
        Guid ID { get; }

        /// <summary>
        /// Gets the <see cref="Node{T}"/> used during the search to record the parent of successor nodes.
        /// </summary>
        Node<T> Parent { get; }

        /// <summary>
        /// Gets the cost of this node.
        /// </summary>
        double Cost { get; }

        /// <summary>
        /// Gets the cost of the path from the start to this node (G).
        /// </summary>
        double PathCost { get; }

        /// <summary>
        /// Gets the heuristic estimate of distance to goal (H).
        /// </summary>
        double HeuristicDistance { get; }

        /// <summary>
        /// Gets the sum of cumulative cost of predecessors and self and heuristic (F).
        /// </summary>
        double PathScore { get; }
        
        /// <summary>
        /// Gets or sets the <typeparamref name="T"/> content of this node.
        /// </summary>
        T Content { get; }

        #endregion

        #region Public methods

        /// <summary>
        /// Create a new <see cref="Node{T}"/> with the same ID, cost, heuristic distance and content of this one.
        /// </summary>
        /// <returns>A new <see cref="Node{T}"/> with the same ID, cost, heuristic distance and content of this one.</returns>
        Node<T> CopyWithoutParent();

        #endregion
    }

}
