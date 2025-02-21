// Copyright (c) 2025 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using AStarNet;
using ConsoleDemo.PathFinding;
using ConsoleDemo.UI;
using System.Diagnostics;
using System.Numerics;

#region Path finding

const int matrixWidth = 22;
const int matrixHeight = 22;

MatrixMap matrixMap = new(matrixWidth, matrixHeight);
PathFinder<Vector2> pathFinder = new(matrixMap, matrixMap);
Path<Vector2>? path = null;

Vector2? startPoint;
Vector2? destinationPoint;

FillDefault();

// Finds the optimal path between the start and destination points using the A* algorithm,
// and displays the result along with the elapsed time.
void FindPath()
{
    if (!startPoint.HasValue)
    {
        ShowMessage("<red>ERROR:</red> <yellow>Start point</yellow> <red>not placed.</red>");
        return;
    }

    if (!destinationPoint.HasValue)
    {
        ShowMessage("<red>ERROR:</red> <green>Destination point</green> <red>not placed.</red>");
        return;
    }

    Stopwatch stopwatch = new();

    stopwatch.Start();
    path = pathFinder.FindOptimalPath(startPoint.Value, destinationPoint.Value);
    stopwatch.Stop();

    if (path is null || path.IsEmpty)
    {
        ShowMessage("<cyan>RESULT:</cyan> <gray>No path found.</gray>");
        return;
    }

    ShowMessage("<cyan>RESULT:</cyan> <green>Path found.</green> <gray>(" + stopwatch.ElapsedMilliseconds + " ms)</gray>");
}

#endregion

#region Info box

TextBox infoBox = new() { Left = matrixWidth + 10, Top = 3, Width = 50, Height = 20 };

infoBox.BeginWrite();
infoBox.WriteLine("<cyan>AStar.net Console Demo</cyan>");
infoBox.WriteLine();
infoBox.WriteLine("Use <darkcyan>← ↑ → ↓</darkcyan> to move the cursor");
infoBox.WriteLine();
infoBox.WriteLine("<yellow>S:</yellow> place or remove the starting point.");
infoBox.WriteLine("<green>D:</green> place or remove the destination point.");
infoBox.WriteLine("<red>X or Space:</red> place or remove a wall block.");
infoBox.WriteLine();
infoBox.WriteLine("<cyan>Enter:</cyan> start the path finding.");
infoBox.WriteLine();
infoBox.WriteLine("<darkyellow>Backspace:</darkyellow> clear the path.");
infoBox.WriteLine("<red>Clear:</red> clear everything.");
infoBox.WriteLine();
infoBox.WriteLine("<blue>Esc:</blue> exit.");
infoBox.EndWrite();

#endregion

#region Message box

TextBox messageBox = new() { Left = matrixWidth + 10, Top = 22, Width = 50, Height = 1 };

// Displays a message in the message box.
void ShowMessage(string message)
{
    messageBox.BeginWrite();
    messageBox.WriteLine(message);
    messageBox.EndWrite();
}

// Prompts the user for a confirmation, displaying a message and waiting for Y/N input.
bool PromptConfirmation()
{
    messageBox.BeginWrite();
    messageBox.WriteLine("<darkyellow>Are you sure? (Y/N)</darkyellow>");
    messageBox.EndWrite();

    do
    {
        ConsoleKeyInfo keyInfo = Console.ReadKey(true);

        if (keyInfo.Key == ConsoleKey.Y)
        {
            messageBox.Clear();
            return true;
        }
        else if (keyInfo.Key == ConsoleKey.N)
        {
            messageBox.Clear();
            return false;
        }
    }
    while (true);
}

#endregion

#region Grid box

GridBox gridBox = new(matrixMap)
{
    Left = 5,
    Top = 2,
    BorderColor = ConsoleColor.DarkCyan,
    StartPointColor = ConsoleColor.Yellow,
    DestinationPointColor = ConsoleColor.Green,
    WallPointColor = ConsoleColor.Red,
    EmptyPointColor = ConsoleColor.DarkGray
};

gridBox.Draw(startPoint, destinationPoint, path);

#endregion

#region Input and refresh loop

// Main input and refresh loop:
// This loop continuously listens for user key input and processes commands as follows:
// - X or Spacebar: Toggle a wall block at the current cursor position.
// - S: Set, change, or remove the starting point.
// - D: Set, change, or remove the destination point.
// - Enter: Run the pathfinding algorithm.
// - Backspace: Clear the current path.
// - Delete: Clear the entire map (start point, destination point, wall blocks, and path).
// - Escape: Exit the application.
// After handling each key input, the grid cursor is updated accordingly.

while (true)
{
    ConsoleKeyInfo keyInfo = Console.ReadKey(true);

    switch (keyInfo.Key)
    {
        // Space: place or remove a wall block.
        case ConsoleKey.X:
        case ConsoleKey.Spacebar:
            ToggleWallBlock(gridBox.MarkerX, gridBox.MarkerY);
            break;

        // S: place, change or remove starting point.
        case ConsoleKey.S:
            UpdatePoint(ref startPoint, gridBox.MarkerX, gridBox.MarkerY);
            gridBox.UpdateContent(startPoint, destinationPoint, path);
            break;

        // D: place, change or remove destination point.
        case ConsoleKey.D:
            UpdatePoint(ref destinationPoint, gridBox.MarkerX, gridBox.MarkerY);
            gridBox.UpdateContent(startPoint, destinationPoint, path);
            break;

        // Enter: start path finding.
        case ConsoleKey.Enter:
            FindPath();
            gridBox.UpdateContent(startPoint, destinationPoint, path);
            break;

        // Backspace: clear the path.
        case ConsoleKey.Backspace:
            path = null;
            gridBox.UpdateContent(startPoint, destinationPoint, path);
            break;

        // Delete: clear everything (start point, destination point, wall blocks, path)
        case ConsoleKey.Delete:
            Clear();
            gridBox.UpdateContent(startPoint, destinationPoint, path);
            break;

        // Close the console.
        case ConsoleKey.Escape:
            return;
    }

    gridBox.UpdateCursor(keyInfo.Key, startPoint, destinationPoint, path);
}

#endregion

#region Data update

// Fills the map with a default configuration inspired by the Wikipedia example of the A* search algorithm.
void FillDefault()
{
    // Recreating the Wikipedia example:
    // https://en.wikipedia.org/wiki/A*_search_algorithm
    // https://en.wikipedia.org/wiki/A*_search_algorithm#/media/File:Astar_progress_animation.gif

    startPoint = new Vector2(2, 19);
    destinationPoint = new Vector2(18, 3);

    // Horizontal wall parth
    for (int i = 5; i <= 15; i++)
    {
        for (int j = 6; j <= 8; j++)
        {
            matrixMap.WallBlocks[i, j] = true;
        }
    }

    // Vertical wall parth
    for (int i = 13; i <= 15; i++)
    {
        for (int j = 8; j <= 13; j++)
        {
            matrixMap.WallBlocks[i, j] = true;
        }
    }
}

// Toggles the wall block state at the specified coordinates in the matrix map.
void ToggleWallBlock(int x, int y)
{
    matrixMap.WallBlocks[x, y] = !matrixMap.WallBlocks[x, y];
}

// Updates the specified point (start or destination) based on the provided coordinates.
// If the point is already set to these coordinate, it is removed.
void UpdatePoint(ref Vector2? point, int x, int y)
{
    // Ensure that the cell is not a wall.
    matrixMap.WallBlocks[x, y] = false;

    if (!point.HasValue)
    {
        point = new Vector2(x, y);
    }
    else if ((int)point.Value.X != x || (int)point.Value.Y != y)
    {
        point = new Vector2(x, y);
    }
    else
    {
        point = null;
    }
}

// Clears all wall blocks, start and destination points, and the current path after confirmation.
void Clear()
{
    if (!PromptConfirmation())
        return;

    Array.Clear(matrixMap.WallBlocks);
    startPoint = null;
    destinationPoint = null;
    path = null;
}

#endregion
