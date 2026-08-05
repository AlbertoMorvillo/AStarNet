// Copyright (c) 2026 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System;

namespace AStarNet;

/// <summary>
/// Defines a path node with an integer identifier and optional content.
/// </summary>
/// <typeparam name="TContent">The type of the optional content associated with the node.</typeparam>
public class PathNode<TContent> : IEquatable<PathNode<TContent>>
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="PathNode{TContent}"/> class.
    /// </summary>
    /// <param name="id">The identifier of the node.</param>
    /// <param name="content">The optional content associated with the node.</param>
    public PathNode(int id, TContent? content = default)
    {
        this.Id = id;
        this.Content = content;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the identifier of the node.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the optional content associated with the node.
    /// </summary>
    public TContent? Content { get; }

    #endregion

    #region Public methods

    /// <summary>
    /// Determines whether this instance and another node have the same identifier.
    /// </summary>
    /// <param name="other">The other node to compare with this instance.</param>
    /// <returns><see langword="true"/> when both nodes have the same identifier; otherwise, <see langword="false"/>.</returns>
    public bool Equals(PathNode<TContent>? other)
    {
        if (other is null)
            return false;

        return this.Id == other.Id;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
            return true;

        return obj is PathNode<TContent> other && this.Equals(other);
    }

    /// <summary>
    /// Determines whether two nodes have the same identifier.
    /// </summary>
    /// <param name="left">The first node to compare.</param>
    /// <param name="right">The second node to compare.</param>
    /// <returns><see langword="true"/> when both nodes are equal; otherwise, <see langword="false"/>.</returns>
    public static bool Equals(PathNode<TContent>? left, PathNode<TContent>? right)
    {
        if (left is null || right is null)
            return left is null && right is null;

        return left.Equals(right);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return this.Id.GetHashCode();
    }

    /// <inheritdoc/>
    public static bool operator ==(PathNode<TContent>? left, PathNode<TContent>? right)
    {
        return PathNode<TContent>.Equals(left, right);
    }

    /// <inheritdoc/>
    public static bool operator !=(PathNode<TContent>? left, PathNode<TContent>? right)
    {
        return !(left == right);
    }

    #endregion
}
