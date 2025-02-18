// Copyright (c) 2025 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System;

namespace AStarNet
{
    /// <summary>
    /// Defines a generic node used within a navigation or pathfinding process.
    /// </summary>
    /// <typeparam name="TContent">The type of the node content.</typeparam>
    public interface INode<TContent>
    {
        #region Properties

        /// <summary>
        /// Gets the unique identifier of the node.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Gets the cost of this node.
        /// </summary>
        double Cost { get; }

        /// <summary>
        /// Gets the <typeparamref name="TContent"/> content of this node.
        /// </summary>
        TContent Content { get; }

        #endregion
    }

}
