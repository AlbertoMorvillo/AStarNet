
// Copyright (c) 2019 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System;

namespace AStarNet
{
    /// <summary>
    /// Contains the node sequence which defines the path, sorted from start to destination node.
    /// </summary>
    /// <typeparam name="T">Type of the node content.</typeparam>

    public interface IPath<T> : IComparable<IPath<T>>, IEquatable<IPath<T>>
    {
        #region Properties

        /// <summary>
        /// Gets the cost of this path.
        /// </summary>
        double Cost { get; }

        /// <summary>
        /// Gets the nodes in this path.
        /// </summary>
        T[] Nodes { get; }

        /// <summary>
        /// Gets or sets the generic tag of this path.
        /// </summary>
        object Tag { get; set; }

        #endregion

        #region Public methods

        /// <summary>
        /// Adds a path to this path.
        /// </summary>
        /// <param name="path">Another <see cref="Path{T}"/> which will be added in this path.</param>
        void Add(Path<T> path);

        #endregion
    }
}
