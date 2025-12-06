using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing; // for Resize()

using System.Text;
using System.Text.RegularExpressions;

namespace DonutMaker
{
    public class TextFormater
    {
        //filler snippets
        private static readonly (int threshold, char snippet)[] fillerChars =
        [
                ( 15, '█'),
                ( 30, '@'),
                ( 45, 'W'),
                ( 60, 'M'),
                ( 75, '#'),
                ( 90, '8'),
                (105, 'B'),
                (120, '%'),
                (135, '▓'),
                (150, 'S'),
                (165, 'K'),
                (180, '▒'),
                (195, 'X'),
                (210, 'o'),
                (225, 'c'),
                (235, '░'),
                (245, ':'),
                (250, ','),
                (253, '.'),
                (256, ' ')
        ];

        private static char GetFillerChar(byte b) => fillerChars.First(f => b < f.threshold).snippet;


        public static string GenerateCodeArt(string imagePath, string code, bool writeToFile = false, string outputPath = "")
        {
            string[] split = SplitPreprocessor(code);
            string preprocessor = split[0];
            string mainCode = TextFormater.MakeOneLine(TextFormater.RemoveComments(split[1]));

            string result = preprocessor + "\n" + GenerateCodeArt2(imagePath, mainCode);

            if (writeToFile)
            {
                File.WriteAllText(outputPath, result, Encoding.UTF8);
            }
            return result;
        }

        private static string GenerateCodeArt2(string imagePath, string code, int cellsWide = 100)
        {
            using var image = Image.Load<Rgba32>(imagePath);

            const float charAspect = 0.15f;

            //compute target height in cells
            int targetCellsX = Math.Max(1, cellsWide);
            int targetCellsY = Math.Max(1, (int)(image.Height * targetCellsX / (float)image.Width / charAspect));

            image.Mutate(x => x.Resize(targetCellsX, targetCellsY));

            //making brightness map
            byte[,] brightness = new byte[targetCellsX, targetCellsY];
            for (int y = 0; y < targetCellsY; y++)
            {
                for (int x = 0; x < targetCellsX; x++)
                {
                    var pixel = image[x, y];
                    brightness[x, y] = (byte)((pixel.R * 0.2126f + pixel.G * 0.7152f + pixel.B * 0.0722f));
                }
            }




            //split input code into tokens
            var tokens = TokenizeCode(code);

            //determine cell width
            int maxTokenLen = tokens.Max(t => t.Length);
            int cellWidth = Math.Min(maxTokenLen, maxTokenLen);


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

            int placeEveryCells = totalCells / (tokens.Count + 1); // number of cells between tokens

            int currSpotInCell = 0;

            for (int y = 0; y < targetCellsY; y++)
            {
                for (int x = 0; x < targetCellsX; x++)
                {
                    string chosen;

                    //if we can fit one token every 10 cells, it will have a 1/10 chance to place in a cell, until it hits 10 then it's
                    //a guaranteed placement
                    bool insertCode = currToken < tokens.Count && (Random.Shared.Next(placeEveryCells) == 0 || currSpotInCell++ >= placeEveryCells);

                    if (insertCode)
                    {
                        chosen = PadCell(tokens[currToken++], (byte)(255 - brightness[x, y]));
                        currSpotInCell = 0;
                    }
                    else
                    {
                        //filler code can be added here later
                        chosen = PadCell("", (byte)(255 - brightness[x, y]));
                    }

                    sb.Append(chosen); // always exactly cellWidth chars
                }

                sb.AppendLine();
            }

            return sb.ToString();

            //File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);

            //Console.WriteLine(sb.ToString());

            //Console.WriteLine($"Generated '{outputPath}' — cells: {targetCellsX}×{targetCellsY}, cellWidth={cellWidth}, tokens used={Math.Min(currToken, tokens.Length)}/{tokens.Length}");
        }

        private static List<string> TokenizeCode(string code)
        {
            var tokens = new List<string>();
            var current = new StringBuilder();

            bool inString = false;
            bool inVerbatimString = false;
            char stringDelimiter = '\0';

            string[] MultiOps =
            {
                ">=", "<=", "==", "!=", "++", "--",
                "+=", "-=", "*=", "/=", "%=",
                "&&", "||", "->", "=>",
                "<<", ">>", "??"
            };
            string singleTokens = "{}[]();,+-*/=.<>&|!?";

            for (int i = 0; i < code.Length; i++)
            {
                char c = code[i];

                // inside string literal
                if (inString)
                {
                    current.Append(c);

                    //skip escape characters
                    if (!inVerbatimString && c == '\\' && i + 1 < code.Length)
                    {
                        current.Append(code[i + 1]);
                        i++;
                        continue;
                    }

                    if (c == stringDelimiter)
                    {
                        if (inVerbatimString && i + 1 < code.Length && code[i + 1] == '"')
                        {
                            current.Append('"');
                            i++;
                            continue;
                        }

                        tokens.Add(current.ToString());
                        current.Clear();
                        inString = false;
                        inVerbatimString = false;
                    }

                    continue;
                }

                // detect start of string
                if (c == '"' || c == '\'')
                {
                    inString = true;
                    stringDelimiter = c;
                    current.Append(c);

                    if (i > 0 && code[i - 1] == '@')
                    {
                        inVerbatimString = true;
                    }

                    continue;
                }

                // detect multi-char operator
                bool matchedMulti = false;
                foreach (var op in MultiOps)
                {
                    if (i + op.Length <= code.Length && string.Compare(code, i, op, 0, op.Length) == 0)
                    {
                        if (current.Length > 0)
                        {
                            tokens.Add(current.ToString());
                            current.Clear();
                        }

                        tokens.Add(op);
                        i += op.Length - 1;
                        matchedMulti = true;
                        break;
                    }
                }
                if (matchedMulti) continue;

                // single-char punctuation
                if (char.IsWhiteSpace(c) || singleTokens.Contains(c))
                {
                    if (current.Length > 0)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                    }

                    tokens.Add(c.ToString());
                    continue;
                }

                // normal identifier/number fragment
                current.Append(c);
            }

            if (current.Length > 0)
            {
                tokens.Add(current.ToString());
            }

            return tokens;
        }




        public static string CondenseAndFormatCSharp(string input)
        {
            // 1. Collapse all whitespace sequences to single spaces
            var collapsed = Regex.Replace(input, @"\s+", " ");

            // 2. Insert line breaks after tokens that should start a new line or block
            collapsed = Regex.Replace(collapsed, @"(?<=[{};])", "\n");

            // 3. Remove extra spaces before punctuation
            collapsed = Regex.Replace(collapsed, @"\s+([{}();,\]])", "$1");

            // 4. Remove spaces after '(' and before ')'
            collapsed = Regex.Replace(collapsed, @"\(\s+", "(");
            collapsed = Regex.Replace(collapsed, @"\s+\)", ")");

            // 5. Now split into lines
            var lines = collapsed.Split('\n')
                                 .Select(l => l.Trim())
                                 .Where(l => l.Length > 0)
                                 .ToList();

            // 6. Re-indent based on braces
            int indent = 0;
            var sb = new StringBuilder();

            foreach (var line in lines)
            {
                if (line.StartsWith("}"))
                    indent--;

                sb.AppendLine(new string(' ', indent * 4) + line);

                if (line.EndsWith("{"))
                    indent++;
            }

            return sb.ToString();
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

        private static string MakeOneLine(string input)
        {
            return Regex.Replace(input.Replace("\r", "").Replace("\n", "").Replace("\t", ""), " {2,}", " ");
        }

        private static string[] SplitPreprocessor(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return [string.Empty, string.Empty];

            // Normalize newlines
            code = code.Replace("\r\n", "\n");

            var lines = code.Split('\n');

            var preprocessor = lines
                .Where(line => line.TrimStart().StartsWith('#'))
                .Select(line => line.Trim())
                .ToArray();

            var others = lines
                .Where(line => !line.TrimStart().StartsWith('#'))
                .ToArray();

            string preprocessorJoined = string.Join(" ", preprocessor);
            string codeJoined = string.Join("\n", others);

            return [preprocessorJoined, codeJoined];
        }



    }
}