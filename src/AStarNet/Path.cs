using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace AStarNet;

/// <summary>
/// Contains an immutable sequence of path steps ordered from start to destination.
/// </summary>
public sealed class Path : IEquatable<Path>
{
    #region Fields

    private readonly int _precomputedHashCode;

    #endregion

    #region Constructors

    /// <summary>
    /// Stores completed steps, their total cost, and their hash.
    /// </summary>
    /// <param name="steps">The immutable steps with consistent accumulated costs.</param>
    /// <param name="cost">The total cost of the steps.</param>
    /// <param name="hashCode">The hash calculated from the steps followed by the total cost.</param>
    private Path(ImmutableArray<PathStep> steps, double cost, int hashCode)
    {
        this.Steps = steps;
        this.Cost = cost;
        this._precomputedHashCode = hashCode;
    }

    /// <summary>
    /// Creates a path from node identifiers and their incoming connection costs.
    /// </summary>
    /// <param name="steps">The steps in order from start to destination. The first cost must be zero.</param>
    /// <remarks>
    /// The sequence is consumed once and is not retained. An empty sequence creates an empty path.
    /// Keep the input stable during construction. Node identifiers and connections are not checked against a map.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="steps"/> is null.</exception>
    /// <exception cref="ArgumentException">A cost is negative or non-finite, or the first cost is not zero.</exception>
    /// <exception cref="InvalidOperationException">The accumulated cost is not finite.</exception>
    public Path(IEnumerable<(int NodeId, double CostFromPrevious)> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);

        double costFromStart = 0;
        HashCode hash = new();
        ImmutableArray<PathStep>.Builder result;

        if (steps is (int NodeId, double CostFromPrevious)[] arraySteps)
        {
            result = ImmutableArray.CreateBuilder<PathStep>(arraySteps.Length);
            foreach ((int nodeId, double costFromPrevious) in arraySteps)
            {
                Path.ValidateStepInput(costFromPrevious, result.Count == 0);
                Path.AppendStep(result, nodeId, costFromPrevious, ref costFromStart, ref hash);
            }
        }
        else if (steps.GetType() == typeof(List<(int NodeId, double CostFromPrevious)>))
        {
            List<(int NodeId, double CostFromPrevious)> listSteps =
                (List<(int NodeId, double CostFromPrevious)>)steps;
            result = ImmutableArray.CreateBuilder<PathStep>(listSteps.Count);
            foreach ((int nodeId, double costFromPrevious) in listSteps)
            {
                Path.ValidateStepInput(costFromPrevious, result.Count == 0);
                Path.AppendStep(result, nodeId, costFromPrevious, ref costFromStart, ref hash);
            }
        }
        else
        {
            result = ImmutableArray.CreateBuilder<PathStep>();
            foreach ((int nodeId, double costFromPrevious) in steps)
            {
                Path.ValidateStepInput(costFromPrevious, result.Count == 0);
                Path.AppendStep(result, nodeId, costFromPrevious, ref costFromStart, ref hash);
            }
        }

        hash.Add(costFromStart);
        this.Steps = result.DrainToImmutable();
        this.Cost = costFromStart;
        this._precomputedHashCode = hash.ToHashCode();
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the ordered path steps.
    /// </summary>
    public ImmutableArray<PathStep> Steps { get; }

    /// <summary>
    /// Gets the total traversal cost.
    /// </summary>
    public double Cost { get; }

    /// <summary>
    /// Gets a value indicating whether the path contains no steps.
    /// </summary>
    public bool IsEmpty => this.Steps.IsEmpty;

    /// <summary>
    /// Gets the first node identifier in the path, or <see langword="null"/> when the path is empty.
    /// </summary>
    public int? StartNodeId => this.IsEmpty ? null : this.Steps[0].NodeId;

    /// <summary>
    /// Gets the last node identifier in the path, or <see langword="null"/> when the path is empty.
    /// </summary>
    public int? EndNodeId => this.IsEmpty ? null : this.Steps[^1].NodeId;

    /// <summary>
    /// Gets the shared empty path.
    /// </summary>
    public static Path Empty { get; } = new(Array.Empty<(int NodeId, double CostFromPrevious)>());

    #endregion

    #region Operators

    /// <inheritdoc/>
    public static bool operator ==(Path? left, Path? right)
    {
        return Path.Equals(left, right);
    }

    /// <inheritdoc/>
    public static bool operator !=(Path? left, Path? right)
    {
        return !Path.Equals(left, right);
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Creates a path by appending another connected path.
    /// </summary>
    /// <remarks>An empty operand returns the other path without copying it.</remarks>
    /// <param name="other">The path to append.</param>
    /// <returns>The concatenated path.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The destination of this path does not match the start of <paramref name="other"/>.</exception>
    /// <exception cref="InvalidOperationException">The combined accumulated cost is not finite.</exception>
    public Path Concat(Path other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (this.IsEmpty)
            return other;

        if (other.IsEmpty)
            return this;

        if (this.EndNodeId != other.StartNodeId)
            throw new ArgumentException("Consecutive paths must share their boundary node.", nameof(other));

        int stepCount = checked(this.Steps.Length + (other.Steps.Length - 1));
        ImmutableArray<PathStep>.Builder result = ImmutableArray.CreateBuilder<PathStep>(stepCount);
        double costFromStart = 0;
        HashCode hash = new();
        int? previousEndNodeId = null;
        Path.AppendPath(result, this, ref previousEndNodeId, ref costFromStart, ref hash);
        Path.AppendPath(result, other, ref previousEndNodeId, ref costFromStart, ref hash);
        hash.Add(costFromStart);
        return new Path(result.MoveToImmutable(), costFromStart, hash.ToHashCode());
    }

    /// <summary>
    /// Concatenates multiple connected paths.
    /// </summary>
    /// <param name="paths">The paths to concatenate.</param>
    /// <returns>The concatenated path, or an empty path when no non-empty paths are supplied.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="paths"/> is <see langword="null"/> or contains a null path.</exception>
    /// <exception cref="ArgumentException">Two consecutive paths are not connected.</exception>
    /// <exception cref="InvalidOperationException">The combined accumulated cost is not finite.</exception>
    public static Path Concat(params Path[] paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        return Path.Concat((IEnumerable<Path>)paths);
    }

    /// <summary>
    /// Concatenates a sequence of connected paths.
    /// </summary>
    /// <remarks>
    /// The sequence is consumed once. If it contains only one non-empty path, that instance is returned.
    /// Connections are checked by their boundary node identifiers, not against a map.
    /// </remarks>
    /// <param name="paths">The paths to concatenate.</param>
    /// <returns>The concatenated path, or an empty path when the sequence has no non-empty paths.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="paths"/> is <see langword="null"/> or contains a null path.</exception>
    /// <exception cref="ArgumentException">Two consecutive paths are not connected.</exception>
    /// <exception cref="InvalidOperationException">The combined accumulated cost is not finite.</exception>
    public static Path Concat(IEnumerable<Path> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        ImmutableArray<PathStep>.Builder? result = null;
        Path? firstPath = null;
        double costFromStart = 0;
        HashCode hash = new();
        int? previousEndNodeId = null;

        foreach (Path path in paths)
        {
            ArgumentNullException.ThrowIfNull(path);
            if (path.IsEmpty)
                continue;

            if (firstPath is null)
            {
                firstPath = path;
                continue;
            }

            if (result is null)
            {
                if (firstPath.EndNodeId != path.StartNodeId)
                    throw new ArgumentException("Consecutive paths must share their boundary node.", nameof(paths));

                int initialCapacity = checked(firstPath.Steps.Length + (path.Steps.Length - 1));
                result = ImmutableArray.CreateBuilder<PathStep>(initialCapacity);
                Path.AppendPath(result, firstPath, ref previousEndNodeId, ref costFromStart, ref hash);
            }

            Path.AppendPath(result, path, ref previousEndNodeId, ref costFromStart, ref hash);
        }

        if (result is null)
            return firstPath ?? Path.Empty;

        hash.Add(costFromStart);
        return new Path(result.DrainToImmutable(), costFromStart, hash.ToHashCode());
    }

    /// <inheritdoc/>
    public bool Equals(Path? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        if (other is null || this._precomputedHashCode != other._precomputedHashCode)
            return false;

        if (!this.Cost.Equals(other.Cost) || this.Steps.Length != other.Steps.Length)
            return false;

        for (int index = 0; index < this.Steps.Length; index++)
        {
            if (!this.Steps[index].Equals(other.Steps[index]))
                return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is Path other && this.Equals(other);
    }

    /// <summary>
    /// Determines whether two paths are equal.
    /// </summary>
    /// <param name="left">The first path.</param>
    /// <param name="right">The second path.</param>
    /// <returns><see langword="true"/> when the paths are equal; otherwise, <see langword="false"/>.</returns>
    public static bool Equals(Path? left, Path? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        return left is not null && left.Equals(right);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return this._precomputedHashCode;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Adds a traversal cost and checks that the total is finite.
    /// </summary>
    /// <param name="accumulatedCost">The accumulated path cost.</param>
    /// <param name="additionalCost">The traversal cost to add.</param>
    /// <returns>The finite accumulated cost.</returns>
    /// <exception cref="InvalidOperationException">The accumulated cost is not finite.</exception>
    private static double AddCosts(double accumulatedCost, double additionalCost)
    {
        double result = accumulatedCost + additionalCost;

        if (!double.IsFinite(result))
            throw new InvalidOperationException("The accumulated path cost must be finite.");

        return result;
    }

    /// <summary>
    /// Checks that an incoming cost is finite, non-negative, and zero for the first step.
    /// </summary>
    /// <param name="costFromPrevious">The incoming connection cost.</param>
    /// <param name="isFirstStep">Whether this is the first step in the path.</param>
    private static void ValidateStepInput(double costFromPrevious, bool isFirstStep)
    {
        if (!double.IsFinite(costFromPrevious) || costFromPrevious < 0)
            throw new ArgumentException("Step costs must be finite and non-negative.", "steps");

        if (isFirstStep && costFromPrevious != 0)
            throw new ArgumentException("The first step must have zero incoming cost.", "steps");
    }

    /// <summary>
    /// Appends a step with a known valid incoming cost, checking the new total for overflow.
    /// </summary>
    /// <param name="result">The completed steps.</param>
    /// <param name="nodeId">The node identifier.</param>
    /// <param name="costFromPrevious">The finite, non-negative incoming cost.</param>
    /// <param name="costFromStart">The accumulated cost.</param>
    /// <param name="hash">The hash accumulator.</param>
    private static void AppendStep(
        ImmutableArray<PathStep>.Builder result,
        int nodeId,
        double costFromPrevious,
        ref double costFromStart,
        ref HashCode hash)
    {
        costFromStart = Path.AddCosts(costFromStart, costFromPrevious);
        PathStep step = new(nodeId, costFromPrevious, costFromStart);
        result.Add(step);
        hash.Add(step);
    }

    /// <summary>
    /// Appends a connected path without repeating its shared boundary node.
    /// </summary>
    /// <param name="result">The completed steps.</param>
    /// <param name="path">The next path to append.</param>
    /// <param name="previousEndNodeId">The end of the previous non-empty path.</param>
    /// <param name="costFromStart">The accumulated cost.</param>
    /// <param name="hash">The hash accumulator.</param>
    private static void AppendPath(
        ImmutableArray<PathStep>.Builder result,
        Path path,
        ref int? previousEndNodeId,
        ref double costFromStart,
        ref HashCode hash)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.IsEmpty)
            return;

        if (previousEndNodeId.HasValue && previousEndNodeId != path.StartNodeId)
            throw new ArgumentException("Consecutive paths must share their boundary node.", "paths");

        int startIndex = previousEndNodeId.HasValue ? 1 : 0;
        for (int index = startIndex; index < path.Steps.Length; index++)
        {
            PathStep step = path.Steps[index];
            Path.AppendStep(result, step.NodeId, step.CostFromPrevious, ref costFromStart, ref hash);
        }

        previousEndNodeId = path.EndNodeId;
    }

    #endregion
}
