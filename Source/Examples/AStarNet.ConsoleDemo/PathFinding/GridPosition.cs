namespace AStarNet.ConsoleDemo.PathFinding;

/// <summary>
/// Identifies a cell in the demonstration grid.
/// </summary>
internal readonly record struct GridPosition
{
    /// <summary>
    /// Initializes a grid position.
    /// </summary>
    /// <param name="x">The zero-based horizontal coordinate.</param>
    /// <param name="y">The zero-based vertical coordinate.</param>
    public GridPosition(int x, int y)
    {
        this.X = x;
        this.Y = y;
    }

    /// <summary>
    /// Gets the zero-based horizontal coordinate.
    /// </summary>
    public int X { get; }

    /// <summary>
    /// Gets the zero-based vertical coordinate.
    /// </summary>
    public int Y { get; }
}
