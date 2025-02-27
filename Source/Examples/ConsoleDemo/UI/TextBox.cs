// Copyright (c) 2025 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using ConsoleDemo.Helpers;
using System.Text;

namespace ConsoleDemo.UI
{
    /// <summary>
    /// Represents a text box that formats and displays text on the console with color support.
    /// The text box manages a list of lines and supports writing text with automatic wrapping.
    /// </summary>
    public class TextBox
    {
        #region Private Classes

        /// <summary>
        /// Represents a line of text with associated color information for each character.
        /// </summary>
        private class ColoredLine
        {
            /// <summary>
            /// The original text, including any color tags.
            /// </summary>
            public string OriginalText { get; }

            /// <summary>
            /// The array of individual characters extracted from the original text.
            /// </summary>
            public char[] Letters { get; }

            /// <summary>
            /// The array of color codes corresponding to each letter.
            /// A negative value indicates that no color is applied.
            /// </summary>
            public sbyte[] Colors { get; }

            /// <summary>
            /// Gets the number of characters in the line.
            /// </summary>
            public int Length => this.Letters.Length;

            /// <summary>
            /// Represents an empty colored line.
            /// </summary>
            public static ColoredLine Empty { get; } = new ColoredLine(string.Empty);

            /// <summary>
            /// Constructs a new ColoredLine from the specified text.
            /// The text is parsed for color segments.
            /// </summary>
            /// <param name="text">The text to process.</param>
            public ColoredLine(string text)
            {
                if (string.IsNullOrEmpty(text))
                {
                    this.OriginalText = string.Empty;
                    this.Letters = [];
                    this.Colors = [];
                }
                else
                {
                    // Parse the text into segments (each segment has text and a color string).
                    List<ColoredSegment> segments = ColoredTextParser.Parse(text);
                    List<char> letters = [];
                    List<sbyte> colors = [];

                    foreach (ColoredSegment segment in segments)
                    {
                        // Try to parse the color name into a ConsoleColor.
                        if (Enum.TryParse(typeof(ConsoleColor), segment.Color, true, out object? parsedColor) &&
                            parsedColor is ConsoleColor consoleColor)
                        {
                            foreach (char letter in segment.Text)
                            {
                                letters.Add(letter);
                                colors.Add((sbyte)consoleColor);
                            }
                        }
                        else
                        {
                            // If color parsing fails, use -1 as default (no color).
                            foreach (char letter in segment.Text)
                            {
                                letters.Add(letter);
                                colors.Add(-1);
                            }
                        }
                    }

                    this.OriginalText = text;
                    this.Letters = [.. letters];
                    this.Colors = [.. colors];
                }
            }

            /// <summary>
            /// Constructs a new ColoredLine using pre-split arrays of letters and colors.
            /// </summary>
            /// <param name="letters">The array of letters.</param>
            /// <param name="colors">The array of color codes.</param>
            public ColoredLine(char[] letters, sbyte[] colors)
            {
                this.OriginalText = string.Empty;
                this.Letters = letters;
                this.Colors = colors;
            }

            /// <summary>
            /// Splits this ColoredLine into chunks of the specified size.
            /// Each chunk retains its corresponding letters and color codes.
            /// </summary>
            /// <param name="chunkSize">The maximum number of characters per chunk.</param>
            /// <returns>An IReadOnlyList of ColoredLine chunks.</returns>
            public IReadOnlyList<ColoredLine> Chunk(int chunkSize)
            {
                List<ColoredLine> chunks = [];

                if (this.Length > chunkSize)
                {
                    for (int i = 0; i < this.Length; i += chunkSize)
                    {
                        int size = Math.Min(chunkSize, this.Length - i);
                        char[] chunkLetters = new char[size];
                        sbyte[] chunkColors = new sbyte[size];

                        for (int j = 0; j < size; j++)
                        {
                            chunkLetters[j] = this.Letters[i + j];
                            chunkColors[j] = this.Colors[i + j];
                        }

                        chunks.Add(new ColoredLine(chunkLetters, chunkColors));
                    }
                }
                else
                {
                    chunks.Add(this);
                }

                return chunks.AsReadOnly();
            }

            /// <summary>
            /// Returns the plain text representation of the ColoredLine (ignoring color formatting).
            /// </summary>
            /// <returns>A string composed of the letters.</returns>
            public override string ToString() => new(this.Letters);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the left (column) position of the text box on the console.
        /// </summary>
        public int Left { get; set; } = 0;

        /// <summary>
        /// Gets or sets the top (row) position of the text box on the console.
        /// </summary>
        public int Top { get; set; } = 0;

        /// <summary>
        /// Gets or sets the width of the text box.
        /// </summary>
        public int Width { get; set; } = 0;

        /// <summary>
        /// Gets or sets the height of the text box.
        /// </summary>
        public int Height { get; set; } = 0;

        #endregion

        #region Fields

        /// <summary>
        /// The list of colored lines to be displayed in the text box.
        /// </summary>
        private readonly List<ColoredLine> _lines = [];

        #endregion

        #region Public Methods

        /// <summary>
        /// Begins a new write session by clearing any previously stored lines and 
        /// erasing the text box area on the console.
        /// </summary>
        public void BeginWrite()
        {
            this.Clear();
        }

        /// <summary>
        /// Appends the specified text as a new line in the text box.
        /// </summary>
        /// <param name="text">The text to add.</param>
        public void WriteLine(string text) => this.AddColoredLine(text);

        /// <summary>
        /// Appends an empty line to the text box.
        /// </summary>
        public void WriteLine() => this._lines.Add(ColoredLine.Empty);

        /// <summary>
        /// Appends the specified text to the current line.
        /// If no line exists, a new line is created.
        /// </summary>
        /// <param name="text">The text to append.</param>
        public void Write(string text)
        {
            if (this._lines.Count > 0)
            {
                // Concatenate the new text to the last line.
                string newText = this._lines[^1].OriginalText + text;
                this._lines.RemoveAt(this._lines.Count - 1);
                this.AddColoredLine(newText);
            }
            else
            {
                this.AddColoredLine(text);
            }
        }

        /// <summary>
        /// Ends the write session by processing the stored lines (wrapping if necessary)
        /// and displaying the text box content on the console.
        /// </summary>
        public void EndWrite()
        {
            Encoding previousEncode = Console.OutputEncoding;
            Console.OutputEncoding = Encoding.Unicode;

            if (this._lines.Count == 0)
                return;

            // Process each line to wrap text that exceeds the text box width.
            for (int i = 0; i < this._lines.Count; i++)
            {
                if (this._lines[i].Length > this.Width)
                {
                    IReadOnlyList<ColoredLine> chunks = this._lines[i].Chunk(this.Width);
                    this._lines.RemoveAt(i);

                    // Insert wrapped chunks in place of the original long line.
                    foreach (ColoredLine chunk in chunks)
                    {
                        this._lines.Insert(i, chunk);
                        i++;
                    }
                    i--;
                }
            }

            // Determine the number of lines to display (up to the height of the text box).
            int lineEdge = Math.Min(this.Height, this._lines.Count);

            // Display each visible line.
            for (int i = 0; i < lineEdge; i++)
            {
                if (this._lines[i].Length <= 0)
                    continue;

                Console.SetCursorPosition(this.Left, this.Top + i);
                WriteColoredLine(this._lines[i]);
            }

            this._lines.Clear();

            Console.OutputEncoding = previousEncode;
        }

        /// <summary>
        /// Clears the text box area by erasing the stored lines and writing blank spaces.
        /// </summary>
        public void Clear()
        {
            this._lines.Clear();

            // Clear the text box area by writing blank spaces.
            for (int i = 0; i < this.Height; i++)
            {
                Console.SetCursorPosition(this.Left, this.Top + i);
                Console.Write(new string(' ', this.Width));
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Parses the provided text and adds it as a new colored line to the text box.
        /// </summary>
        /// <param name="text">The text to add.</param>
        private void AddColoredLine(string text)
        {
            ColoredLine newLine = new(text);
            this._lines.Add(newLine);
        }

        /// <summary>
        /// Writes a ColoredLine to the console using its color formatting.
        /// Each letter is output with its associated color.
        /// </summary>
        /// <param name="line">The colored line to display.</param>
        private static void WriteColoredLine(ColoredLine line)
        {
            for (int i = 0; i < line.Letters.Length; i++)
            {
                if (line.Colors[i] >= 0)
                {
                    ConsoleColor previousColor = Console.ForegroundColor;
                    Console.ForegroundColor = (ConsoleColor)line.Colors[i];
                    Console.Write(line.Letters[i]);
                    Console.ForegroundColor = previousColor;
                }
                else
                {
                    Console.Write(line.Letters[i]);
                }
            }
        }

        #endregion
    }
}
