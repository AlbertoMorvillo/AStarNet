using System.Diagnostics;
using AStarNet.ConsoleDemo.PathFinding;
using AStarNet.ConsoleDemo.UI;

namespace AStarNet.ConsoleDemo;

/// <summary>
/// Coordinates user input, pathfinding, and rendering for the console demo.
/// </summary>
internal sealed class ConsoleDemoApplication
{
    private const int GridWidth = 22;
    private const int GridHeight = 22;

    private readonly MatrixMap _map;
    private readonly PathFinder<GridPosition> _pathFinder;
    private readonly ConsoleRenderer _renderer;

    private GridPosition? _start;
    private GridPosition? _destination;
    private Path<GridPosition>? _path;
    private string _statusMessage;
    private ConsoleColor _statusColor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleDemoApplication"/> class.
    /// </summary>
    public ConsoleDemoApplication()
    {
        this._map = new MatrixMap(GridWidth, GridHeight);
        this._pathFinder = new PathFinder<GridPosition>(this._map, this._map);
        this._renderer = new ConsoleRenderer(this._map);
        this._statusMessage = "Choose a start and destination, then press Enter.";
        this._statusColor = ConsoleColor.Gray;

        this.FillDefaultMap();
    }

    /// <summary>
    /// Runs the interactive demo until the user presses Escape.
    /// </summary>
    public void Run()
    {
        if (!this._renderer.CanRender(out string? reason))
        {
            Console.Error.WriteLine(reason);
            return;
        }

        try
        {
            Console.CursorVisible = true;
            this._renderer.Draw(
                this._start,
                this._destination,
                this._path,
                this._statusMessage,
                this._statusColor);

            while (true)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);

                if (keyInfo.Key == ConsoleKey.Escape)
                {
                    break;
                }

                this.HandleKey(keyInfo.Key);
                this._renderer.Draw(
                    this._start,
                    this._destination,
                    this._path,
                    this._statusMessage,
                    this._statusColor);
            }
        }
        finally
        {
            Console.ResetColor();
            Console.CursorVisible = true;
            this._renderer.MoveCursorBelowDemo();
        }
    }

    /// <summary>
    /// Handles a single input key.
    /// </summary>
    /// <param name="key">The pressed key.</param>
    private void HandleKey(ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.UpArrow:
            case ConsoleKey.DownArrow:
            case ConsoleKey.LeftArrow:
            case ConsoleKey.RightArrow:
                this._renderer.MoveMarker(key);
                break;

            case ConsoleKey.S:
                this.SetStart();
                break;

            case ConsoleKey.D:
                this.SetDestination();
                break;

            case ConsoleKey.X:
            case ConsoleKey.Spacebar:
                this.ToggleWall();
                break;

            case ConsoleKey.Enter:
                this.FindPath();
                break;

            case ConsoleKey.Backspace:
                this._path = null;
                this.SetStatus("Path hidden.", ConsoleColor.Gray);
                break;

            case ConsoleKey.Delete:
                this.ClearMapAfterConfirmation();
                break;
        }
    }

    /// <summary>
    /// Sets or removes the start at the current marker position.
    /// </summary>
    private void SetStart()
    {
        GridPosition marker = this._renderer.Marker;
        this._start = this._start == marker ? null : marker;

        if (this._start.HasValue)
        {
            this._map.SetWall(marker, isWall: false);
            this.SetStatus("Start set.", ConsoleColor.Green);
        }
        else
        {
            this.SetStatus("Start removed.", ConsoleColor.Gray);
        }

        this.InvalidatePath();
    }

    /// <summary>
    /// Sets or removes the destination at the current marker position.
    /// </summary>
    private void SetDestination()
    {
        GridPosition marker = this._renderer.Marker;
        this._destination = this._destination == marker ? null : marker;

        if (this._destination.HasValue)
        {
            this._map.SetWall(marker, isWall: false);
            this.SetStatus("Destination set.", ConsoleColor.Red);
        }
        else
        {
            this.SetStatus("Destination removed.", ConsoleColor.Gray);
        }

        this.InvalidatePath();
    }

    /// <summary>
    /// Toggles the wall at the current marker position.
    /// </summary>
    private void ToggleWall()
    {
        GridPosition marker = this._renderer.Marker;

        if (this._start == marker || this._destination == marker)
        {
            this.SetStatus("Start and destination cannot be walls.", ConsoleColor.Yellow);
            return;
        }

        bool isWall = !this._map.IsWall(marker);
        this._map.SetWall(marker, isWall);
        this.InvalidatePath();
        this.SetStatus(isWall ? "Wall added." : "Wall removed.", ConsoleColor.Gray);
    }

    /// <summary>
    /// Finds and displays a path between the selected endpoints.
    /// </summary>
    private void FindPath()
    {
        if (!this._start.HasValue || !this._destination.HasValue)
        {
            this.SetStatus("Select both a start and a destination.", ConsoleColor.Yellow);
            return;
        }

        int startId = this._map.GetNodeId(this._start.Value);
        int destinationId = this._map.GetNodeId(this._destination.Value);
        long startTimestamp = Stopwatch.GetTimestamp();

        this._path = this._pathFinder.FindPath(startId, destinationId);

        TimeSpan elapsed = Stopwatch.GetElapsedTime(startTimestamp);

        if (this._path.Count == 0)
        {
            this.SetStatus(
                $"No path found ({elapsed.TotalMilliseconds:0.###} ms).",
                ConsoleColor.Yellow);
            return;
        }

        this.SetStatus(
            $"Path found: {this._path.Count} nodes, cost {this._path.Cost:0.###} " +
            $"({elapsed.TotalMilliseconds:0.###} ms).",
            ConsoleColor.Cyan);
    }

    /// <summary>
    /// Clears the grid and selections after an explicit confirmation.
    /// </summary>
    private void ClearMapAfterConfirmation()
    {
        this.SetStatus("Clear walls, endpoints, and path? Press Y to confirm.", ConsoleColor.Yellow);
        this._renderer.Draw(
            this._start,
            this._destination,
            this._path,
            this._statusMessage,
            this._statusColor);

        ConsoleKeyInfo confirmation = Console.ReadKey(intercept: true);

        if (confirmation.Key != ConsoleKey.Y)
        {
            this.SetStatus("Clear cancelled.", ConsoleColor.Gray);
            return;
        }

        this._map.ClearWalls();
        this._start = null;
        this._destination = null;
        this._path = null;
        this.SetStatus("Map cleared.", ConsoleColor.Gray);
    }

    /// <summary>
    /// Removes a path that no longer represents the current map or endpoints.
    /// </summary>
    private void InvalidatePath()
    {
        this._path = null;
    }

    /// <summary>
    /// Updates the status displayed beside the grid.
    /// </summary>
    /// <param name="message">The status message.</param>
    /// <param name="color">The status color.</param>
    private void SetStatus(string message, ConsoleColor color)
    {
        this._statusMessage = message;
        this._statusColor = color;
    }

    /// <summary>
    /// Adds the initial walls used to make the demo immediately interesting.
    /// </summary>
    private void FillDefaultMap()
    {
        for (int y = 3; y < 18; y++)
        {
            if (y != 10)
            {
                this._map.SetWall(new GridPosition(7, y), isWall: true);
            }
        }

        for (int x = 7; x < 18; x++)
        {
            if (x != 13)
            {
                this._map.SetWall(new GridPosition(x, 15), isWall: true);
            }
        }
    }
}
