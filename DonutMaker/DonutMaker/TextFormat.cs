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
            using var image = Image.Load<Rgba32>(imagePath);

            const float charAspect = 0.25f;

            //compute target height in cells
            int targetCellsX = Math.Max(1, cellsWide);
            int targetCellsY = Math.Max(1, (int)(image.Height * targetCellsX / (float)image.Width / charAspect));

            image.Mutate(x => x.Resize(targetCellsX, targetCellsY));

            image.Mutate(x => x.Resize(targetCellsX, targetCellsY));

            //making brightness map
            byte[,] brightness = new byte[targetCellsX, targetCellsY];
            for (int y = 0; y < targetCellsY; y++)
            {
                for (int x = 0; x < targetCellsX; x++)
                {
                    var p = image[x, y];
                    brightness[x, y] = (byte)((p.R * 0.2126f + p.G * 0.7152f + p.B * 0.0722f));
                }
            }

            //filler snippets
            var fillerChars = new (int threshold, char snippet)[]
            {
                ( 86, '█'),
                (129, '▓'),
                (171, '▒'),
                (214, '░'),
                (256, ' ')
            };

            char GetFillerChar(byte b) => fillerChars.First(f => b < f.threshold).snippet;


            //split input code into tokens
            var tokens = Regex.Split(code, @"(\s+|[;{}()\[\]=,+\-*/\.])")
                                 .Where(s => s != null && s.Length > 0)
                                 .ToArray();

            //determine cell width
            int maxTokenLen = tokens.Max(t => t.Length);
            int cellWidth = Math.Min(maxTokenLen, preferredCellWidth == 0 ? maxTokenLen : preferredCellWidth);

            
            //helper to pad to cellWidth
            string PadCell(string s, byte brightness)
            {
                if (s.Length == cellWidth)
                {
                    return s;
                }
                else if (s.Length >= cellWidth - 4)
                {
                    return s + new string(' ', cellWidth - s.Length);
                }
                else
                {
                    return s + "/*" + new string(GetFillerChar(brightness), cellWidth - s.Length - 4) + "*/";
                }
            }


            var sb = new StringBuilder();

            int currToken = 0;
            int totalCells = targetCellsX * targetCellsY;

            int placeEveryCells = totalCells / tokens.Length; // number of cells between tokens

            int currSpotInCell = 0;

            for (int y = 0; y < targetCellsY; y++)
            {
                for (int x = 0; x < targetCellsX; x++)
                {
                    string chosen;

                    //if we can fit one token every 10 cells, it will have a 1/10 chance to place in a cell, until it hits 10 then it's
                    //a guaranteed placement
                    bool insertCode = (currToken < tokens.Length && Random.Shared.Next(placeEveryCells) == 0) || currSpotInCell >= placeEveryCells;

                    if (insertCode)
                    {
                        chosen = PadCell(tokens[currToken++], brightness[x, y]);
                    }
                    else
                    {
                        //filler code can be added here later
                        chosen = PadCell("", brightness[x, y]);
                    }

                    sb.Append(chosen); // always exactly cellWidth chars
                }

                sb.AppendLine();
            }

            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);

            Console.WriteLine(sb.ToString());

            Console.WriteLine($"Generated '{outputPath}' — cells: {targetCellsX}×{targetCellsY}, cellWidth={cellWidth}, tokens used={Math.Min(currToken, tokens.Length)}/{tokens.Length}");
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