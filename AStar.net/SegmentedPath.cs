
// Copyright (c) 2019 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;

namespace AStarNet
{
    /// <summary>
    /// Contains a collection of <see cref="IPath{T}"/> segments which defines the path, sorted from start to destination node.
    /// </summary>
    /// <typeparam name="T">Type of the node content.</typeparam>
    public class SegmentedPath<T> : IComparable<SegmentedPath<T>>, IEquatable<SegmentedPath<T>>
    {
        #region Fields

        /// <summary>
        /// The segments in this path.
        /// </summary>
        protected List<IPath<T>> _segments;

        #endregion

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
        /// Gets the segments in this path.
        /// </summary>
        public IReadOnlyList<IPath<T>> Segments
        {
            get
            {
                return this._segments;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of a <see cref="SegmentedPath{T}"/>.
        /// </summary>
        public SegmentedPath()
        {
            this._segments = new List<IPath<T>>();
            this.Cost = 0;
        }

        /// <summary>
        /// Creates a new instance of a <see cref="SegmentedPath{T}"/>.
        /// </summary>
        /// <param name="segmentCollection"><see cref="IPath{T}"/> collection containing path segments.</param>
        /// <exception cref="ArgumentNullException"><paramref name="segmentCollection"/> is <see langword="null"/>.</exception>
        public SegmentedPath(IEnumerable<IPath<T>> segmentCollection)
        {
            if (segmentCollection == null)
                throw new ArgumentNullException(nameof(segmentCollection));

            this._segments = new List<IPath<T>>(segmentCollection);
            this.Cost = 0;

            for (int i = 0; i < this.Segments.Count; i++)
            {
                this.Cost += this.Segments[i].Cost;
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Adds a segment to this path.
        /// </summary>
        /// <param name="segment"><see cref="IPath{T}"/> segment which will be added in this path.</param>
        public void Add(IPath<T> segment)
        {
            this._segments.Add(segment);
            this.Cost += segment.Cost;
        }

        /// <summary>
        /// Adds a range of segments to this path.
        /// </summary>
        /// <param name="segmentCollection"><see cref="IPath{T}"/> segment collection which will be added in this path.</param>
        public void AddRange(IEnumerable<IPath<T>> segmentCollection)
        {
            this._segments.AddRange(segmentCollection);

            foreach (IPath<T> path in segmentCollection)
            {
                this.Cost += path.Cost;
            }
        }

        /// <summary>
        /// Removes the segment at the specific index from this path.
        /// </summary>
        /// <param name="index">The zero-based index of the segment to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0. -or- <paramref name="index"/> is equal to or greater than <see cref="SegmentedPath{T}.Segments"/> count.</exception>
        public void RemoveAt(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (index >= this.Segments.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            this.Cost -= this._segments[index].Cost;

            this._segments.RemoveAt(index);
        }

        /// <summary>
        /// Removes all segments from this path.
        /// </summary>
        public void Clear()
        {
            this._segments.Clear();
        }

        #endregion

        #region IComparable<Node> Members

        /// <summary>
        /// Compares the cost of this path with another one.
        /// </summary>
        /// <param name="other">Other <see cref="SegmentedPath{T}"/> to compare.</param>
        /// <returns>
        /// <para>Less than zero: This path has the cost less than other path.</para>
        /// <para>Zero: This path has the cost equal to other node.</para>
        /// <para>Greater than zero: This path has the cost greater than other node.</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is <see langword="null"/>.</exception>
        public int CompareTo(SegmentedPath<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            int scoreCompare = this.Cost.CompareTo(other.Cost);

            return scoreCompare;
        }

        #endregion

        #region IEquatable<Node> Members

        /// <summary>
        /// Returns a value indicating whether this istance and a specific <see cref="SegmentedPath{T}"/> rappresent the same path.
        /// </summary>
        /// <param name="other">Other <see cref="SegmentedPath{T}"/> istance.</param>
        /// <returns>True if this and the other istance rappresent the same path.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is <see langword="null"/>.</exception>
        public bool Equals(SegmentedPath<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (this.CompareTo(other) == 0)
            {
                for (int i = 0; i < this.Segments.Count; i++)
                {
                    if (!this.Segments[i].Equals(other.Segments[i]))
                        return false;
                }
            }

            return true;
        }

        #endregion
    }
}
