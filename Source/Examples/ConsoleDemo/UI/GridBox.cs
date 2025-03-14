// Copyright (c) 2025 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using AStarNet;
using ConsoleDemo.PathFinding;
using System.Numerics;
using System.Text;

namespace ConsoleDemo.UI
{
    /// <summary>
    /// Represents a grid box for displaying a matrix-based map in the console.
    /// It handles drawing borders, content, and a movable marker.
    /// </summary>
    public class GridBox
    {
        #region Properties

        /// <summary>
        /// Gets the matrix map that defines the layout of the grid.
        /// </summary>
        public MatrixMap MatrixMap { get; }

        /// <summary>
        /// Gets or sets the left position (column offset) of the grid box in the console.
        /// </summary>
        public int Left { get; set; } = 0;

        /// <summary>
        /// Gets or sets the top position (row offset) of the grid box in the console.
        /// </summary>
        public int Top { get; set; } = 0;

        /// <summary>
        /// Gets the current horizontal position of the marker within the grid.
        /// </summary>
        public int MarkerX { get; private set; }

        /// <summary>
        /// Gets the current vertical position of the marker within the grid.
        /// </summary>
        public int MarkerY { get; private set; }

        /// <summary>
        /// Gets or sets the background color used when drawing the grid.
        /// </summary>
        public ConsoleColor? BackgroundColor { get; set; }

        /// <summary>
        /// Gets or sets the border color used when drawing the grid.
        /// </summary>
        public ConsoleColor? BorderColor { get; set; }

        /// <summary>
        /// Gets or sets the color used to represent the start point in the grid.
        /// </summary>
        public ConsoleColor? StartPointColor { get; set; }

        /// <summary>
        /// Gets or sets the color used to represent the destination point in the grid.
        /// </summary>
        public ConsoleColor? DestinationPointColor { get; set; }

        /// <summary>
        /// Gets or sets the color used to represent wall cells in the grid.
        /// </summary>
        public ConsoleColor? WallPointColor { get; set; }

        /// <summary>
        /// Gets or sets the color used to represent cells that are part of the path in the grid.
        /// </summary>
        public ConsoleColor? PathPointColor { get; set; }

        /// <summary>
        /// Gets or sets the color used to represent empty cells in the grid.
        /// </summary>
        public ConsoleColor? EmptyPointColor { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GridBox"/> class with the specified matrix map.
        /// </summary>
        /// <param name="matrixMap">The matrix map that defines the grid layout.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="matrixMap"/> is <see langword="null"/>.</exception>
        public GridBox(MatrixMap matrixMap)
        {
            ArgumentNullException.ThrowIfNull(matrixMap);

            this.MatrixMap = matrixMap;
            this.MarkerX = this.MatrixMap.Width / 2;
            this.MarkerY = this.MatrixMap.Height / 2;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Draws the grid box, including borders, cell content, and the marker.
        /// </summary>
        /// <param name="startPoint">The starting point coordinates (if any) to be highlighted.</param>
        /// <param name="destinationPoint">The destination point coordinates (if any) to be highlighted.</param>
        /// <param name="path">The path to be drawn on the grid, if provided.</param>
        public void Draw(Vector2? startPoint, Vector2? destinationPoint, Path<Vector2, PathNode<Vector2>>? path)
        {
            Console.CursorVisible = false;

            Encoding previousEncode = Console.OutputEncoding;
            Console.OutputEncoding = Encoding.Unicode;

            this.DrawBorder();
            this.DrawContent(startPoint, destinationPoint, path);
            this.DrawMarker(startPoint, destinationPoint, path);

            Console.OutputEncoding = previousEncode;
        }

        /// <summary>
        /// Draws the content of the grid cells within the area defined by the matrix map.
        /// </summary>
        /// <param name="startPoint">The starting point to highlight, if provided.</param>
        /// <param name="destinationPoint">The destination point to highlight, if provided.</param>
        /// <param name="path">The path to be drawn on the grid, if provided.</param>
        public void UpdateContent(Vector2? startPoint, Vector2? destinationPoint, Path<Vector2, PathNode<Vector2>>? path)
        {
            Encoding previousEncode = Console.OutputEncoding;
            Console.OutputEncoding = Encoding.Unicode;

            this.DrawContent(startPoint, destinationPoint, path);

            Console.OutputEncoding = previousEncode;
        }

        /// <summary>
        /// Updates the marker position based on a pressed key and redraws it.
        /// </summary>
        /// <param name="pressedKey">The key pressed by the user.</param>
        /// <param name="startPoint">The starting point coordinates (if any) to be highlighted.</param>
        /// <param name="destinationPoint">The destination point coordinates (if any) to be highlighted.</param>
        /// <param name="path">The path to be drawn on the grid, if provided.</param>
        public void UpdateCursor(ConsoleKey pressedKey, Vector2? startPoint, Vector2? destinationPoint, Path<Vector2, PathNode<Vector2>>? path)
        {
            int newMarkerX = this.MarkerX;
            int newMarkerY = this.MarkerY;

            // Check the key pressed.
            switch (pressedKey)
            {
                // Arrow key: move the cursor.
                case ConsoleKey.UpArrow:
                    newMarkerY--;
                    break;
                case ConsoleKey.DownArrow:
                    newMarkerY++;
                    break;
                case ConsoleKey.LeftArrow:
                    newMarkerX--;
                    break;
                case ConsoleKey.RightArrow:
                    newMarkerX++;
                    break;
            }

            Encoding previousEncode = Console.OutputEncoding;
            Console.OutputEncoding = Encoding.Unicode;

            this.ClearMarker(startPoint, destinationPoint, path);

            this.MarkerX = int.Clamp(newMarkerX, 0, this.MatrixMap.Width - 1);
            this.MarkerY = int.Clamp(newMarkerY, 0, this.MatrixMap.Height - 1);

            this.DrawMarker(startPoint, destinationPoint, path);

            Console.OutputEncoding = previousEncode;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Draws the border of the grid box on the console.
        /// </summary>
        private void DrawBorder()
        {
            ConsoleColor previousBackground = Console.BackgroundColor;
            ConsoleColor previousForeground = Console.ForegroundColor;

            this.ChangeConsoleColor(this.BorderColor, false);

            // Draw top border.
            Console.SetCursorPosition(this.Left, this.Top);
            Console.Write("╔" + new string('═', this.MatrixMap.Width) + "╗");

            // Draw bottom border.
            Console.SetCursorPosition(this.Left, this.Top + this.MatrixMap.Height + 1);
            Console.Write("╚" + new string('═', this.MatrixMap.Width) + "╝");

            // Draw left and right borders.
            for (int i = 1; i <= this.MatrixMap.Height; i++)
            {
                Console.SetCursorPosition(this.Left, this.Top + i);
                Console.Write("║");
                Console.SetCursorPosition(this.Left + this.MatrixMap.Width + 1, this.Top + i);
                Console.Write("║");
            }

            Console.SetCursorPosition(0, 0);
            Console.BackgroundColor = previousBackground;
            Console.ForegroundColor = previousForeground;
        }

        /// <summary>
        /// Draws the content of the grid cells within the area defined by the matrix map.
        /// </summary>
        /// <param name="startPoint">The starting point to highlight, if provided.</param>
        /// <param name="destinationPoint">The destination point to highlight, if provided.</param>
        /// <param name="path">The path to be drawn on the grid, if provided.</param>
        public void DrawContent(Vector2? startPoint, Vector2? destinationPoint, Path<Vector2, PathNode<Vector2>>? path)
        {
            for (int i = 0; i < this.MatrixMap.Width; i++)
            {
                for (int j = 0; j < this.MatrixMap.Height; j++)
                {
                    Console.SetCursorPosition(this.Left + i + 1, this.Top + j + 1);

                    this.DrawSymbol(i, j, startPoint, destinationPoint, path, false);
                }
            }

            Console.SetCursorPosition(0, 0);
        }

        /// <summary>
        /// Draws the marker at the current marker position.
        /// </summary>
        /// <param name="startPoint">The starting point to highlight, if provided.</param>
        /// <param name="destinationPoint">The destination point to highlight, if provided.</param>
        /// <param name="path">The path to be drawn on the grid, if provided.</param>
        private void DrawMarker(Vector2? startPoint, Vector2? destinationPoint, Path<Vector2, PathNode<Vector2>>? path)
        {
            Console.SetCursorPosition(this.Left + this.MarkerX + 1, this.Top + this.MarkerY + 1);

            this.DrawSymbol(this.MarkerX, this.MarkerY, startPoint, destinationPoint, path, true);

            Console.SetCursorPosition(0, 0);
        }

        /// <summary>
        /// Clears the marker from its current position by redrawing the cell content without highlighting.
        /// </summary>
        /// <param name="startPoint">The starting point to highlight, if provided.</param>
        /// <param name="destinationPoint">The destination point to highlight, if provided.</param>
        /// <param name="path">The path to be drawn on the grid, if provided.</param>
        private void ClearMarker(Vector2? startPoint, Vector2? destinationPoint, Path<Vector2, PathNode<Vector2>>? path)
        {
            Console.SetCursorPosition(this.Left + this.MarkerX + 1, this.Top + this.MarkerY + 1);

            this.DrawSymbol(this.MarkerX, this.MarkerY, startPoint, destinationPoint, path, false);

            Console.SetCursorPosition(0, 0);
        }

        /// <summary>
        /// Draws the symbol representing a cell at the given coordinates. The symbol and colors depend on:
        /// - Whether the cell corresponds to the start point, destination point, a wall, or an empty cell.
        /// - Whether the cell is being highlighted (e.g., the marker is present).
        /// </summary>
        /// <param name="x">The cell's column index (0-based within the grid area).</param>
        /// <param name="y">The cell's row index (0-based within the grid area).</param>
        /// <param name="startPoint">The starting point to highlight, if provided.</param>
        /// <param name="destinationPoint">The destination point to highlight, if provided.</param>
        /// <param name="path">The path containing nodes to highlight, if provided.</param>
        /// <param name="highlight">A flag indicating whether to draw the cell in highlighted mode.</param>
        private void DrawSymbol(int x, int y, Vector2? startPoint, Vector2? destinationPoint, Path<Vector2, PathNode<Vector2>>? path, bool highlight)
        {
            ConsoleColor previousBackground = Console.BackgroundColor;
            ConsoleColor previousForeground = Console.ForegroundColor;

            Vector2 currentPoint = new(x, y);
            bool isEmptyPoint = true;

            if (startPoint.HasValue && currentPoint.Equals(startPoint.Value))
            {
                this.ChangeConsoleColor(this.StartPointColor, highlight);
                Console.Write("S");
                isEmptyPoint = false;
            }
            else if (destinationPoint.HasValue && currentPoint.Equals(destinationPoint.Value))
            {
                this.ChangeConsoleColor(this.DestinationPointColor, highlight);
                Console.Write("D");
                isEmptyPoint = false;
            }
            else if (this.MatrixMap.WallBlocks[x, y])
            {
                this.ChangeConsoleColor(this.WallPointColor, highlight);
                Console.Write("X");
                isEmptyPoint = false;
            }
            else if (path is not null && !path.IsEmpty)
            {
                if (currentPoint.Equals(path.Nodes[0].Id))
                {
                    this.ChangeConsoleColor(this.PathPointColor, highlight);
                    Console.Write("S");
                    isEmptyPoint = false;
                }
                else if (currentPoint.Equals(path.Nodes[^1].Id))
                {
                    this.ChangeConsoleColor(this.PathPointColor, highlight);
                    Console.Write("D");
                    isEmptyPoint = false;
                }
                else
                {
                    foreach (IPathNode<Vector2> node in path.Nodes)
                    {
                        if (currentPoint.Equals(node.Id))
                        {
                            this.ChangeConsoleColor(this.PathPointColor, highlight);
                            Console.Write("○");
                            isEmptyPoint = false;
                            break;
                        }
                    }
                }
            }

            if (isEmptyPoint)
            {
                this.ChangeConsoleColor(this.EmptyPointColor, highlight);
                Console.Write("·");
            }

            Console.BackgroundColor = previousBackground;
            Console.ForegroundColor = previousForeground;
        }

        /// <summary>
        /// Adjusts the console colors based on the provided foreground color and a highlight flag.
        /// When <paramref name="highlight"/> is <see langword="true"/>, if a foreground color is specified, it is used as the background color,
        /// and the console's foreground color is set to a contrasting color determined by the current background color.
        /// When <paramref name="highlight"/> is <see langword="false"/>, if a foreground color is provided, it sets the console's foreground color,
        /// and resets the background color to the configured background color (if available).
        /// </summary>
        /// <param name="foreground">The optional foreground color to use for formatting.</param>
        /// <param name="highlight">
        /// A flag indicating whether to apply highlight formatting. 
        /// If <see langword="true"/>, the background color is updated and a contrasting foreground color is chosen.
        /// If <see langword="false"/>, the specified foreground color is applied and the background is reset.
        /// </param>
        private void ChangeConsoleColor(ConsoleColor? foreground, bool highlight)
        {
            if (highlight)
            {
                if (foreground.HasValue)
                    Console.BackgroundColor = foreground.Value;

                Console.ForegroundColor = Console.BackgroundColor switch
                {
                    ConsoleColor.Gray or ConsoleColor.Green or ConsoleColor.Cyan or ConsoleColor.Magenta or ConsoleColor.Yellow or ConsoleColor.White => ConsoleColor.Black,
                    _ => ConsoleColor.White,
                };
            }
            else
            {
                if (foreground.HasValue)
                    Console.ForegroundColor = foreground.Value;

                if (this.BackgroundColor.HasValue)
                    Console.BackgroundColor = this.BackgroundColor.Value;
            }
        }

        #endregion
    }
}
