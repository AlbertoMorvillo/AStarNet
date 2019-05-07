
// Copyright (c) 2019 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System;

namespace AStarNet
{
    /// <summary>
    /// Contains data to manage the nodes for the A* algorithm.
    /// </summary>
    public class Node<T> : INode<T>, IComparable<Node<T>>, IEquatable<Node<T>>
    {
        #region Fields

        /// <summary>
        /// 
        /// </summary>
        protected readonly Guid _id;                    // Identifier for this node

        /// <summary>
        /// 
        /// </summary>
        protected readonly Node<T> _parent;             // Node used during the search to record the parent of successor nodes

        /// <summary>
        /// 
        /// </summary>
        protected readonly double _cost;                // Cost of this node

        /// <summary>
        /// 
        /// </summary>
        protected readonly double _pathCost;            // Cost of the path from the start to this node

        /// <summary>
        /// 
        /// </summary>
        protected readonly double _heuristicDistance;   // Heuristic estimate of distance to goal

        /// <summary>
        /// 
        /// </summary>
        protected readonly double _pathScore;           // Sum of cumulative cost of predecessors and self and heuristic

        #endregion

        #region Properties

        /// <summary>
        /// Gets the identifier for this node.
        /// </summary>
        public Guid ID
        {
            get
            {
                return this._id;
            }
        }

        /// <summary>
        /// Gets the <see cref="Node{T}"/> used during the search to record the parent of successor nodes.
        /// </summary>
        public Node<T> Parent
        {
            get
            {
                return this._parent;
            }
        }

        /// <summary>
        /// Gets the cost of this node.
        /// </summary>
        public double Cost
        {
            get
            {
                return this._cost;
            }
        }

        /// <summary>
        /// Gets the cost of the path from the start to this node (G).
        /// </summary>
        public double PathCost
        {
            get
            {
                return this._pathCost;
            }
        }

        /// <summary>
        /// Gets the heuristic estimate of distance to goal (H).
        /// </summary>
        public double HeuristicDistance
        {
            get
            {
                return this._heuristicDistance;
            }
        }

        /// <summary>
        /// Gets the sum of cumulative cost of predecessors and self and heuristic (F).
        /// </summary>
        public double PathScore
        {
            get
            {
                return this._pathScore;
            }
        }

        /// <summary>
        /// Gets or sets the <typeparamref name="T"/> content of this node.
        /// </summary>
        public T Content
        {
            get;
            set;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of a <see cref="Node{T}"/>.
        /// </summary>
        /// <param name="nodeGuid"><see cref="Guid"/> of this node.</param>
        /// <param name="content">The <typeparamref name="T"/> content of this node.</param>
        public Node(Guid nodeGuid, T content)
            : this(nodeGuid, content,  null, 0, 0)
        {
        }

        /// <summary>
        /// Creates a new instance of a node.
        /// </summary>
        /// <param name="nodeGuid"><see cref="Guid"/> of this node.</param>
        /// <param name="content">The <typeparamref name="T"/> content of this node.</param>
        /// <param name="parent">Parent <see cref="Node{T}"/>.</param>
        /// <param name="cost">Cost from parent node.</param>
        /// <param name="heuristicDistance">Heuristic distance from the destination node.</param>
        public Node(Guid nodeGuid, T content, Node<T> parent, double cost, double heuristicDistance)
        {
            double parentPathCost = parent == null ? 0 : parent.PathCost;

            this._id = nodeGuid;
            this._parent = parent;
            this._cost = cost;
            this._pathCost = parentPathCost + this.Cost;
            this._heuristicDistance = heuristicDistance;
            this._pathScore = this.PathCost + this.HeuristicDistance;
            this.Content = content;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Create a new <see cref="Node{T}"/> with the same Id, cost, heuristic distance and content of this one.
        /// </summary>
        /// <returns>A new <see cref="Node{T}"/> with the same Id, cost, heuristic distance and content of this one.</returns>
        public Node<T> CopyWithoutParent()
        {
            Node<T> cloneNode = null;

            cloneNode = new Node<T>(this.ID, this.Content, null, this.Cost, this.HeuristicDistance);

            return cloneNode;
        }

        #endregion

        #region IComparable<Node> Members

        /// <summary>
        /// Compares first the path score then the path cost of this node with another one.
        /// </summary>
        /// <param name="other">Other <see cref="Node{T}"/> to compare.</param>
        /// <returns>
        /// <para>Less than zero: This node as the path score less than other node or the path score equal and the current cost less than other node.</para>
        /// <para>Zero: This node has the path score and the current cost equal to other node.</para>
        /// <para>Greater than zero: This node as the path score greater than other node or the path score equal ant the current cost greater than other node.</para>
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
        public int CompareTo(Node<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            int scoreCompare = this.PathScore.CompareTo(other.PathScore);

            if (scoreCompare == 0)
            {
                // Path scores equals: return the path cost comparison
                return this.PathCost.CompareTo(other.PathCost);
            }
            else
            {
                // Path scores not equals: return the path scores comparison
                return scoreCompare;
            }
        }

        #endregion

        #region IEquatable<Node> Members

        /// <summary>
        /// Returns a value indicating whether this istance and a specific <see cref="Node{T}"/> rappresent the same node.
        /// </summary>
        /// <param name="other">Other <see cref="Node{T}"/> istance.</param>
        /// <returns>True if this and the other istance rappresent the same node.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
        public bool Equals(Node<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            return this.ID.Equals(other.ID);
        }

        #endregion
    }
}
