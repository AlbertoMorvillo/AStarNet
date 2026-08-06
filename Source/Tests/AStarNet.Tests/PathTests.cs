namespace AStarNet.Tests;

/// <summary>
/// Tests immutable path behavior, equality, and concatenation.
/// </summary>
public sealed class PathTests
{
    /// <summary>
    /// Verifies the shared empty path contract.
    /// </summary>
    [Fact]
    public void Empty_ContainsNoStepsAndHasZeroCost()
    {
        Path<string> path = Path<string>.Empty;

        Assert.True(path.IsEmpty);
        Assert.Empty(path.Steps);
        Assert.Equal(0, path.Count);
        Assert.Equal(0, path.Cost);
        Assert.Null(path.Start);
        Assert.Null(path.End);
    }

    /// <summary>
    /// Verifies that construction calculates every step cost correctly.
    /// </summary>
    [Fact]
    public void Constructor_CalculatesStepAndTotalCosts()
    {
        Path<string> path = TestPathFactory.Create(0, (1, 1.25), (2, 2.75));

        Assert.Equal([0, 1, 2], path.Steps.Select(step => step.Node.Id));
        Assert.Equal([0, 1.25, 2.75], path.Steps.Select(step => step.CostFromPrevious));
        Assert.Equal([0, 1.25, 4], path.Steps.Select(step => step.CostFromStart));
        Assert.Equal(4, path.Cost);
        Assert.Equal(path.Steps[1].Node, path[1]);
        Assert.Same(path.Steps[0].Node, path.Start);
        Assert.Same(path.Steps[^1].Node, path.End);
    }

    /// <summary>
    /// Verifies that paths with equal steps have equal value semantics.
    /// </summary>
    [Fact]
    public void Equality_WhenStepsMatch_ReturnsTrueAndProducesSameHashCode()
    {
        Path<string> left = TestPathFactory.Create(0, (1, 1), (2, 2));
        Path<string> right = TestPathFactory.Create(0, (1, 1), (2, 2));

        Assert.Equal(left, right);
        Assert.True(left == right);
        Assert.False(left != right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
        Assert.True(Path<string>.Equals(left, right));
    }

    /// <summary>
    /// Verifies that step costs remain part of path identity even when nodes and total cost match.
    /// </summary>
    [Fact]
    public void Equality_WhenIntermediateStepCostsDiffer_ReturnsFalse()
    {
        Path<string> left = TestPathFactory.Create(0, (1, 1), (2, 2));
        Path<string> right = TestPathFactory.Create(0, (1, 2), (2, 1));

        Assert.Equal(left.Cost, right.Cost);
        Assert.Equal(left.Steps.Select(step => step.Node), right.Steps.Select(step => step.Node));
        Assert.NotEqual(left, right);
        Assert.True(left != right);
    }

    /// <summary>
    /// Verifies the null semantics of path equality.
    /// </summary>
    [Fact]
    public void Equality_WhenOperandsAreNull_FollowsReferenceNullSemantics()
    {
        Path<string>? left = null;
        Path<string>? right = null;
        Path<string> path = Path<string>.Empty;

        Assert.True(left == right);
        Assert.False(left != right);
        Assert.False(path == left);
        Assert.True(path != left);
    }

    /// <summary>
    /// Verifies that connected paths concatenate without duplicating their boundary node.
    /// </summary>
    [Fact]
    public void Concat_WhenPathsAreConnected_RecalculatesAccumulatedCosts()
    {
        Path<string> first = TestPathFactory.Create(0, (1, 1), (2, 2));
        Path<string> second = TestPathFactory.Create(2, (3, 4));

        Path<string> combined = first.Concat(second);

        Assert.Equal([0, 1, 2, 3], combined.Steps.Select(step => step.Node.Id));
        Assert.Equal([0, 1, 3, 7], combined.Steps.Select(step => step.CostFromStart));
        Assert.Equal(7, combined.Cost);
    }

    /// <summary>
    /// Verifies that both multi-path overloads skip empty paths consistently.
    /// </summary>
    [Fact]
    public void Concat_WhenSequenceContainsEmptyPaths_SkipsThem()
    {
        Path<string> first = TestPathFactory.Create(0, (1, 1));
        Path<string> second = TestPathFactory.Create(1, (2, 2));

        Path<string> fromArray = Path<string>.Concat(Path<string>.Empty, first, Path<string>.Empty, second);
        IEnumerable<Path<string>> sequence = [Path<string>.Empty, first, second, Path<string>.Empty];
        Path<string> fromEnumerable = Path<string>.Concat(sequence);

        Assert.Equal(fromArray, fromEnumerable);
        Assert.Equal([0, 1, 2], fromArray.Steps.Select(step => step.Node.Id));
    }

    /// <summary>
    /// Verifies that concatenating no non-empty paths returns the shared empty path.
    /// </summary>
    [Fact]
    public void Concat_WhenNoNonEmptyPathExists_ReturnsSharedEmptyPath()
    {
        Path<string> result = Path<string>.Concat(Path<string>.Empty, Path<string>.Empty);

        Assert.Same(Path<string>.Empty, result);
    }

    /// <summary>
    /// Verifies that disconnected paths cannot be concatenated.
    /// </summary>
    [Fact]
    public void Concat_WhenPathsAreDisconnected_Throws()
    {
        Path<string> first = TestPathFactory.Create(0, (1, 1));
        Path<string> second = TestPathFactory.Create(2, (3, 1));

        Assert.Throws<ArgumentException>(() => first.Concat(second));
    }

    /// <summary>
    /// Verifies that null paths are rejected by every concatenation entry point.
    /// </summary>
    [Fact]
    public void Concat_WhenAnArgumentIsNull_Throws()
    {
        Path<string> path = TestPathFactory.Create(0, (1, 1));
        IEnumerable<Path<string>> sequence = [path, null!];

        Assert.Throws<ArgumentNullException>(() => path.Concat(null!));
        Assert.Throws<ArgumentNullException>(() => Path<string>.Concat((Path<string>[]?)null!));
        Assert.Throws<ArgumentNullException>(() => Path<string>.Concat(sequence));
    }

    /// <summary>
    /// Verifies that concatenation rejects a non-finite accumulated cost.
    /// </summary>
    [Fact]
    public void Concat_WhenAccumulatedCostOverflows_Throws()
    {
        Path<string> first = TestPathFactory.Create(0, (1, double.MaxValue));
        Path<string> second = TestPathFactory.Create(1, (2, double.MaxValue));

        Assert.Throws<InvalidOperationException>(() => first.Concat(second));
    }
}
