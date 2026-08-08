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
        Path path = Path.Empty;

        Assert.True(path.IsEmpty);
        Assert.Empty(path.Steps);
        Assert.Equal(0, path.Cost);
        Assert.Null(path.StartNodeId);
        Assert.Null(path.EndNodeId);
    }

    /// <summary>
    /// Verifies that a created path contains every calculated step cost.
    /// </summary>
    [Fact]
    public void CreatedPath_ContainsCalculatedStepAndTotalCosts()
    {
        Path path = TestPathFactory.Create(0, (1, 1.25), (2, 2.75));

        Assert.Equal([0, 1, 2], path.Steps.Select(step => step.NodeId));
        Assert.Equal([0, 1.25, 2.75], path.Steps.Select(step => step.CostFromPrevious));
        Assert.Equal([0, 1.25, 4], path.Steps.Select(step => step.CostFromStart));
        Assert.Equal(4, path.Cost);
        Assert.Equal(path.Steps[0].NodeId, path.StartNodeId);
        Assert.Equal(path.Steps[^1].NodeId, path.EndNodeId);
    }

    /// <summary>
    /// Verifies that paths with equal steps have equal value semantics.
    /// </summary>
    [Fact]
    public void Equality_WhenStepsMatch_ReturnsTrueAndProducesSameHashCode()
    {
        Path left = TestPathFactory.Create(0, (1, 1), (2, 2));
        Path right = TestPathFactory.Create(0, (1, 1), (2, 2));

        Assert.Equal(left, right);
        Assert.True(left == right);
        Assert.False(left != right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
        Assert.True(Path.Equals(left, right));
    }

    /// <summary>
    /// Verifies that step costs remain part of path identity even when nodes and total cost match.
    /// </summary>
    [Fact]
    public void Equality_WhenIntermediateStepCostsDiffer_ReturnsFalse()
    {
        Path left = TestPathFactory.Create(0, (1, 1), (2, 2));
        Path right = TestPathFactory.Create(0, (1, 2), (2, 1));

        Assert.Equal(left.Cost, right.Cost);
        Assert.Equal(left.Steps.Select(step => step.NodeId), right.Steps.Select(step => step.NodeId));
        Assert.NotEqual(left, right);
        Assert.True(left != right);
    }

    /// <summary>
    /// Verifies the null semantics of path equality.
    /// </summary>
    [Fact]
    public void Equality_WhenOperandsAreNull_FollowsReferenceNullSemantics()
    {
        Path? left = null;
        Path? right = null;
        Path path = Path.Empty;

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
        Path first = TestPathFactory.Create(0, (1, 1), (2, 2));
        Path second = TestPathFactory.Create(2, (3, 4));

        Path combined = first.Concat(second);

        Assert.Equal([0, 1, 2, 3], combined.Steps.Select(step => step.NodeId));
        Assert.Equal([0, 1, 3, 7], combined.Steps.Select(step => step.CostFromStart));
        Assert.Equal(7, combined.Cost);
    }

    /// <summary>
    /// Verifies that both multi-path overloads skip empty paths consistently.
    /// </summary>
    [Fact]
    public void Concat_WhenSequenceContainsEmptyPaths_SkipsThem()
    {
        Path first = TestPathFactory.Create(0, (1, 1));
        Path second = TestPathFactory.Create(1, (2, 2));

        Path fromArray = Path.Concat(Path.Empty, first, Path.Empty, second);
        IEnumerable<Path> sequence = [Path.Empty, first, second, Path.Empty];
        Path fromEnumerable = Path.Concat(sequence);

        Assert.Equal(fromArray, fromEnumerable);
        Assert.Equal([0, 1, 2], fromArray.Steps.Select(step => step.NodeId));
    }

    /// <summary>
    /// Verifies that concatenating no non-empty paths returns the shared empty path.
    /// </summary>
    [Fact]
    public void Concat_WhenNoNonEmptyPathExists_ReturnsSharedEmptyPath()
    {
        Path result = Path.Concat(Path.Empty, Path.Empty);

        Assert.Same(Path.Empty, result);
    }

    /// <summary>
    /// Verifies that concatenating no arguments returns the shared empty path.
    /// </summary>
    [Fact]
    public void Concat_WhenNoPathsAreSupplied_ReturnsSharedEmptyPath()
    {
        Path result = Path.Concat();

        Assert.Same(Path.Empty, result);
    }

    /// <summary>
    /// Verifies that disconnected paths cannot be concatenated.
    /// </summary>
    [Fact]
    public void Concat_WhenPathsAreDisconnected_Throws()
    {
        Path first = TestPathFactory.Create(0, (1, 1));
        Path second = TestPathFactory.Create(2, (3, 1));

        Assert.Throws<ArgumentException>(() => first.Concat(second));
    }

    /// <summary>
    /// Verifies that null paths are rejected by every concatenation entry point.
    /// </summary>
    [Fact]
    public void Concat_WhenAnArgumentIsNull_Throws()
    {
        Path path = TestPathFactory.Create(0, (1, 1));
        IEnumerable<Path> sequence = [path, null!];

        Assert.Throws<ArgumentNullException>(() => path.Concat(null!));
        Assert.Throws<ArgumentNullException>(() => Path.Concat((Path[]?)null!));
        Assert.Throws<ArgumentNullException>(() => Path.Concat(sequence));
    }

    /// <summary>
    /// Verifies that concatenation rejects a non-finite accumulated cost.
    /// </summary>
    [Fact]
    public void Concat_WhenAccumulatedCostOverflows_Throws()
    {
        Path first = TestPathFactory.Create(0, (1, double.MaxValue));
        Path second = TestPathFactory.Create(1, (2, double.MaxValue));

        Assert.Throws<InvalidOperationException>(() => first.Concat(second));
    }
}
