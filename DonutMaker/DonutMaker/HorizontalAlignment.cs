using System;
using System.Text;

namespace DonutMaker
{
    public enum HorizontalAlignment
    {
        Left,
        Center,
        Right
    }

    // Minimal layout formatter contract
    public interface ILayoutFormatter
    {
        // Render the formatted output into a 2D char grid (rows x cols)
        char[,] RenderGrid(string text);
    }

    public static class LayoutUtils
    {
        // Convert grid to string with newline per row
        public static string GridToString(char[,] grid)
        {
            if (grid == null) return string.Empty;
            var sb = new StringBuilder();
            int rows = grid.GetLength(0);
            int cols = grid.GetLength(1);
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                    sb.Append(grid[r, c]);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        // Create an empty grid filled with spaces
        public static char[,] CreateEmptyGrid(int rows, int cols)
        {
            var g = new char[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    g[r, c] = ' ';
            return g;
        }
    }

    // Stack two formatters vertically (top above bottom)
    // This version trims trailing empty rows of the top and leading empty rows of the bottom
    // to reduce the vertical gap between stacked items.
    public class VerticalStackFormatter : ILayoutFormatter
    {
        public ILayoutFormatter Top { get; }
        public ILayoutFormatter Bottom { get; }
        public HorizontalAlignment Alignment { get; }

        public VerticalStackFormatter(ILayoutFormatter top, ILayoutFormatter bottom, HorizontalAlignment alignment = HorizontalAlignment.Center)
        {
            Top = top ?? throw new ArgumentNullException(nameof(top));
            Bottom = bottom ?? throw new ArgumentNullException(nameof(bottom));
            Alignment = alignment;
        }

        public char[,] RenderGrid(string text)
        {
            // Render children grids
            var topGrid = Top.RenderGrid(text) ?? LayoutUtils.CreateEmptyGrid(1, 1);
            var bottomGrid = Bottom.RenderGrid(text) ?? LayoutUtils.CreateEmptyGrid(1, 1);

            // Trim trailing empty rows from top and leading empty rows from bottom
            int topRows = topGrid.GetLength(0);
            int topCols = topGrid.GetLength(1);
            int bottomRows = bottomGrid.GetLength(0);
            int bottomCols = bottomGrid.GetLength(1);

            int topTrim = CountTrailingEmptyRows(topGrid);
            int bottomTrim = CountLeadingEmptyRows(bottomGrid);

            int topEffectiveRows = Math.Max(0, topRows - topTrim);
            int bottomEffectiveRows = Math.Max(0, bottomRows - bottomTrim);

            // If everything trimmed away, keep a single blank row to avoid 0 height
            if (topEffectiveRows == 0 && bottomEffectiveRows == 0)
            {
                return LayoutUtils.CreateEmptyGrid(1, Math.Max(topCols, bottomCols));
            }

            int outCols = Math.Max(topCols, bottomCols);
            int outRows = topEffectiveRows + bottomEffectiveRows;

            var outGrid = LayoutUtils.CreateEmptyGrid(outRows, outCols);

            // copy top (only effective rows)
            CopyAligned(topGrid, outGrid, 0, Alignment, outCols, srcRowStart: 0, srcRowCount: topEffectiveRows);

            // copy bottom (skip leading empty rows)
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