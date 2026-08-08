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
    /// Initializes an empty path.
    /// </summary>
    private Path()
    {
        this.Steps = [];
        this.Cost = 0;
        this._precomputedHashCode = this.GenerateHashCode();
    }

    /// <summary>
    /// Initializes a path from an immutable sequence of validated steps.
    /// </summary>
    /// <param name="steps">The ordered path steps.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="steps"/> is empty, is uninitialized, or contains inconsistent costs.
    /// </exception>
    internal Path(ImmutableArray<PathStep> steps)
    {
        Path.ValidateSteps(steps);

        this.Steps = steps;
        this.Cost = steps[^1].CostFromStart;
        this._precomputedHashCode = this.GenerateHashCode();
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
    public static Path Empty { get; } = new();

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
    /// <param name="other">The path to append.</param>
    /// <returns>The concatenated path.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The destination of this path does not match the start of <paramref name="other"/>.</exception>
    /// <exception cref="InvalidOperationException">The combined accumulated cost is not finite.</exception>
    public Path Concat(Path other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return Path.Concat(this, other);
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
    /// <param name="paths">The paths to concatenate.</param>
    /// <returns>The concatenated path, or an empty path when the sequence has no non-empty paths.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="paths"/> is <see langword="null"/> or contains a null path.</exception>
    /// <exception cref="ArgumentException">Two consecutive paths are not connected.</exception>
    /// <exception cref="InvalidOperationException">The combined accumulated cost is not finite.</exception>
    public static Path Concat(IEnumerable<Path> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        ImmutableArray<PathStep>.Builder combinedSteps = ImmutableArray.CreateBuilder<PathStep>();
        int? previousEndNodeId = null;
        double costFromStart = 0;

        foreach (Path path in paths)
        {
            ArgumentNullException.ThrowIfNull(path);

            if (path.IsEmpty)
                continue;

            if (previousEndNodeId.HasValue && previousEndNodeId != path.StartNodeId)
                throw new ArgumentException("Consecutive paths must share their boundary node.", nameof(paths));

            int startIndex = combinedSteps.Count == 0 ? 0 : 1;
            for (int index = startIndex; index < path.Steps.Length; index++)
            {
                PathStep step = path.Steps[index];
                double costFromPrevious = combinedSteps.Count == 0 ? 0 : step.CostFromPrevious;
                costFromStart = Path.AddCosts(costFromStart, costFromPrevious);
                combinedSteps.Add(new PathStep(step.NodeId, costFromPrevious, costFromStart));
            }

            previousEndNodeId = path.EndNodeId;
        }

        return combinedSteps.Count == 0
            ? Path.Empty
            : new Path(combinedSteps.ToImmutable());
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
    /// Adds a traversal cost while preserving the finite path-cost invariant.
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
    /// Validates the structural and numeric invariants of a non-empty path-step sequence.
    /// </summary>
    /// <param name="steps">The steps to validate.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="steps"/> is empty, is uninitialized, or contains inconsistent costs.
    /// </exception>
    private static void ValidateSteps(ImmutableArray<PathStep> steps)
    {
        if (steps.IsDefaultOrEmpty)
            throw new ArgumentException("A non-empty path must contain at least one initialized step.", nameof(steps));

        PathStep startStep = steps[0];
        if (!startStep.CostFromPrevious.Equals(0) || !startStep.CostFromStart.Equals(0))
            throw new ArgumentException("The first path step must have zero traversal costs.", nameof(steps));

        double expectedCostFromStart = 0;

        for (int index = 1; index < steps.Length; index++)
        {
            PathStep step = steps[index];

            if (!double.IsFinite(step.CostFromPrevious) || step.CostFromPrevious < 0)
                throw new ArgumentException("Path-step costs must be finite and non-negative.", nameof(steps));

            expectedCostFromStart = Path.AddCosts(expectedCostFromStart, step.CostFromPrevious);

            if (!step.CostFromStart.Equals(expectedCostFromStart))
                throw new ArgumentException("A path step contains an inconsistent accumulated cost.", nameof(steps));
        }
    }

    /// <summary>
    /// Generates the immutable hash code for this path.
    /// </summary>
    /// <returns>The generated hash code.</returns>
    private int GenerateHashCode()
    {
        HashCode hash = new();
        hash.Add(this.Cost);

        foreach (PathStep step in this.Steps)
        {
            hash.Add(step);
        }

        return hash.ToHashCode();
    }

    #endregion
}
