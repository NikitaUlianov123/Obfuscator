using System;
using System.Text;
using System.Collections.Generic;

namespace DonutMaker
{
    public class DonutFormatter : ILayoutFormatter
    {
        // Tunable parameters
        public double InnerRatio { get; init; } = 0.4;   // inner radius = outer * InnerRatio
        public double VerticalCompression { get; init; } = 2.0;
        public double InitialScaleFactor { get; init; } = 0.5;
        public double MaxOuterRadius { get; init; } = 500.0;

        public string Format(string text) => LayoutUtils.GridToString(RenderGrid(text));

        // Render donut to grid (composable)
        public char[,] RenderGrid(string text)
        {
            text = TextProcessing.Normalize(text);
            if (string.IsNullOrEmpty(text))
                return LayoutUtils.CreateEmptyGrid(1, 1);

            int len = text.Length;

            // Estimate required outer radius analytically to avoid a slow/infinite incremental search.
            // Approximate mask capacity (number of character cells in annulus) as:
            //   capacity ≈ (π * R^2 * (1 - innerRatio^2)) / verticalCompression
            // Solve for R:
            //   R ≈ sqrt(len * verticalCompression / (π * (1 - innerRatio^2)))
            double denom = Math.PI * (1.0 - InnerRatio * InnerRatio);
            if (denom <= 0.0) denom = 1e-6; // guard against degenerate InnerRatio values

            double estimatedOuter = Math.Sqrt(len * VerticalCompression / denom);

            // Give a small margin and enforce minimum
            double outer = Math.Max(6.0, Math.Ceiling(estimatedOuter) + 1.0);

            // Clamp to MaxOuterRadius to avoid runaway sizes
            if (outer > MaxOuterRadius)
                outer = MaxOuterRadius;

            // If for some reason the estimate is insufficient (rare), fall back to a short incremental loop
            // to guarantee capacity without long running loops.
            if (MaskCapacity(outer) < len)
            {
                double step = outer < 30 ? 1.0 : Math.Max(0.5, outer * 0.03);
                int safety = 0;
                while (outer < MaxOuterRadius && MaskCapacity(outer) < len && safety++ < 2000)
                {
                    outer += step;
                }
            }

            double inner = Math.Max(0.0, outer * InnerRatio);
            int width = Math.Max(10, (int)Math.Ceiling(outer * 2 + 4));
            int height = Math.Max(6, (int)Math.Ceiling(outer * 2 + 2));

            var mask = BuildMask(width, height, outer, inner);
            var grid = LayoutUtils.CreateEmptyGrid(height, width);

            var words = TextProcessing.SplitWords(text);
            int wi = 0;

            for (int y = 0; y < height && wi < words.Count; y++)
            {
                int x = 0;
                while (x < width && wi < words.Count)
                {
                    while (x < width && !mask[y, x]) x++;
                    if (x >= width) break;

                    int segStart = x;
                    while (x < width && mask[y, x]) x++;
                    int segEnd = x;
                    int segLen = segEnd - segStart;
                    if (segLen <= 0) continue;

                    var lineWords = new List<string>();
                    int lineLen = 0;

                    while (wi < words.Count)
                    {
                        string w = words[wi];

                        if (w.Length > segLen && lineWords.Count == 0)
                        {
                            string piece = w.Substring(0, segLen);
                            string remainder = w.Substring(segLen);
                            lineWords.Add(piece);
                            lineLen += piece.Length;
                            words[wi] = remainder;
                            break;
                        }

                        if (lineLen + lineWords.Count + w.Length <= segLen)
                        {
                            lineWords.Add(w);
                            lineLen += w.Length;
                            wi++;
                        }
                        else break;
                    }

                    if (lineWords.Count == 0) continue;

                    string filled = Justifier.JustifySegment(lineWords, lineLen, segLen);
                    for (int i = 0; i < segLen; i++)
                        grid[y, segStart + i] = filled[i];
                }
            }

            return grid;
        }

        int MaskCapacity(double outerRadius)
        {
            double innerRadius = Math.Max(0.0, outerRadius * InnerRatio);
            int w = (int)Math.Ceiling(outerRadius * 2 + 4);
            int h = (int)Math.Ceiling(outerRadius * 2 + 2);
            int count = 0;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    double dx = x - (w - 1) / 2.0;
                    double dy = (y - (h - 1) / 2.0) * VerticalCompression;
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist >= innerRadius && dist <= outerRadius)
                        count++;
                }
            }
            return count;
        }

        bool[,] BuildMask(int width, int height, double outerRadius, double innerRadius)
        {
            var mask = new bool[height, width];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double dx = x - (width - 1) / 2.0;
                    double dy = (y - (height - 1) / 2.0) * VerticalCompression;
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    mask[y, x] = dist >= innerRadius && dist <= outerRadius;
                }
            }
            return mask;
        }
    }
}