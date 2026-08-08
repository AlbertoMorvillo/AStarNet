using System.Diagnostics;
using System.Text;
using AStarNet.ConsoleDemo.PathFinding;
using AStarNet.ConsoleDemo.PathFinding.Heuristics;
using AStarNet.ConsoleDemo.PathFinding.TieBreakers;
using AStarNet.ConsoleDemo.UI;
using AStarNet.ConsoleDemo.WorldGeneration;

namespace AStarNet.ConsoleDemo;

/// <summary>
/// Coordinates user input, pathfinding, and rendering for the console demo.
/// </summary>
internal sealed class ConsoleDemoApplication
{
    #region Constants

    private const int GridWidth = 22;
    private const int GridHeight = 22;
    private const int DefaultWallSeed = 2026;

    #endregion

    #region Fields

    private readonly MatrixMap _map;
    // Parallel arrays keep the pathfinding setup and displayed measurements explicit in this demo.
    private readonly PathFinder[] _pathFinders;
    private readonly GridPathfindingMode[] _pathfindingModes;
    private readonly Path?[] _pathfindingResults;
    private readonly TimeSpan[] _pathfindingElapsedTimes;
    private readonly ConsoleRenderer _renderer;

    private GridPosition? _start;
    private GridPosition? _destination;
    private Path? _path;
    private int _selectedPathfindingModeIndex;
    private int _wallSeed;
    private bool _isWallLayoutModified;
    private string _statusMessage;
    private ConsoleColor _statusColor;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleDemoApplication"/> class.
    /// </summary>
    public ConsoleDemoApplication()
    {
        (MatrixMap map, PathFinder[] pathFinders, GridPathfindingMode[] modes) =
            ConsoleDemoApplication.CreatePathfinding();

        this._map = map;
        this._pathFinders = pathFinders;
        this._pathfindingModes = modes;
        this._pathfindingResults = new Path?[this._pathfindingModes.Length];
        this._pathfindingElapsedTimes = new TimeSpan[this._pathfindingModes.Length];
        this._renderer = new ConsoleRenderer(this._map);
        this._wallSeed = DefaultWallSeed;
        this._statusMessage = "Choose a start and destination, then press Enter.";
        this._statusColor = ConsoleColor.Gray;

        RandomWallLayoutGenerator.Generate(this._map, DefaultWallSeed);
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the currently selected pathfinding mode.
    /// </summary>
    private GridPathfindingMode SelectedPathfindingMode =>
        this._pathfindingModes[this._selectedPathfindingModeIndex];

    #endregion

    #region Public methods

    /// <summary>
    /// Runs the interactive demo until the user presses Escape.
    /// </summary>
    public void Run()
    {
        bool isConsoleInitialized = false;

        try
        {
            if (!this.TryEnsureCanRender())
                return;

            Console.CursorVisible = true;
            isConsoleInitialized = true;
            this.DrawCurrentState();

            while (true)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);

                if (keyInfo.Key == ConsoleKey.Escape)
                {
                    break;
                }

                this.HandleKey(keyInfo.Key);

                if (!this.TryDraw())
                    return;
            }
        }
        finally
        {
            if (isConsoleInitialized)
            {
                Console.ResetColor();
                Console.CursorVisible = true;
                this._renderer.MoveCursorBelowDemo();
            }
        }
    }

    #endregion

    #region Private methods - Input

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
                this.CalculatePaths();
                break;

            case ConsoleKey.H:
                this.SelectNextPathfindingMode();
                break;

            case ConsoleKey.Backspace:
                this._path = null;
                this.SetStatus("Path hidden.", ConsoleColor.Gray);
                break;

            case ConsoleKey.R:
                this.GenerateRandomMap();
                break;

            case ConsoleKey.Delete:
                this.ClearMapAfterConfirmation();
                break;
        }
    }

    #endregion

    #region Private methods - Map editing

    /// <summary>
    /// Sets or removes the start at the current marker position.
    /// </summary>
    private void SetStart()
    {
        GridPosition marker = this._renderer.Marker;
        this._start = this._start == marker ? null : marker;

        if (this._start.HasValue)
        {
            this._isWallLayoutModified |= this._map.IsWall(marker);
            this._map.SetWall(marker, isWall: false);
            this.SetStatus("Start set.", ConsoleColor.Green);
        }
        else
        {
            this.SetStatus("Start removed.", ConsoleColor.Gray);
        }

        this.InvalidateSearchResults();
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
            this._isWallLayoutModified |= this._map.IsWall(marker);
            this._map.SetWall(marker, isWall: false);
            this.SetStatus("Destination set.", ConsoleColor.Red);
        }
        else
        {
            this.SetStatus("Destination removed.", ConsoleColor.Gray);
        }

        this.InvalidateSearchResults();
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
        this._isWallLayoutModified = true;
        this.InvalidateSearchResults();
        this.SetStatus(isWall ? "Wall added." : "Wall removed.", ConsoleColor.Gray);
    }

    #endregion

    #region Private methods - Pathfinding

    /// <summary>
    /// Creates the demonstration map and one pathfinder for each pathfinding mode being compared.
    /// </summary>
    /// <returns>The map, pathfinders, and modes used by the demo.</returns>
    private static (MatrixMap Map, PathFinder[] PathFinders, GridPathfindingMode[] Modes) CreatePathfinding()
    {
        MatrixMap map = new(GridWidth, GridHeight);
        GridHeuristic octileHeuristic = new(map, GridHeuristicKind.Octile);
        GridHeuristic euclideanHeuristic = new(map, GridHeuristicKind.Euclidean);
        GridHeuristic manhattanHeuristic = new(map, GridHeuristicKind.Manhattan);
        LineDeviationTieBreaker lineDeviationTieBreaker = new(map);

        // Each PathFinder is permanently associated with its map and optional providers.
        PathFinder[] pathFinders =
        [
            new PathFinder(map),
            new PathFinder(map, tieBreakerProvider: lineDeviationTieBreaker),
            new PathFinder(map, octileHeuristic),
            new PathFinder(map, octileHeuristic, lineDeviationTieBreaker),
            new PathFinder(map, euclideanHeuristic),
            new PathFinder(map, euclideanHeuristic, lineDeviationTieBreaker),
            new PathFinder(map, manhattanHeuristic)
        ];
        GridPathfindingMode[] modes =
        [
            GridPathfindingMode.Dijkstra,
            GridPathfindingMode.DijkstraWithLineTieBreaker,
            GridPathfindingMode.Octile,
            GridPathfindingMode.OctileWithLineTieBreaker,
            GridPathfindingMode.Euclidean,
            GridPathfindingMode.EuclideanWithLineTieBreaker,
            GridPathfindingMode.Manhattan
        ];

        return (map, pathFinders, modes);
    }

    /// <summary>
    /// Calculates a path for every mode and displays the result of the selected mode.
    /// </summary>
    private void CalculatePaths()
    {
        if (!this._start.HasValue || !this._destination.HasValue)
        {
            this.SetStatus("Select both a start and a destination.", ConsoleColor.Yellow);
            return;
        }

        int startId = this._map.GetNodeId(this._start.Value);
        int destinationId = this._map.GetNodeId(this._destination.Value);

        for (int index = 0; index < this._pathfindingModes.Length; index++)
        {
            long startTimestamp = Stopwatch.GetTimestamp();
            Path path = this._pathFinders[index].FindPath(
                startNodeId: startId,
                destinationNodeId: destinationId);

            TimeSpan elapsed = Stopwatch.GetElapsedTime(startTimestamp);

            this._pathfindingResults[index] = path;
            this._pathfindingElapsedTimes[index] = elapsed;
        }

        this.ShowSelectedPathfindingResult();
    }

    /// <summary>
    /// Selects the next pathfinding mode and displays its recorded result when available.
    /// </summary>
    private void SelectNextPathfindingMode()
    {
        this._selectedPathfindingModeIndex =
            (this._selectedPathfindingModeIndex + 1) % this._pathfindingModes.Length;

        if (this._pathfindingResults[this._selectedPathfindingModeIndex] is not null)
        {
            this.ShowSelectedPathfindingResult();
            return;
        }

        this._path = null;
        this.SetStatus(
            $"Mode selected: {this.SelectedPathfindingMode.GetDisplayName()}. Press Enter to calculate.",
            ConsoleColor.Gray);
    }

    /// <summary>
    /// Displays the path and timing recorded for the currently selected pathfinding mode.
    /// </summary>
    private void ShowSelectedPathfindingResult()
    {
        this._path = this._pathfindingResults[this._selectedPathfindingModeIndex]
            ?? throw new InvalidOperationException("The selected pathfinding mode has not been executed.");
        TimeSpan elapsed = this._pathfindingElapsedTimes[this._selectedPathfindingModeIndex];
        string modeName = this.SelectedPathfindingMode.GetDisplayName();

        if (this._path.IsEmpty)
        {
            this.SetStatus(
                $"Last generated: {modeName} | no path | {elapsed.TotalMilliseconds:0.###} ms",
                ConsoleColor.Yellow);
            return;
        }

        this.SetStatus(
            $"Last generated: {modeName} | {this._path.Steps.Length} nodes | " +
            $"cost {this._path.Cost:0.###} | {elapsed.TotalMilliseconds:0.###} ms",
            ConsoleColor.Cyan);
    }

    #endregion

    #region Private methods - World generation

    /// <summary>
    /// Requests a seed and replaces the current map with a generated wall layout.
    /// </summary>
    private void GenerateRandomMap()
    {
        int? seed = this.ReadSeed();

        if (!seed.HasValue)
            return;

        this._map.ClearWalls();
        this._start = null;
        this._destination = null;
        RandomWallLayoutGenerator.Generate(this._map, seed.Value);
        this.InvalidateSearchResults();
        this._wallSeed = seed.Value;
        this._isWallLayoutModified = false;
        this.SetStatus($"Map generated with seed {seed.Value}.", ConsoleColor.Cyan);
    }

    /// <summary>
    /// Reads a numeric seed, or generates one after two consecutive empty confirmations.
    /// </summary>
    /// <returns>The selected seed, or <see langword="null"/> when input is cancelled.</returns>
    private int? ReadSeed()
    {
        StringBuilder input = new();
        bool awaitingRandomConfirmation = false;

        this.SetStatus("Seed: integer; Enter twice = random; Esc = cancel.", ConsoleColor.Yellow);
        if (!this.TryDraw())
            return null;

        while (true)
        {
            ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);

            if (keyInfo.Key == ConsoleKey.Escape)
            {
                this.SetStatus("Map generation cancelled.", ConsoleColor.Gray);
                return null;
            }

            if (keyInfo.Key == ConsoleKey.Enter)
            {
                if (input.Length > 0)
                {
                    if (int.TryParse(input.ToString(), out int seed))
                        return seed;

                    this.SetStatus("Seed must be a 32-bit integer.", ConsoleColor.Yellow);
                    if (!this.TryDraw())
                        return null;

                    continue;
                }

                if (awaitingRandomConfirmation)
                    return Random.Shared.Next();

                awaitingRandomConfirmation = true;
            }
            else if (keyInfo.Key == ConsoleKey.Backspace)
            {
                if (input.Length > 0)
                    input.Length--;

                awaitingRandomConfirmation = false;
            }
            else if (input.Length < 11 &&
                (char.IsDigit(keyInfo.KeyChar) || (keyInfo.KeyChar == '-' && input.Length == 0)))
            {
                input.Append(keyInfo.KeyChar);
                awaitingRandomConfirmation = false;
            }
            else
            {
                continue;
            }

            string message = awaitingRandomConfirmation
                ? "Empty seed: press Enter again for random."
                : $"Seed: {input}_";

            this.SetStatus(message, ConsoleColor.Yellow);
            if (!this.TryDraw())
                return null;
        }
    }

    /// <summary>
    /// Clears the grid and selections after an explicit confirmation.
    /// </summary>
    private void ClearMapAfterConfirmation()
    {
        this.SetStatus("Clear map? Press Y to confirm.", ConsoleColor.Yellow);
        if (!this.TryDraw())
            return;

        ConsoleKeyInfo confirmation = Console.ReadKey(intercept: true);

        if (confirmation.Key != ConsoleKey.Y)
        {
            this.SetStatus("Clear cancelled.", ConsoleColor.Gray);
            return;
        }

        this._map.ClearWalls();
        this._start = null;
        this._destination = null;
        this.InvalidateSearchResults();
        this._isWallLayoutModified = true;
        this.SetStatus("Map cleared.", ConsoleColor.Gray);
    }

    #endregion

    #region Private methods - Rendering and state

    /// <summary>
    /// Removes paths and statistics that no longer represent the current map or endpoints.
    /// </summary>
    private void InvalidateSearchResults()
    {
        this._path = null;

        Array.Clear(this._pathfindingResults);
        Array.Clear(this._pathfindingElapsedTimes);
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
    /// Draws the current state when the console has sufficient space.
    /// </summary>
    /// <returns><see langword="true"/> when the state was rendered; otherwise, <see langword="false"/>.</returns>
    private bool TryDraw()
    {
        if (!this.TryEnsureCanRender())
            return false;

        this.DrawCurrentState();
        return true;
    }

    /// <summary>
    /// Draws the current application state after console availability has been verified.
    /// </summary>
    private void DrawCurrentState()
    {
        this._renderer.Draw(
            this._start,
            this._destination,
            this._path,
            this._pathfindingModes,
            this._pathfindingResults,
            this._pathfindingElapsedTimes,
            this._selectedPathfindingModeIndex,
            this._wallSeed,
            this._isWallLayoutModified,
            this._statusMessage,
            this._statusColor);
    }

    /// <summary>
    /// Verifies that console operations required by the demo are available.
    /// </summary>
    /// <returns><see langword="true"/> when rendering can continue; otherwise, <see langword="false"/>.</returns>
    private bool TryEnsureCanRender()
    {
        if (this._renderer.CanRender(out string? reason))
            return true;

        if (!Console.IsOutputRedirected)
            Console.Clear();

        Console.Error.WriteLine(reason);
        return false;
    }

    #endregion
}
