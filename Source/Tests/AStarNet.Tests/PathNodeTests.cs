namespace AStarNet.Tests;

/// <summary>
/// Tests node construction and identifier-based identity.
/// </summary>
public sealed class PathNodeTests
{
    /// <summary>
    /// Verifies that content is optional.
    /// </summary>
    [Fact]
    public void Constructor_WhenContentIsOmitted_StoresNullContent()
    {
        PathNode<string> node = new(7);

        Assert.Null(node.Content);
    }

    /// <summary>
    /// Verifies that equal identifiers define equal nodes independently of content.
    /// </summary>
    [Fact]
    public void Equality_WhenIdentifiersMatch_IgnoresContent()
    {
        PathNode<string> left = new(7, "left");
        PathNode<string> right = new(7, "right");

        Assert.Equal(left, right);
        Assert.True(left == right);
        Assert.False(left != right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    /// <summary>
    /// Verifies that different identifiers produce different nodes.
    /// </summary>
    [Fact]
    public void Equality_WhenIdentifiersDiffer_ReturnsFalse()
    {
        PathNode<string> left = new(7, "same");
        PathNode<string> right = new(8, "same");

        Assert.NotEqual(left, right);
        Assert.False(left == right);
        Assert.True(left != right);
    }

    /// <summary>
    /// Verifies null semantics of the equality operators.
    /// </summary>
    [Fact]
    public void EqualityOperators_WhenOperandsAreNull_FollowReferenceNullSemantics()
    {
        PathNode<string>? left = null;
        PathNode<string>? right = null;
        PathNode<string> node = new(7);

        Assert.True(left == right);
        Assert.False(left != right);
        Assert.False(left == node);
        Assert.True(left != node);
    }
}
