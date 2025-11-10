using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing; // for Resize()

using System.Text;
using System.Text.RegularExpressions;

namespace DonutMaker
{
    public class TextFormat
    {
        public int Width { get; }
        public int Height { get; }
        public bool[,] Mask { get; }

        public bool this[int y, int x] => Mask[y, x];

        public TextFormat(bool[,] mask)
        {
            Mask = mask;
            Height = mask.GetLength(0);
            Width = mask.GetLength(1);
        }

        public static string RemoveComments(string code)
        {
            if (string.IsNullOrEmpty(code))
                return code;

            // Regex:
            // 1. Match // comments (until line end)
            // 2. Match /* ... */ comments (including multiline)
            string pattern = @"(//[^\r\n]*)|(/\*[\s\S]*?\*/)";
            return Regex.Replace(code, pattern, "");
        }

        public static string ConvertToAscii(string path, int width)
        {
            using var image = Image.Load<Rgba32>(path);

            // Maintain aspect ratio
            double ratio = (double)image.Height / image.Width;
            int height = (int)(width * ratio * 0.5); // 0.5 = adjust for font aspect ratio

            // Resize the image
            image.Mutate(x => x.Resize(width, height));

            var sb = new StringBuilder();
            const string chars = "@%#*+=-:. "; // darkest → lightest

            for (int y = 0; y < image.Height; y++)
            {
                sb.Append("//");
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = image[x, y];
                    byte gray = (byte)((pixel.R + pixel.G + pixel.B) / 3);

                    int index = gray * (chars.Length - 1) / 255;
                    sb.Append(chars[chars.Length - 1 - index]); // invert if you want dark=solid
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }


        public static void GenerateCodeArt(string imagePath, string code, string outputPath, int maxWidth)
        {
            //Load image and resize
            using var image = Image.Load<Rgba32>(imagePath);
            int height = (int)(image.Height * (maxWidth / (float)image.Width));
            image.Mutate(x => x.Resize(maxWidth, height));

            //Convert to brightness map
            byte[,] brightness = new byte[image.Width, image.Height];
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var p = image[x, y];
                    byte b = (byte)((p.R + p.G + p.B) / 3);
                    brightness[x, y] = b;
                }
            }

            // 3️⃣ Define filler snippets for brightness ranges
            var fillerSnippets = new (int threshold, string snippet)[]
            {
                (32, "if(true){}"),
                (64, "for(int i=0;i<1;i++){}"),
                (96, "try{}catch{}"),
                //(128, "return;"),
                //(160, "break;"),
                (192, "/* */"),
                (224, "//"),
                (256, "  ")
            };

            string GetSnippetForBrightness(byte b)
                => fillerSnippets.First(f => b < f.threshold).snippet;

            //Split input code into safe chunks
            string[] codeChunks = Regex.Split(code, @"(\s+|[;{}()\[\]=,+\-*/])")
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();

            //Generate C# art
            var sb = new StringBuilder();
            //sb.AppendLine("using System;");
            //sb.AppendLine("class Art { static void Main() {");

            int codeIndex = 0;
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    byte b = brightness[x, y];
                    string snippet = GetSnippetForBrightness(b);

                    // Occasionally insert real code snippet
                    if (codeIndex < codeChunks.Length && (x + y * image.Width) % 9 == 0)
                        snippet = codeChunks[codeIndex++];

                    sb.Append(snippet + " ");
                }
                sb.AppendLine();
            }

            //sb.AppendLine("} }");

            //Save to file
            File.WriteAllText(outputPath, sb.ToString());
            Console.WriteLine(sb.ToString());
        }




        public static void GenerateCodeArt2(
        string imagePath,
        string code,
        string outputPath,
        int cellsWide = 100,
        int preferredCellWidth = 0,
        bool splitLongChunks = true)
        {
            // 1) load image and compute target height in cells
            using var image = Image.Load<Rgba32>(imagePath);
            // Approximate character aspect ratio (height / width) for monospace fonts.
            // If each cell contains many characters, the effective width grows, so we'll not adjust cell aspect here;
            // we assume cell is rectangular block of characters whose height ~ 1 and width ~ cellWidth.
            // Use baseline char aspect for single-character cells:
            const float charAspect = 0.55f; // tweak 0.45-0.6 depending on your font

            int targetCellsX = Math.Max(1, cellsWide);
            int targetCellsY = Math.Max(1, (int)(image.Height * (targetCellsX / (float)image.Width) * charAspect));

            image.Mutate(x => x.Resize(targetCellsX, targetCellsY));

            // 2) brightness map
            byte[,] brightness = new byte[targetCellsX, targetCellsY];
            for (int y = 0; y < targetCellsY; y++)
                for (int x = 0; x < targetCellsX; x++)
                {
                    var p = image[x, y];
                    brightness[x, y] = (byte)((p.R * 0.2126f + p.G * 0.7152f + p.B * 0.0722f));
                }

            // 3) filler snippets (can be adjusted)
            var fillerSnippets = new (int threshold, string snippet)[]
            {
            (32, "if(true){}"),
            (64, "for(int i=0;i<1;i++){}"),
            (96, "try{}catch{}"),
            //(128, "return;"),
            //(160, "break;"),
            (192, "/* */"),
            (224, "//"),
            (256, "  ")
            };
            string GetSnippet(byte b) => fillerSnippets.First(f => b < f.threshold).snippet;

            // 4) split input code into tokens (keeps punctuation tokens)
            // We'll split into tokens that are reasonably small so they fit cells.
            var rawTokens = Regex.Split(code, @"(\s+|[;{}()\[\]=,+\-*/\.])")
                                 .Where(s => s != null && s.Length > 0)
                                 .ToArray();

            // Optionally collapse purely whitespace tokens (but we may want to preserve spaces)
            // Keep them as tokens so they get inserted as ' ' cells when chosen.

            // 5) determine cell width
            int maxFillerLen = fillerSnippets.Max(f => f.snippet.Length);
            int maxTokenLen = rawTokens.Any() ? rawTokens.Max(t => t.Length) : 0;
            int computedCellWidth = Math.Max(maxFillerLen, Math.Min(maxTokenLen, preferredCellWidth == 0 ? maxTokenLen : preferredCellWidth));

            // If user didn't set preferredCellWidth, pick a reasonable width:
            if (preferredCellWidth == 0)
            {
                // choose small but safe width; if the tokens are huge, use a cap
                const int cap = 30;
                computedCellWidth = Math.Clamp(Math.Max(maxFillerLen, Math.Min(maxTokenLen, 12)), maxFillerLen, cap);
            }
            else
            {
                computedCellWidth = Math.Max(computedCellWidth, preferredCellWidth);
            }

            int cellWidth = Math.Max(1, computedCellWidth);

            // 6) Prepare token queue, splitting long tokens if requested
            var tokens = new System.Collections.Generic.List<string>();
            foreach (var t in rawTokens)
            {
                if (t.Length <= cellWidth)
                {
                    tokens.Add(t);
                }
                else if (splitLongChunks)
                {
                    // split into chunks of size <= cellWidth
                    for (int i = 0; i < t.Length; i += cellWidth)
                    {
                        int len = Math.Min(cellWidth, t.Length - i);
                        tokens.Add(t.Substring(i, len));
                    }
                }
                else
                {
                    // truncate
                    tokens.Add(t.Substring(0, cellWidth));
                }
            }

            if (tokens.Count == 0) tokens.Add(""); // ensure at least one

            // 7) helper to pad to cellWidth
            string PadCell(string s) => s.Length == cellWidth ? s : s + new string(' ', cellWidth - s.Length);

            // Also store padded filler variants so we don't re-pad every time
            var paddedFillers = fillerSnippets.ToDictionary(f => f.snippet, f => PadCell(f.snippet));

            // 8) generate output .cs
            var sb = new StringBuilder();

            int codeIdx = 0;
            int totalCells = targetCellsX * targetCellsY;

            // Strategy for placing real tokens:
            // place tokens sparsely (e.g., every Nth cell). You can tune placement pattern.
            int placeEvery = Math.Max(6, Math.Min(20, Math.Max(6, cellWidth))); // heuristic

            for (int y = 0; y < targetCellsY; y++)
            {
                var lineBuilder = new StringBuilder();

                for (int x = 0; x < targetCellsX; x++)
                {
                    string chosen;
                    // determine snippet by brightness
                    var filler = GetSnippet(brightness[x, y]);

                    // Insert real token occasionally (pattern can be changed)
                    bool insertCode = (codeIdx < tokens.Count) && ((x + y * targetCellsX) % placeEvery == 0);

                    if (insertCode)
                    {
                        // take next token
                        chosen = tokens[codeIdx++];
                        // if token shorter than cell, pad
                        chosen = PadCell(chosen);
                    }
                    else
                    {
                        // padded filler
                        if (!paddedFillers.TryGetValue(filler, out chosen))
                            chosen = PadCell(filler);
                    }

                    lineBuilder.Append(chosen);
                }

                // Write the line indented into the method body
                sb.AppendLine("        " + lineBuilder.ToString());
            }

            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);

            Console.WriteLine(sb.ToString());

            Console.WriteLine($"Generated '{outputPath}' — cells: {targetCellsX}×{targetCellsY}, cellWidth={cellWidth}, tokens used={Math.Min(codeIdx, tokens.Count)}/{tokens.Count}");
        }


        public static string MakeOneLine(string input)
        {
            return Regex.Replace(input.Replace("\r", "").Replace("\n", "").Replace("\t", ""), " {2,}", " ");
        }

        public static string[] SplitPreprocessor(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return [string.Empty, string.Empty];

            // Normalize newlines
            code = code.Replace("\r\n", "\n");

            var lines = code.Split('\n');

            var preprocessor = lines
                .Where(line => line.TrimStart().StartsWith("#"))
                .Select(line => line.Trim())
                .ToArray();

            var others = lines
                .Where(line => !line.TrimStart().StartsWith("#"))
                .ToArray();

            string preprocessorJoined = string.Join(" ", preprocessor);
            string codeJoined = string.Join("\n", others);

            return [preprocessorJoined, codeJoined];
        }
    }
}