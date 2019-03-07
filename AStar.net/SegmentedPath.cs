
// Copyright (c) 2019 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System;

namespace AStarNet
{
    /// <summary>
    /// Contains a collection of <see cref="IPath{T}"/> segments which defines the path, sorted from start to destination node.
    /// </summary>
    /// <typeparam name="T">Type of the node content.</typeparam>
    public class SegmentedPath<T> : IComparable<SegmentedPath<T>>, IEquatable<SegmentedPath<T>>
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
        /// Gets the segments in this path.
        /// </summary>
        public IPath<T>[] Segments
        {
            get;
            private set;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of a <see cref="SegmentedPath{T}"/>.
        /// </summary>
        public SegmentedPath()
        {
            this.Segments = new IPath<T>[0];
            this.Cost = 0;
        }

        /// <summary>
        /// Creates a new instance of a <see cref="SegmentedPath{T}"/>.
        /// </summary>
        /// <param name="segments"><see cref="IPath{T}"/> array containing path segments.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="segments"/> is <see langword="null"/>.</exception>
        public SegmentedPath(IPath<T>[] segments)
        {
            if (segments == null)
                throw new ArgumentNullException(nameof(segments));

            this.Segments = segments;
            this.Cost = 0;

            for (int i = 0; i < this.Segments.Length; i++)
            {
                this.Cost += this.Segments[i].Cost;
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Adds a segment to this path.
        /// </summary>
        /// <param name="path"><see cref="IPath{T}"/> segment which will be added in this path.</param>
        public void Add(IPath<T> segment)
        {
            IPath<T>[] newSegments = new IPath<T>[this.Segments.Length + 1];

            this.Segments.CopyTo(newSegments, 0);
            newSegments[this.Segments.Length] = segment;

            this.Segments = newSegments;
            this.Cost += segment.Cost;
        }

        /// <summary>
        /// Adds more segments to this path.
        /// </summary>
        /// <param name="path"><see cref="IPath{T}"/> segment array which will be added in this path.</param>
        public void Add(IPath<T>[] segments)
        {
            IPath<T>[] newSegments = new IPath<T>[this.Segments.Length + segments.Length];

            this.Segments.CopyTo(newSegments, 0);
            segments.CopyTo(newSegments, this.Segments.Length);

            this.Segments = newSegments;

            for (int i = 0; i < segments.Length; i++)
            {
                this.Cost += segments[i].Cost;
            }
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
                for (int i = 0; i < this.Segments.Length; i++)
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
