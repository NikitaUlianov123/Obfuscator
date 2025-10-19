using System;
using System.Text;
using System.Collections.Generic;

namespace DonutMaker
{
    public class RectangleFormatter : ILayoutFormatter
    {
        public int Width { get; }

        // If width is 0, auto-calc using sqrt(length) like original function
        public RectangleFormatter(int width = 0)
        {
            Width = width;
        }

        // Keep existing convenient string API
        public string Format(string text) => LayoutUtils.GridToString(RenderGrid(text));

        // Render to grid so it can be composed
        public char[,] RenderGrid(string text)
        {
            text = TextProcessing.Normalize(text);
            if (string.IsNullOrEmpty(text))
                return LayoutUtils.CreateEmptyGrid(1, 1);

            int len = text.Length;
            int width = Width > 0 ? Width : (int)Math.Ceiling(Math.Sqrt(len));

            var words = TextProcessing.SplitWords(text);
            var lines = new List<string>();
            var lineWords = new List<string>();
            int lineLen = 0;

            foreach (var word in words)
            {
                if (lineLen + lineWords.Count + word.Length <= width)
                {
                    lineWords.Add(word);
                    lineLen += word.Length;
                }
                else
                {
                    lines.Add(Justifier.JustifyLine(lineWords, lineLen, width));
                    lineWords.Clear();
                    lineWords.Add(word);
                    lineLen = word.Length;
                }
            }

            if (lineWords.Count > 0)
            {
                // last line left aligned and padded to width
                string last = string.Join(" ", lineWords).PadRight(width);
                lines.Add(last);
            }

            int rows = lines.Count;
            var grid = LayoutUtils.CreateEmptyGrid(rows, width);
            for (int r = 0; r < rows; r++)
            {
                var row = lines[r];
                for (int c = 0; c < width && c < row.Length; c++)
                    grid[r, c] = row[c];
            }

            return grid;
        }
    }
}