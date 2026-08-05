// Copyright (c) 2026 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace AStarNet;

/// <summary>
/// Contains an immutable sequence of path steps ordered from start to destination.
/// </summary>
/// <typeparam name="TContent">The type of the optional node content.</typeparam>
public class Path<TContent> : IComparable, IComparable<Path<TContent>>, IEquatable<Path<TContent>>
{
    #region Fields

    private readonly int _precomputedHashCode;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes an empty path with the specified identifier.
    /// </summary>
    /// <param name="id">The path identifier.</param>
    public Path(Guid id)
    {
        this.Id = id;
        this.Steps = ImmutableArray<PathStep<TContent>>.Empty;
        this.Cost = 0;
        this._precomputedHashCode = this.GenerateHashCode();
    }

    /// <summary>
    /// Initializes a path from a start node and its outgoing connections.
    /// </summary>
    /// <param name="id">The path identifier.</param>
    /// <param name="startNode">The first node in the path.</param>
    /// <param name="connections">The ordered connections traversed after the start node.</param>
    /// <exception cref="ArgumentNullException"><paramref name="startNode"/> or <paramref name="connections"/> is <see langword="null"/>.</exception>
    internal Path(
        Guid id,
        PathNode<TContent> startNode,
        IEnumerable<PathConnection<TContent>> connections)
    {
        ArgumentNullException.ThrowIfNull(startNode);
        ArgumentNullException.ThrowIfNull(connections);

        ImmutableArray<PathStep<TContent>>.Builder stepBuilder = ImmutableArray.CreateBuilder<PathStep<TContent>>();
        double costFromStart = 0;

        stepBuilder.Add(new PathStep<TContent>(startNode, 0, costFromStart));

        foreach (PathConnection<TContent> connection in connections)
        {
            ArgumentNullException.ThrowIfNull(connection.Destination);

            costFromStart += connection.Cost;
            stepBuilder.Add(new PathStep<TContent>(connection.Destination, connection.Cost, costFromStart));
        }

        this.Id = id;
        this.Steps = stepBuilder.ToImmutable();
        this.Cost = costFromStart;
        this._precomputedHashCode = this.GenerateHashCode();
    }

    /// <summary>
    /// Initializes a path from an ordered sequence of steps.
    /// </summary>
    /// <param name="id">The path identifier.</param>
    /// <param name="steps">The ordered path steps.</param>
    /// <exception cref="ArgumentNullException"><paramref name="steps"/> is <see langword="null"/>.</exception>
    private Path(Guid id, IEnumerable<PathStep<TContent>> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);

        ImmutableArray<PathStep<TContent>>.Builder stepBuilder = ImmutableArray.CreateBuilder<PathStep<TContent>>();
        double costFromStart = 0;

        foreach (PathStep<TContent> step in steps)
        {
            double costFromPrevious = stepBuilder.Count == 0 ? 0 : step.CostFromPrevious;
            costFromStart += costFromPrevious;
            stepBuilder.Add(new PathStep<TContent>(step.Node, costFromPrevious, costFromStart));
        }

        this.Id = id;
        this.Steps = stepBuilder.ToImmutable();
        this.Cost = costFromStart;
        this._precomputedHashCode = this.GenerateHashCode();
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the identifier of this path.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the ordered path steps.
    /// </summary>
    public IReadOnlyList<PathStep<TContent>> Steps { get; }

    /// <summary>
    /// Gets the total traversal cost.
    /// </summary>
    public double Cost { get; }

    /// <summary>
    /// Gets or sets an optional application-defined tag.
    /// </summary>
    public object? Tag { get; set; }

    /// <summary>
    /// Gets a value indicating whether the path contains no steps.
    /// </summary>
    public bool IsEmpty => this.Steps.Count == 0;

    /// <summary>
    /// Gets the number of steps in the path.
    /// </summary>
    public int Count => this.Steps.Count;

    /// <summary>
    /// Gets the node at the specified path index.
    /// </summary>
    /// <param name="index">The zero-based path index.</param>
    /// <returns>The node at <paramref name="index"/>.</returns>
    public PathNode<TContent> this[int index] => this.Steps[index].Node;

    /// <summary>
    /// Gets the shared empty path.
    /// </summary>
    public static Path<TContent> Empty { get; } = new(Guid.Empty);

    #endregion

    #region Public methods

    /// <summary>
    /// Gets the accumulated cost at the specified path index.
    /// </summary>
    /// <param name="index">The zero-based path index.</param>
    /// <returns>The accumulated cost from the start through the specified step.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the path.</exception>
    public double GetCostAtIndex(int index)
    {
        if (index < 0 || index >= this.Steps.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");

        return this.Steps[index].CostFromStart;
    }

    /// <summary>
    /// Creates a path by appending another connected path.
    /// </summary>
    /// <param name="other">The path to append.</param>
    /// <returns>The concatenated path.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The destination of this path does not match the start of <paramref name="other"/>.</exception>
    public Path<TContent> Concat(Path<TContent> other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return Path<TContent>.Concat([this, other]);
    }

    /// <summary>
    /// Concatenates two connected paths.
    /// </summary>
    /// <param name="path1">The first path.</param>
    /// <param name="path2">The path to append.</param>
    /// <returns>The concatenated path.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path1"/> or <paramref name="path2"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The paths are not connected.</exception>
    public static Path<TContent> Concat(Path<TContent> path1, Path<TContent> path2)
    {
        ArgumentNullException.ThrowIfNull(path1);
        ArgumentNullException.ThrowIfNull(path2);

        return Path<TContent>.Concat([path1, path2]);
    }

    /// <summary>
    /// Concatenates multiple connected paths.
    /// </summary>
    /// <param name="paths">The paths to concatenate.</param>
    /// <returns>The concatenated path.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="paths"/> is <see langword="null"/> or contains a null path.</exception>
    /// <exception cref="ArgumentException">Two consecutive paths are not connected.</exception>
    public static Path<TContent> Concat(params Path<TContent>[] paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        return Path<TContent>.Concat((IEnumerable<Path<TContent>>)paths);
    }

    /// <summary>
    /// Concatenates a sequence of connected paths.
    /// </summary>
    /// <param name="paths">The paths to concatenate.</param>
    /// <returns>The concatenated path, or an empty path when the sequence has no non-empty paths.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="paths"/> is <see langword="null"/> or contains a null path.</exception>
    /// <exception cref="ArgumentException">Two consecutive paths are not connected.</exception>
    public static Path<TContent> Concat(IEnumerable<Path<TContent>> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        List<PathStep<TContent>> combinedSteps = [];
        PathNode<TContent>? previousDestination = null;

        foreach (Path<TContent> path in paths)
        {
            ArgumentNullException.ThrowIfNull(path);

            if (path.IsEmpty)
                continue;

            if (previousDestination is not null && previousDestination != path[0])
                throw new ArgumentException("Consecutive paths must share their boundary node.", nameof(paths));

            int startIndex = combinedSteps.Count == 0 ? 0 : 1;
            for (int i = startIndex; i < path.Steps.Count; i++)
            {
                combinedSteps.Add(path.Steps[i]);
            }

            previousDestination = path[^1];
        }

        return combinedSteps.Count == 0
            ? Path<TContent>.Empty
            : new Path<TContent>(Guid.NewGuid(), combinedSteps);
    }

    /// <summary>
    /// Compares this path with another path by total cost and then node count.
    /// </summary>
    /// <param name="other">The other path.</param>
    /// <returns>A value indicating the relative ordering of the paths.</returns>
    public int CompareTo(Path<TContent>? other)
    {
        if (other is null)
            return 1;

        int costComparison = this.Cost.CompareTo(other.Cost);
        return costComparison != 0
            ? costComparison
            : this.Count.CompareTo(other.Count);
    }

    /// <inheritdoc/>
    public int CompareTo(object? obj)
    {
        if (obj is null)
            return 1;

        if (obj is not Path<TContent> other)
            throw new ArgumentException($"Object must be of type {nameof(Path<TContent>)}.", nameof(obj));

        return this.CompareTo(other);
    }

    /// <summary>
    /// Compares two paths by total cost and then node count.
    /// </summary>
    /// <param name="left">The first path.</param>
    /// <param name="right">The second path.</param>
    /// <returns>A value indicating the relative ordering of the paths.</returns>
    public static int Compare(Path<TContent>? left, Path<TContent>? right)
    {
        if (left is null)
            return right is null ? 0 : -1;

        return right is null ? 1 : left.CompareTo(right);
    }

    /// <summary>
    /// Determines whether this path has the same total cost and node sequence as another path.
    /// </summary>
    /// <param name="other">The other path.</param>
    /// <returns><see langword="true"/> when the paths are equal; otherwise, <see langword="false"/>.</returns>
    public bool Equals(Path<TContent>? other)
    {
        if (other is null || this.Cost != other.Cost)
            return false;

        if (this.Count != other.Count)
            return false;

        for (int i = 0; i < this.Count; i++)
        {
            if (this[i] != other[i])
                return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj)
            || obj is Path<TContent> other && this.Equals(other);
    }

    /// <summary>
    /// Determines whether two paths are equal.
    /// </summary>
    /// <param name="left">The first path.</param>
    /// <param name="right">The second path.</param>
    /// <returns><see langword="true"/> when the paths are equal; otherwise, <see langword="false"/>.</returns>
    public static bool Equals(Path<TContent>? left, Path<TContent>? right)
    {
        if (left is null || right is null)
            return left is null && right is null;

        return left.Equals(right);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return this._precomputedHashCode;
    }

    /// <inheritdoc/>
    public static bool operator ==(Path<TContent>? left, Path<TContent>? right)
    {
        return Path<TContent>.Equals(left, right);
    }

    /// <inheritdoc/>
    public static bool operator !=(Path<TContent>? left, Path<TContent>? right)
    {
        return !(left == right);
    }

    /// <inheritdoc/>
    public static bool operator <(Path<TContent>? left, Path<TContent>? right)
    {
        return left is null ? right is not null : left.CompareTo(right) < 0;
    }

    /// <inheritdoc/>
    public static bool operator <=(Path<TContent>? left, Path<TContent>? right)
    {
        return left is null || left.CompareTo(right) <= 0;
    }

    /// <inheritdoc/>
    public static bool operator >(Path<TContent>? left, Path<TContent>? right)
    {
        return left is not null && left.CompareTo(right) > 0;
    }

    /// <inheritdoc/>
    public static bool operator >=(Path<TContent>? left, Path<TContent>? right)
    {
        return left is null ? right is null : left.CompareTo(right) >= 0;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Generates the immutable hash code for this path.
    /// </summary>
    /// <returns>The generated hash code.</returns>
    private int GenerateHashCode()
    {
        HashCode hash = new();
        hash.Add(this.Cost);

        foreach (PathStep<TContent> step in this.Steps)
        {
            hash.Add(step.Node.Id);
        }

        return hash.ToHashCode();
    }

    #endregion
}
