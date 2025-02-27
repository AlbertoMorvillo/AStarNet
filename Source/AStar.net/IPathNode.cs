// Copyright (c) 2025 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System;

namespace AStarNet
{
    /// <summary>
    /// Defines a generic node used within a navigation or pathfinding process.
    /// </summary>
    /// <typeparam name="TId">The type of the node identifier.</typeparam>
    public interface IPathNode<TId> : IEquatable<IPathNode<TId>> where TId : notnull
    {
        #region Properties

        /// <summary>
        /// Gets the identifier of the node.
        /// </summary>
        TId Id { get; }

        /// <summary>
        /// Gets the cost of this node.
        /// </summary>
        double Cost { get; }

        #endregion
    }
}
