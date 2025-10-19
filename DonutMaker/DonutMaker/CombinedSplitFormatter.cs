using System;
using System.Collections.Generic;

namespace DonutMaker
{
    // Splits the input text between two formatters (top/bottom) and stacks them.
    // Uses a nearest-fit split (by character count) on word boundaries to avoid dropping words.
    // Trims trailing empty rows of the top and leading empty rows of the bottom
    // to eliminate the empty gap between stacked items.
    public class CombinedSplitFormatter : ILayoutFormatter
    {
        public ILayoutFormatter Top { get; }
        public ILayoutFormatter Bottom { get; }
        public double TopFraction { get; init; } = 0.6;
        public HorizontalAlignment Alignment { get; init; } = HorizontalAlignment.Center;

        public CombinedSplitFormatter(ILayoutFormatter top, ILayoutFormatter bottom, double topFraction = 0.6, HorizontalAlignment alignment = HorizontalAlignment.Center)
        {
            Top = top ?? throw new ArgumentNullException(nameof(top));
            Bottom = bottom ?? throw new ArgumentNullException(nameof(bottom));
            if (topFraction <= 0.0 || topFraction >= 1.0)
                throw new ArgumentOutOfRangeException(nameof(topFraction), "TopFraction must be between 0 and 1 (exclusive).");
            TopFraction = topFraction;
            Alignment = alignment;
        }

        public char[,] RenderGrid(string text)
        {
            text = TextProcessing.Normalize(text);
            if (string.IsNullOrEmpty(text))
                return LayoutUtils.CreateEmptyGrid(1, 1);

            var words = TextProcessing.SplitWords(text);
            if (words.Count == 0)
                return LayoutUtils.CreateEmptyGrid(1, 1);

            int totalChars = text.Length;
            int target = (int)Math.Round(totalChars * TopFraction);

            // Find split index (inclusive) that gives cumulative char count closest to target.
            // cumulative count includes single spaces between words.
            int bestIndex = 0;
            int bestDiff = int.MaxValue;
            int acc = 0;

            for (int i = 0; i < words.Count; i++)
            {
                if (i == 0) acc = words[0].Length;
                else acc += 1 + words[i].Length;

                int diff = Math.Abs(acc - target);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    bestIndex = i;
                }
            }

            // Construct top and bottom word lists using bestIndex
            var topWords = new List<string>();
            var bottomWords = new List<string>();

            for (int i = 0; i <= bestIndex && i < words.Count; i++)
                topWords.Add(words[i]);
            for (int i = bestIndex + 1; i < words.Count; i++)
                bottomWords.Add(words[i]);

            // Edge cases: ensure both sides have at least one word
            if (topWords.Count == 0 && bottomWords.Count > 0)
            {
                topWords.Add(bottomWords[0]);
                bottomWords.RemoveAt(0);
            }
            if (bottomWords.Count == 0 && topWords.Count > 1)
            {
                // move last word from top to bottom
                var last = topWords[topWords.Count - 1];
                topWords.RemoveAt(topWords.Count - 1);
                bottomWords.Add(last);
            }

            string topText = string.Join(" ", topWords);
            string bottomText = string.Join(" ", bottomWords);

            var topGrid = Top.RenderGrid(topText) ?? LayoutUtils.CreateEmptyGrid(1, 1);
            var bottomGrid = Bottom.RenderGrid(bottomText) ?? LayoutUtils.CreateEmptyGrid(1, 1);

            // Trim trailing empty rows from top and leading empty rows from bottom
            int topCols = topGrid.GetLength(1);
            int bottomCols = bottomGrid.GetLength(1);

            int topTrim = CountTrailingEmptyRows(topGrid);
            int bottomTrim = CountLeadingEmptyRows(bottomGrid);

            int topRows = topGrid.GetLength(0);
            int bottomRows = bottomGrid.GetLength(0);

            int topEffectiveRows = Math.Max(0, topRows - topTrim);
            int bottomEffectiveRows = Math.Max(0, bottomRows - bottomTrim);

            // Ensure we don't produce a zero-height result; keep at least one row
            if (topEffectiveRows == 0 && bottomEffectiveRows == 0)
                return LayoutUtils.CreateEmptyGrid(1, Math.Max(topCols, bottomCols));

            int outCols = Math.Max(topCols, bottomCols);
            int outRows = topEffectiveRows + bottomEffectiveRows;

            var outGrid = LayoutUtils.CreateEmptyGrid(outRows, outCols);

            // Copy only effective rows from top (skip trailing empty rows)
            CopyAligned(topGrid, outGrid, 0, Alignment, outCols, srcRowStart: 0, srcRowCount: topEffectiveRows);

            // Copy only effective rows from bottom (skip leading empty rows)
            CopyAligned(bottomGrid, outGrid, topEffectiveRows, Alignment, outCols, srcRowStart: bottomTrim, srcRowCount: bottomEffectiveRows);

            return outGrid;
        }

        static int CountTrailingEmptyRows(char[,] grid)
        {
            int rows = grid.GetLength(0);
            int cols = grid.GetLength(1);
            int trim = 0;
            for (int r = rows - 1; r >= 0; r--)
            {
                bool empty = true;
                for (int c = 0; c < cols; c++)
                {
                    if (grid[r, c] != ' ')
                    {
                        empty = false;
                        break;
                    }
                }
                if (empty) trim++; else break;
            }
            return trim;
        }

        static int CountLeadingEmptyRows(char[,] grid)
        {
            int rows = grid.GetLength(0);
            int cols = grid.GetLength(1);
            int trim = 0;
            for (int r = 0; r < rows; r++)
            {
                bool empty = true;
                for (int c = 0; c < cols; c++)
                {
                    if (grid[r, c] != ' ')
                    {
                        empty = false;
                        break;
                    }
                }
                if (empty) trim++; else break;
            }
            return trim;
        }

        static void CopyAligned(char[,] src, char[,] dst, int dstRowOffset, HorizontalAlignment align, int dstCols, int srcRowStart = 0, int srcRowCount = -1)
        {
            int srcRows = src.GetLength(0);
            int srcCols = src.GetLength(1);

            if (srcRowCount < 0) srcRowCount = srcRows;

            int xOffset = align switch
            {
                HorizontalAlignment.Left => 0,
                HorizontalAlignment.Center => (dstCols - srcCols) / 2,
                HorizontalAlignment.Right => dstCols - srcCols,
                _ => 0
            };
            xOffset = Math.Max(0, xOffset);

            for (int r = 0; r < srcRowCount; r++)
            {
                int srcR = srcRowStart + r;
                if (srcR < 0 || srcR >= srcRows) continue;
                for (int c = 0; c < srcCols; c++)
                {
                    int dr = dstRowOffset + r;
                    int dc = xOffset + c;
                    if (dr >= 0 && dr < dst.GetLength(0) && dc >= 0 && dc < dst.GetLength(1))
                        dst[dr, dc] = src[srcR, c];
                }
            }
        }
    }
}