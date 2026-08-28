using AStarNet.ConsoleDemo.PathFinding;

namespace AStarNet.ConsoleDemo.UI;

/// <summary>
/// Renders the pathfinding demo directly to the console.
/// </summary>
internal sealed class ConsoleRenderer
{
    #region Constants

    private const char EmptySymbol = ' ';
    private const char StartSymbol = 'S';
    private const char DestinationSymbol = 'D';
    private const char StartAndDestinationSymbol = 'B';
    private const char PathSymbol = '*';
    private const char WallSymbol = '#';
    private const char TopLeftBorderSymbol = '+';
    private const char TopRightBorderSymbol = '+';
    private const char BottomLeftBorderSymbol = '+';
    private const char BottomRightBorderSymbol = '+';
    private const char HorizontalBorderSymbol = '-';
    private const char VerticalBorderSymbol = '|';

    private const ConsoleColor EmptyColor = ConsoleColor.Gray;
    private const ConsoleColor StartColor = ConsoleColor.Green;
    private const ConsoleColor DestinationColor = ConsoleColor.Red;
    private const ConsoleColor StartAndDestinationColor = ConsoleColor.Yellow;
    private const ConsoleColor PathColor = ConsoleColor.Cyan;
    private const ConsoleColor WallColor = ConsoleColor.DarkGray;
    private const ConsoleColor BorderColor = ConsoleColor.DarkGray;
    private const ConsoleColor MapBackgroundColor = ConsoleColor.Black;

    private const int GridLeft = 1;
    private const int GridTop = 1;
    private const int InformationGap = 3;
    private const int InformationWidth = 40;
    private const int StatisticsGap = 3;
    private const int StatisticsWidth = 36;
    private const int StatusWidth = InformationWidth + StatisticsGap + StatisticsWidth;

    #endregion

    #region Fields

    private readonly MatrixMap _map;
    private readonly HashSet<int> _pathNodeIds;
    private readonly char[,] _renderedSymbols;
    private readonly ConsoleColor[,] _renderedForegroundColors;

    private bool _gridFrameDrawn;
    private bool _informationDrawn;
    private long _renderedMapVersion = -1;
    private GridPosition? _renderedStart;
    private GridPosition? _renderedDestination;
    private Path? _renderedPath;
    private int? _renderedWallSeed;
    private bool _renderedWallLayoutModified;
    private string? _renderedStatusMessage;
    private ConsoleColor _renderedStatusColor;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleRenderer"/> class.
    /// </summary>
    /// <param name="map">The grid to render.</param>
    public ConsoleRenderer(MatrixMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        this._map = map;
        this._pathNodeIds = [];
        this._renderedSymbols = new char[map.Width, map.Height];
        this._renderedForegroundColors = new ConsoleColor[map.Width, map.Height];
        this.Marker = new GridPosition(map.Width / 2, map.Height / 2);
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the grid position selected by the marker.
    /// </summary>
    public GridPosition Marker { get; private set; }

    #endregion

    #region Public methods

    /// <summary>
    /// Determines whether the current console can display the interactive demo.
    /// </summary>
    /// <param name="reason">The reason rendering is unavailable, if any.</param>
    /// <returns><see langword="true"/> when the console can render the demo; otherwise, <see langword="false"/>.</returns>
    public bool CanRender(out string? reason)
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            reason = "AStar.net Console Demo requires an interactive terminal.";
            return false;
        }

        int requiredWidth = Math.Max(this.GetInformationLeft() + InformationWidth, this.GetStatisticsLeft() + StatisticsWidth);
        int requiredHeight = GridTop + this._map.Height + 3;

        if (Console.WindowWidth < requiredWidth || Console.WindowHeight < requiredHeight)
        {
            reason = $"The terminal must be at least {requiredWidth} columns by {requiredHeight} rows.";
            return false;
        }

        reason = null;
        return true;
    }

    /// <summary>
    /// Moves the marker one cell in the requested direction while keeping it inside the grid.
    /// </summary>
    /// <param name="key">An arrow key identifying the direction.</param>
    public void MoveMarker(ConsoleKey key)
    {
        int x = this.Marker.X;
        int y = this.Marker.Y;

        switch (key)
        {
            case ConsoleKey.UpArrow:
                y--;
                break;
            case ConsoleKey.DownArrow:
                y++;
                break;
            case ConsoleKey.LeftArrow:
                x--;
                break;
            case ConsoleKey.RightArrow:
                x++;
                break;
        }

        this.Marker = new GridPosition(
            Math.Clamp(x, 0, this._map.Width - 1),
            Math.Clamp(y, 0, this._map.Height - 1));
    }

    /// <summary>
    /// Draws the complete current state of the demo.
    /// </summary>
    /// <param name="start">The selected start, if any.</param>
    /// <param name="destination">The selected destination, if any.</param>
    /// <param name="path">The current path, if any.</param>
    /// <param name="pathfindingModes">The available pathfinding modes.</param>
    /// <param name="pathfindingResults">The latest path calculated by each mode.</param>
    /// <param name="pathfindingElapsedTimes">The latest elapsed time recorded for each mode.</param>
    /// <param name="selectedPathfindingModeIndex">The index of the currently selected mode.</param>
    /// <param name="wallSeed">The seed used to generate the current wall layout.</param>
    /// <param name="isWallLayoutModified">Whether the wall layout has been modified after generation.</param>
    /// <param name="statusMessage">The status message.</param>
    /// <param name="statusColor">The status message color.</param>
    public void Draw(
        GridPosition? start,
        GridPosition? destination,
        Path? path,
        GridPathfindingMode[] pathfindingModes,
        Path?[] pathfindingResults,
        TimeSpan[] pathfindingElapsedTimes,
        int selectedPathfindingModeIndex,
        int wallSeed,
        bool isWallLayoutModified,
        string statusMessage,
        ConsoleColor statusColor)
    {
        ConsoleColor previousForeground = Console.ForegroundColor;
        ConsoleColor previousBackground = Console.BackgroundColor;

        try
        {
            if (this.HasGridChanged(start, destination, path))
            {
                this.UpdatePathNodeIds(path);
                this.DrawGrid(start, destination);
                this._renderedMapVersion = this._map.Version;
                this._renderedStart = start;
                this._renderedDestination = destination;
                this._renderedPath = path;
            }

            this.DrawInformation(
                wallSeed,
                isWallLayoutModified,
                statusMessage,
                statusColor);

            this.DrawStatistics(
                pathfindingModes,
                pathfindingResults,
                pathfindingElapsedTimes,
                selectedPathfindingModeIndex);

            this.PositionCursorAtMarker();
        }
        finally
        {
            Console.ForegroundColor = previousForeground;
            Console.BackgroundColor = previousBackground;
        }
    }

    /// <summary>
    /// Moves the cursor below the rendered area before the application exits.
    /// </summary>
    public void MoveCursorBelowDemo()
    {
        int bottom = Math.Min(GridTop + this._map.Height + 2, Console.BufferHeight - 1);
        Console.SetCursorPosition(0, bottom);
    }

    #endregion

    #region Private methods - Grid rendering

    /// <summary>
    /// Draws the grid border and its cells.
    /// </summary>
    /// <param name="start">The selected start, if any.</param>
    /// <param name="destination">The selected destination, if any.</param>
    private void DrawGrid(
        GridPosition? start,
        GridPosition? destination)
    {
        if (!this._gridFrameDrawn)
        {
            Console.ForegroundColor = BorderColor;
            WriteAt(
                GridLeft,
                GridTop,
                $"{TopLeftBorderSymbol}{new string(HorizontalBorderSymbol, this._map.Width)}{TopRightBorderSymbol}");

            for (int y = 0; y < this._map.Height; y++)
            {
                WriteAt(GridLeft, GridTop + y + 1, VerticalBorderSymbol.ToString());
                WriteAt(
                    GridLeft + this._map.Width + 1,
                    GridTop + y + 1,
                    VerticalBorderSymbol.ToString());
            }

            WriteAt(
                GridLeft,
                GridTop + this._map.Height + 1,
                $"{BottomLeftBorderSymbol}{new string(HorizontalBorderSymbol, this._map.Width)}{BottomRightBorderSymbol}");

            this._gridFrameDrawn = true;
        }

        for (int y = 0; y < this._map.Height; y++)
        {
            for (int x = 0; x < this._map.Width; x++)
            {
                GridPosition position = new(x, y);
                this.DrawCell(position, start, destination);
            }
        }
    }

    /// <summary>
    /// Draws one grid cell using the appropriate semantic color.
    /// </summary>
    /// <param name="position">The cell position.</param>
    /// <param name="start">The selected start, if any.</param>
    /// <param name="destination">The selected destination, if any.</param>
    private void DrawCell(
        GridPosition position,
        GridPosition? start,
        GridPosition? destination)
    {
        char symbol = EmptySymbol;
        ConsoleColor foreground = EmptyColor;

        if (start == position && destination == position)
        {
            symbol = StartAndDestinationSymbol;
            foreground = StartAndDestinationColor;
        }
        else if (start == position)
        {
            symbol = StartSymbol;
            foreground = StartColor;
        }
        else if (destination == position)
        {
            symbol = DestinationSymbol;
            foreground = DestinationColor;
        }
        else if (this._map.IsWall(position))
        {
            symbol = WallSymbol;
            foreground = WallColor;
        }
        else if (this._pathNodeIds.Contains(this._map.GetNodeId(position)))
        {
            symbol = PathSymbol;
            foreground = PathColor;
        }

        if (this._renderedSymbols[position.X, position.Y] == symbol &&
            this._renderedForegroundColors[position.X, position.Y] == foreground)
        {
            return;
        }

        Console.ForegroundColor = foreground;
        Console.BackgroundColor = MapBackgroundColor;
        WriteAt(GridLeft + position.X + 1, GridTop + position.Y + 1, symbol.ToString());

        this._renderedSymbols[position.X, position.Y] = symbol;
        this._renderedForegroundColors[position.X, position.Y] = foreground;
    }

    /// <summary>
    /// Determines whether any state represented by grid cells has changed.
    /// </summary>
    /// <param name="start">The selected start, if any.</param>
    /// <param name="destination">The selected destination, if any.</param>
    /// <param name="path">The current path, if any.</param>
    /// <returns><see langword="true"/> when the grid must be redrawn; otherwise, <see langword="false"/>.</returns>
    private bool HasGridChanged(GridPosition? start, GridPosition? destination, Path? path)
    {
        return !this._gridFrameDrawn ||
            this._renderedMapVersion != this._map.Version ||
            this._renderedStart != start ||
            this._renderedDestination != destination ||
            !ReferenceEquals(this._renderedPath, path);
    }

    /// <summary>
    /// Rebuilds the path-node lookup used while drawing cells.
    /// </summary>
    /// <param name="path">The current path, if any.</param>
    private void UpdatePathNodeIds(Path? path)
    {
        if (ReferenceEquals(this._renderedPath, path))
            return;

        this._pathNodeIds.Clear();

        if (path is null)
            return;

        foreach (PathStep step in path.Steps)
        {
            this._pathNodeIds.Add(step.NodeId);
        }
    }

    #endregion

    #region Private methods - Information rendering

    /// <summary>
    /// Draws the title, controls, legend, and current status.
    /// </summary>
    /// <param name="wallSeed">The seed used to generate the current wall layout.</param>
    /// <param name="isWallLayoutModified">Whether the wall layout has been modified after generation.</param>
    /// <param name="statusMessage">The status message.</param>
    /// <param name="statusColor">The status color.</param>
    private void DrawInformation(
        int wallSeed,
        bool isWallLayoutModified,
        string statusMessage,
        ConsoleColor statusColor)
    {
        int left = this.GetInformationLeft();

        if (!this._informationDrawn)
        {
            WriteLineAt(left, GridTop, "AStar.net Console Demo", ConsoleColor.Cyan);

            WriteLineAt(left, GridTop + 3, "[ Controls ]", ConsoleColor.White);
            WriteLineAt(left, GridTop + 4, "Arrow keys    Move marker", ConsoleColor.Gray);
            WriteLineAt(left, GridTop + 5, "S             Set/remove start", ConsoleColor.Gray);
            WriteLineAt(left, GridTop + 6, "D             Set/remove destination", ConsoleColor.Gray);
            WriteLineAt(left, GridTop + 7, "X or Space    Add/remove wall", ConsoleColor.Gray);
            WriteLineAt(left, GridTop + 8, "H             Select mode", ConsoleColor.Gray);
            WriteLineAt(left, GridTop + 9, "Enter         Find paths and statistics", ConsoleColor.Gray);
            WriteLineAt(left, GridTop + 10, "Backspace     Hide path", ConsoleColor.Gray);
            WriteLineAt(left, GridTop + 11, "R             Generate random walls", ConsoleColor.Gray);
            WriteLineAt(left, GridTop + 12, "Delete        Clear everything", ConsoleColor.Gray);
            WriteLineAt(left, GridTop + 13, "Escape        Exit", ConsoleColor.Gray);

            WriteLineAt(left, GridTop + 15, "[ Legend ]", ConsoleColor.White);
            WriteLineAt(left, GridTop + 16, $"{StartSymbol}  Start", StartColor);
            WriteLineAt(left, GridTop + 17, $"{DestinationSymbol}  Destination", DestinationColor);
            WriteLineAt(left, GridTop + 18, $"{StartAndDestinationSymbol}  Start and destination", StartAndDestinationColor);
            WriteLineAt(left, GridTop + 19, $"{PathSymbol}  Path", PathColor);
            WriteLineAt(left, GridTop + 20, $"{WallSymbol}  Wall", WallColor);

            WriteLineAt(left, GridTop + 22, "[ Status ]", ConsoleColor.White);

            this._informationDrawn = true;
        }

        if (this._renderedWallSeed != wallSeed ||
            this._renderedWallLayoutModified != isWallLayoutModified)
        {
            string modificationMarker = isWallLayoutModified ? "*" : string.Empty;
            WriteLineAt(left, GridTop + 1, $"Wall seed: {wallSeed}{modificationMarker}", ConsoleColor.DarkGray);
            this._renderedWallSeed = wallSeed;
            this._renderedWallLayoutModified = isWallLayoutModified;
        }

        if (this._renderedStatusMessage == statusMessage &&
            this._renderedStatusColor == statusColor)
        {
            return;
        }

        WriteLineAt(left, GridTop + 23, statusMessage, statusColor, StatusWidth);
        this._renderedStatusMessage = statusMessage;
        this._renderedStatusColor = statusColor;
    }

    /// <summary>
    /// Draws the latest execution statistics for every available pathfinding mode.
    /// </summary>
    /// <param name="pathfindingModes">The available pathfinding modes.</param>
    /// <param name="pathfindingResults">The latest path calculated by each mode.</param>
    /// <param name="pathfindingElapsedTimes">The latest elapsed time recorded for each mode.</param>
    /// <param name="selectedPathfindingModeIndex">The index of the currently selected mode.</param>
    private void DrawStatistics(
        GridPathfindingMode[] pathfindingModes,
        Path?[] pathfindingResults,
        TimeSpan[] pathfindingElapsedTimes,
        int selectedPathfindingModeIndex)
    {
        int left = this.GetStatisticsLeft();

        WriteLineAt(
            left,
            GridTop + 3,
            "[ Last run: cost / time ]",
            ConsoleColor.White,
            StatisticsWidth);

        for (int index = 0; index < pathfindingModes.Length; index++)
        {
            GridPathfindingMode mode = pathfindingModes[index];
            Path? result = pathfindingResults[index];
            string selectionMarker = index == selectedPathfindingModeIndex ? ">" : " ";
            string displayName = mode == GridPathfindingMode.Manhattan
                ? $"{mode.GetDisplayName()} (!)"
                : mode.GetDisplayName();
            string cost = result switch
            {
                null => "--",
                { IsEmpty: true } => "none",
                _ => $"{result.Cost:0.###}"
            };
            string elapsed = result is null
                ? "--"
                : $"{pathfindingElapsedTimes[index].TotalMilliseconds:0.###}ms";

            WriteLineAt(
                left,
                GridTop + 4 + index,
                $"{selectionMarker}{displayName,-15} {cost,8} {elapsed,10}",
                index == selectedPathfindingModeIndex ? ConsoleColor.Cyan : ConsoleColor.Gray,
                StatisticsWidth);
        }

        WriteLineAt(
            left,
            GridTop + 5 + pathfindingModes.Length,
            "(!) May be non-optimal",
            ConsoleColor.DarkGray,
            StatisticsWidth);

    }

    #endregion

    #region Private methods - Console layout

    /// <summary>
    /// Positions the native console cursor on the selected grid cell.
    /// </summary>
    private void PositionCursorAtMarker()
    {
        Console.SetCursorPosition(
            GridLeft + this.Marker.X + 1,
            GridTop + this.Marker.Y + 1);
    }

    /// <summary>
    /// Writes a padded line at a fixed location so previous content is fully overwritten.
    /// </summary>
    /// <param name="left">The zero-based column.</param>
    /// <param name="top">The zero-based row.</param>
    /// <param name="text">The text to write.</param>
    /// <param name="color">The optional foreground color.</param>
    /// <param name="width">The number of columns to overwrite.</param>
    private static void WriteLineAt(
        int left,
        int top,
        string text,
        ConsoleColor? color = null,
        int width = InformationWidth)
    {
        if (color.HasValue)
        {
            Console.ForegroundColor = color.Value;
        }

        string output = text.Length > width
            ? text[..width]
            : text.PadRight(width);

        WriteAt(left, top, output);
    }

    /// <summary>
    /// Writes text at a fixed console location.
    /// </summary>
    /// <param name="left">The zero-based column.</param>
    /// <param name="top">The zero-based row.</param>
    /// <param name="text">The text to write.</param>
    private static void WriteAt(int left, int top, string text)
    {
        Console.SetCursorPosition(left, top);
        Console.Write(text);
    }

    /// <summary>
    /// Gets the leftmost console column reserved for controls and status information.
    /// </summary>
    /// <returns>The zero-based column where the information panel begins.</returns>
    private int GetInformationLeft()
    {
        return GridLeft + this._map.Width + 2 + InformationGap;
    }

    /// <summary>
    /// Gets the leftmost console column reserved for pathfinding statistics.
    /// </summary>
    /// <returns>The zero-based column where the statistics panel begins.</returns>
    private int GetStatisticsLeft()
    {
        return this.GetInformationLeft() + InformationWidth + StatisticsGap;
    }

    #endregion
}
