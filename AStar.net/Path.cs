
// Copyright (c) 2019 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;

namespace AStarNet
{
    /// <summary>
    /// Contains the node sequence which defines the path, sorted from start to destination node.
    /// </summary>
    /// <typeparam name="T">Type of the node content.</typeparam>
    public class Path<T> : IPath<T>
    {
        #region Properties

        /// <summary>
        /// Gets the cost of this path.
        /// </summary>
        public double Cost
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the nodes in this path.
        /// </summary>
        public T[] Nodes
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the generic tag of this path.
        /// </summary>
        public object Tag
        {
            get;
            set;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of a <see cref="Path{T}"/>.
        /// </summary>
        public Path()
        {
            this.Nodes = new T[0];
            this.Cost = 0;
        }

        /// <summary>
        /// Creates a new instance of a <see cref="Path{T}"/>.
        /// </summary>
        /// <param name="nodes"><typeparamref name="T"/> array containing the sorted node contents of this path, from start to destination.</param>
        /// <param name="cost">The cost of this path.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="nodes"/> is <see langword="null"/>.</exception>
        public Path(T[] nodes, double cost)
        {
            if (nodes == null)
                throw new ArgumentNullException(nameof(nodes));

            this.Nodes = nodes;
            this.Cost = cost;
        }

        /// <summary>
        /// Creates a new instance of a path.
        /// </summary>
        /// <param name="nodes"><see cref="Node{T}"/> list containing the sorted nodes of this path, from start to destination.</param>
        /// <param name="cost">The cost of this path.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="nodes"/> is <see langword="null"/>.</exception>
        public Path(IList<Node<T>> nodes, double cost)
        {
            if (nodes == null)
                throw new ArgumentNullException(nameof(nodes));

            List<T> contents = new List<T>();

            for (int i = 0; i < nodes.Count; i++)
            {
                contents.Add(nodes[i].Content);
            }

            this.Nodes = contents.ToArray();
            this.Cost = cost;
        }

        /// <summary>
        /// Creates a new instance of a path.
        /// </summary>
        /// <param name="segments"><see cref="Path{T}"/> array containing path segments.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="segments"/> is <see langword="null"/>.</exception>
        public Path(Path<T>[] segments)
        {
            if (segments == null)
                throw new ArgumentNullException(nameof(segments));

            List<T> contents = new List<T>();
            double totalCost = 0;

            for (int i = 0; i < segments.Length; i++)
            {
                contents.AddRange(segments[i].Nodes);
                totalCost += segments[i].Cost;
            }

            this.Nodes = contents.ToArray();
            this.Cost = totalCost;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Adds a path to this path.
        /// </summary>
        /// <param name="path">Another <see cref="Path{T}"/> which will be added in this path.</param>
        public void Add(Path<T> path)
        {
            T[] newNodes = new T[this.Nodes.Length + path.Nodes.Length];

            this.Nodes.CopyTo(newNodes, 0);
            path.Nodes.CopyTo(newNodes, this.Nodes.Length);

            this.Nodes = newNodes;
            this.Cost += path.Cost;
        }

        #endregion

        #region IComparable<Node> Members

        /// <summary>
        /// Compares first the cost then the length of this path with another one.
        /// </summary>
        /// <param name="other">Other <see cref="Path{T}"/> to compare.</param>
        /// <returns>
        /// <para>Less than zero: This path has the cost less than other path or the cost equal and the length less than other path.</para>
        /// <para>Zero: This path has the cost and the length equal to other node.</para>
        /// <para>Greater than zero: This path has the cost greater than other node or the cost equal and the length greater than the other path.</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is <see langword="null"/>.</exception>
        public int CompareTo(IPath<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            int scoreCompare = this.Cost.CompareTo(other.Cost);

            if (scoreCompare == 0)
            {
                // Cost equals: return the path cost comparison
                return this.Nodes.Length.CompareTo(other.Nodes.Length);
            }
            else
            {
                // Cost not equals: return the path scores comparison
                return scoreCompare;
            }
        }

        #endregion

        #region IEquatable<Node> Members

        /// <summary>
        /// Returns a value indicating whether this istance and a specific <see cref="Path{T}"/> rappresent the same path.
        /// </summary>
        /// <param name="other">Other <see cref="Path{T}"/> istance.</param>
        /// <returns>True if this and the other istance rappresent the same path.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is <see langword="null"/>.</exception>
        public bool Equals(IPath<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (this.CompareTo(other) == 0)
            {
                for (int i = 0; i < this.Nodes.Length; i++)
                {
                    if (this.Nodes[i] is IEquatable<T> eNode && other.Nodes[i] is IEquatable<T> eOther)
                    {
                        if (!eNode.Equals(eOther))
                            return false;
                    }
                    else
                    {
                        if (!this.Nodes[i].Equals(other.Nodes[i]))
                            return false;
                    }
                }
            }

            return true;
        }

        #endregion
    }
}
