using AStarNet.ConsoleDemo.PathFinding;

namespace AStarNet.ConsoleDemo.UI;

/// <summary>
/// Renders the pathfinding demo directly to the console.
/// </summary>
internal sealed class ConsoleRenderer
{
    private const char EmptySymbol = ' ';
    private const char StartSymbol = 'S';
    private const char DestinationSymbol = 'D';
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
    private const ConsoleColor PathColor = ConsoleColor.Cyan;
    private const ConsoleColor WallColor = ConsoleColor.DarkGray;
    private const ConsoleColor BorderColor = ConsoleColor.DarkGray;
    private const ConsoleColor MapBackgroundColor = ConsoleColor.Black;

    private const int GridLeft = 1;
    private const int GridTop = 1;
    private const int InformationGap = 4;
    private const int InformationWidth = 50;

    private readonly MatrixMap _map;
    private readonly char[,] _renderedSymbols;
    private readonly ConsoleColor[,] _renderedForegroundColors;

    private bool _gridFrameDrawn;
    private bool _informationDrawn;
    private string? _renderedStatusMessage;
    private ConsoleColor _renderedStatusColor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleRenderer"/> class.
    /// </summary>
    /// <param name="map">The grid to render.</param>
    public ConsoleRenderer(MatrixMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        this._map = map;
        this._renderedSymbols = new char[map.Width, map.Height];
        this._renderedForegroundColors = new ConsoleColor[map.Width, map.Height];
        this.Marker = new GridPosition(map.Width / 2, map.Height / 2);
    }

    /// <summary>
    /// Gets the grid position selected by the marker.
    /// </summary>
    public GridPosition Marker { get; private set; }

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

        int requiredWidth = this.GetInformationLeft() + InformationWidth;
        int requiredHeight = GridTop + this._map.Height + 3;

        if (Console.BufferWidth < requiredWidth || Console.BufferHeight < requiredHeight)
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
    /// <param name="statusMessage">The status message.</param>
    /// <param name="statusColor">The status message color.</param>
    public void Draw(
        GridPosition? start,
        GridPosition? destination,
        Path<GridPosition>? path,
        string statusMessage,
        ConsoleColor statusColor)
    {
        HashSet<int> pathNodeIds = [];

        if (path is not null)
        {
            foreach (PathStep<GridPosition> step in path.Steps)
            {
                pathNodeIds.Add(step.Node.Id);
            }
        }

        ConsoleColor previousForeground = Console.ForegroundColor;
        ConsoleColor previousBackground = Console.BackgroundColor;

        try
        {
            this.DrawGrid(start, destination, pathNodeIds);
            this.DrawInformation(statusMessage, statusColor);
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

    /// <summary>
    /// Draws the grid border and its cells.
    /// </summary>
    /// <param name="start">The selected start, if any.</param>
    /// <param name="destination">The selected destination, if any.</param>
    /// <param name="pathNodeIds">The node identifiers contained in the current path.</param>
    private void DrawGrid(
        GridPosition? start,
        GridPosition? destination,
        HashSet<int> pathNodeIds)
    {
        if (!this._gridFrameDrawn)
        {
            Console.ForegroundColor = BorderColor;
            this.WriteAt(
                GridLeft,
                GridTop,
                $"{TopLeftBorderSymbol}{new string(HorizontalBorderSymbol, this._map.Width)}{TopRightBorderSymbol}");

            for (int y = 0; y < this._map.Height; y++)
            {
                this.WriteAt(GridLeft, GridTop + y + 1, VerticalBorderSymbol.ToString());
                this.WriteAt(
                    GridLeft + this._map.Width + 1,
                    GridTop + y + 1,
                    VerticalBorderSymbol.ToString());
            }

            this.WriteAt(
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
                this.DrawCell(position, start, destination, pathNodeIds);
            }
        }
    }

    /// <summary>
    /// Draws one grid cell using the appropriate semantic color.
    /// </summary>
    /// <param name="position">The cell position.</param>
    /// <param name="start">The selected start, if any.</param>
    /// <param name="destination">The selected destination, if any.</param>
    /// <param name="pathNodeIds">The node identifiers contained in the current path.</param>
    private void DrawCell(
        GridPosition position,
        GridPosition? start,
        GridPosition? destination,
        HashSet<int> pathNodeIds)
    {
        char symbol = EmptySymbol;
        ConsoleColor foreground = EmptyColor;

        if (start == position)
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
        else if (pathNodeIds.Contains(this._map.GetNodeId(position)))
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
        this.WriteAt(GridLeft + position.X + 1, GridTop + position.Y + 1, symbol.ToString());

        this._renderedSymbols[position.X, position.Y] = symbol;
        this._renderedForegroundColors[position.X, position.Y] = foreground;
    }

    /// <summary>
    /// Draws the title, controls, legend, and current status.
    /// </summary>
    /// <param name="statusMessage">The status message.</param>
    /// <param name="statusColor">The status color.</param>
    private void DrawInformation(string statusMessage, ConsoleColor statusColor)
    {
        int left = this.GetInformationLeft();

        if (!this._informationDrawn)
        {
            this.WriteLineAt(left, GridTop, "AStar.net Console Demo", ConsoleColor.Cyan);
            this.WriteLineAt(left, GridTop + 2, "Controls", ConsoleColor.White);
            this.WriteLineAt(left, GridTop + 3, "Arrow keys    Move marker", ConsoleColor.Gray);
            this.WriteLineAt(left, GridTop + 4, "S             Set/remove start", ConsoleColor.Gray);
            this.WriteLineAt(left, GridTop + 5, "D             Set/remove destination", ConsoleColor.Gray);
            this.WriteLineAt(left, GridTop + 6, "X or Space    Add/remove wall", ConsoleColor.Gray);
            this.WriteLineAt(left, GridTop + 7, "Enter         Find path", ConsoleColor.Gray);
            this.WriteLineAt(left, GridTop + 8, "Backspace     Hide path", ConsoleColor.Gray);
            this.WriteLineAt(left, GridTop + 9, "Delete        Clear everything", ConsoleColor.Gray);
            this.WriteLineAt(left, GridTop + 10, "Escape        Exit", ConsoleColor.Gray);

            this.WriteLineAt(left, GridTop + 12, "Legend", ConsoleColor.White);
            this.WriteLineAt(left, GridTop + 13, $"{StartSymbol}  Start", StartColor);
            this.WriteLineAt(left, GridTop + 14, $"{DestinationSymbol}  Destination", DestinationColor);
            this.WriteLineAt(left, GridTop + 15, $"{PathSymbol}  Path", PathColor);
            this.WriteLineAt(left, GridTop + 16, $"{WallSymbol}  Wall", WallColor);
            this.WriteLineAt(left, GridTop + 18, "Status", ConsoleColor.White);

            this._informationDrawn = true;
        }

        if (this._renderedStatusMessage == statusMessage &&
            this._renderedStatusColor == statusColor)
        {
            return;
        }

        this.WriteLineAt(left, GridTop + 19, statusMessage, statusColor);
        this._renderedStatusMessage = statusMessage;
        this._renderedStatusColor = statusColor;
    }

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
    private void WriteLineAt(
        int left,
        int top,
        string text,
        ConsoleColor? color = null)
    {
        if (color.HasValue)
        {
            Console.ForegroundColor = color.Value;
        }

        string output = text.Length > InformationWidth
            ? text[..InformationWidth]
            : text.PadRight(InformationWidth);

        this.WriteAt(left, top, output);
    }

    /// <summary>
    /// Writes text at a fixed console location.
    /// </summary>
    /// <param name="left">The zero-based column.</param>
    /// <param name="top">The zero-based row.</param>
    /// <param name="text">The text to write.</param>
    private void WriteAt(int left, int top, string text)
    {
        Console.SetCursorPosition(left, top);
        Console.Write(text);
    }

    /// <summary>
    /// Gets the first column available for explanatory text.
    /// </summary>
    /// <returns>The zero-based information column.</returns>
    private int GetInformationLeft()
    {
        return GridLeft + this._map.Width + 2 + InformationGap;
    }
}
