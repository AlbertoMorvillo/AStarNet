// Copyright (c) 2025 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

using System.Text.RegularExpressions;

namespace ConsoleDemo.Helpers
{
    /// <summary>
    /// Represents a segment of text along with its associated color.
    /// </summary>
    public class ColoredSegment
    {
        /// <summary>
        /// Gets or sets the color associated with this segment ("none" if not specified).
        /// </summary>
        public required string Color { get; set; }

        /// <summary>
        /// Gets or sets the text of the segment.
        /// </summary>
        public required string Text { get; set; }
    }

    /// <summary>
    /// Provides functionality to parse a string with color markup into colored segments.
    /// </summary>
    public static partial class ColoredTextParser
    {
        /// <summary>
        /// Returns a compiled <see cref="Regex"/> that matches HTML-like color tags in the format 
        /// <c>&lt;color&gt;text&lt;/color&gt;</c>. It captures the color name in the "color" group and the enclosed text in the "text" group.
        /// </summary>
        /// <returns>A <see cref="Regex"/> instance that matches color tags with case‐insensitive matching.</returns>
        [GeneratedRegex(@"<(?<color>\w+)>(?<text>.*?)</\k<color>>", RegexOptions.IgnoreCase)]
        private static partial Regex ColoredTextParserRegex();

        /// <summary>
        /// Splits the input string into segments, each with its associated color.
        /// Text outside of any color tags is assigned the color "none".
        /// </summary>
        /// <param name="input">
        /// The input string containing color tags (for example: 
        /// "Hello, <red>world</red>! This is a <green>test</green>.").
        /// </param>
        /// <returns>
        /// A list of <see cref="ColoredSegment"/> objects, where each segment includes the text and the associated color.
        /// </returns>
        public static List<ColoredSegment> Parse(string input)
        {
            List<ColoredSegment> segments = [];

            // Regex pattern to match tags of the form <color>text</color>
            Regex regex = ColoredTextParserRegex();
            int currentIndex = 0;

            // Process each match of the color tag.
            foreach (Match match in regex.Matches(input))
            {
                // If there is text before this match, add it as a segment with "none" color.
                if (match.Index > currentIndex)
                {
                    segments.Add(new ColoredSegment
                    {
                        Color = "none",
                        Text = input[currentIndex..match.Index]
                    });
                }

                // Add the segment captured by the current tag.
                segments.Add(new ColoredSegment
                {
                    Color = match.Groups["color"].Value,
                    Text = match.Groups["text"].Value
                });

                currentIndex = match.Index + match.Length;
            }

            // If any text remains after the last match, add it as a "none" segment.
            if (currentIndex < input.Length)
            {
                segments.Add(new ColoredSegment
                {
                    Color = "none",
                    Text = input[currentIndex..]
                });
            }

            return segments;
        }
    }
}