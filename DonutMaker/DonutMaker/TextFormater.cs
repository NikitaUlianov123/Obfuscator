using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing; // for Resize()
using CSharpier;//for code formatting when deobfuscating

using System.Text;
using System.Text.RegularExpressions;
using CSharpier.Core.CSharp;

namespace DonutMaker
{
    public class TextFormater
    {
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
            string mainCode = MakeOneLine(SplitLongLiterals(RemoveComments(split[1])));

            string result = preprocessor + "\n" + GenerateCodeArt2(imagePath, mainCode);

            if (writeToFile)
            {
                File.WriteAllText(outputPath, result, Encoding.UTF8);
            }
            return result;
        }

        private static string GenerateCodeArt2(string imagePath, string code, int cellsWide = 70)
        {
            // split input code into tokens
            var tokens = TokenizeCode(code);

            //determine cell width
            int maxTokenLen = tokens.Max(t => t.Length);
            int cellWidth = Math.Min(maxTokenLen, maxTokenLen) + 4;//for the *//*

            using var image = Image.Load<Rgba32>(imagePath);

            #region Determine scaling

            // --- Character aspect ratio influenced by token size ---
            // Bigger tokens -> wider cell -> smaller aspect ratio
            float charAspect = (1f / cellWidth) * 2;//0.07f;//0.15f;

            //compute target height in cells
            int targetCellsX = Math.Max(1, cellsWide);
            int targetCellsY = Math.Max(1, (int)(image.Height * targetCellsX / (float)image.Width / charAspect));


            image.Mutate(x => x.MedianBlur(7, true));//GaussianBlur(4.0f)); // radius 3.0f is "typical"

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

            #endregion


            //helper to pad to cellWidth
            string PadCell(string s, byte brightness)
            {
                if (s.Length == cellWidth)
                {
                    return s;
                }
                else
                {
                    return s + new string(GetFillerChar(brightness), cellWidth - s.Length);
                }
            }


            var sb = new StringBuilder();

            int currToken = 0;
            int totalCells = targetCellsX * targetCellsY;

            int placeEveryCells = totalCells / (tokens.Count + 1); // number of cells between tokens

            int currSpotInCell = 0;

            for (int y = 0; y < targetCellsY; y++)
            {
                sb.Append("/*"); // start of line comment

                for (int x = 0; x < targetCellsX; x++)
                {
                    string chosen;

                    //if we can fit one token every 10 cells, it will have a 1/10 chance to place in a cell, until it hits 10 then it's
                    //a guaranteed placement
                    bool insertCode = currToken < tokens.Count && (Random.Shared.Next(placeEveryCells) == 0 || currSpotInCell++ >= placeEveryCells);

                    if (insertCode)
                    {
                        chosen = PadCell("*/" + tokens[currToken++] + "/*", (byte)(255 - brightness[x, y]));
                        currSpotInCell = 0;
                    }
                    else
                    {
                        //filler code can be added here later
                        chosen = PadCell("", (byte)(255 - brightness[x, y]));
                    }

                    sb.Append(chosen); // always exactly cellWidth chars
                }

                sb.Append("*/"); // end of line comment

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
            [
                ">=", "<=", "==", "!=", "++", "--",
                "+=", "-=", "*=", "/=", "%=",
                "&&", "||", "->", "=>",
                "<<", ">>", "??"
            ];
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

        private static string SplitLongLiterals(string code, int chunkSize = 15)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(chunkSize, 1);

            var sb = new StringBuilder();
            int i = 0;
            int n = code.Length;

            while (i < n)
            {
                char c = code[i];

                // If we see a quote that begins a string literal, handle it.
                if (c == '"')
                {
                    // Determine prefix characters right before quote: e.g. @, $, $@
                    int prefixStart = i - 1;
                    while (prefixStart >= 0)
                    {
                        char p = code[prefixStart];
                        if (p == '@') { prefixStart--; continue; }
                        if (p == '$') { prefixStart--; continue; }
                        break;
                    }
                    // But we only treat @/$ if immediately adjacent (no whitespace)
                    int prefixIdx = i - 1;
                    string prefix = "";
                    if (prefixIdx >= 0)
                    {
                        // gather contiguous sequence of @ and $ immediately before the quote
                        int j = prefixIdx;
                        while (j >= 0 && (code[j] == '@' || code[j] == '$')) j--;
                        if (j + 1 == prefixIdx - ((prefixIdx - j) - 0)) { /*noop*/ }
                        int start = j + 1;
                        if (start <= prefixIdx)
                        {
                            var pfx = code.Substring(start, prefixIdx - start + 1);
                            // only accept if it's composed solely of @ and $ (and not separated by spaces)
                            bool ok = true;
                            foreach (var ch in pfx) if (ch != '@' && ch != '$') { ok = false; break; }
                            if (ok) prefix = pfx;
                        }
                    }

                    // If this is an interpolated string (prefix contains $), skip splitting it.
                    if (prefix.Contains('$'))
                    {
                        // Just copy the string literal as-is (scan to its end properly)
                        sb.Append(code[i]); i++;
                        bool inVerbatim = prefix.Contains('@');
                        if (inVerbatim)
                        {
                            // verbatim: double quotes "" escape
                            while (i < n)
                            {
                                sb.Append(code[i]);
                                if (code[i] == '"' && i + 1 < n && code[i + 1] == '"')
                                {
                                    // "" inside verbatim -> skip both as content
                                    i += 2;
                                    continue;
                                }
                                if (code[i] == '"') { i++; break; }
                                i++;
                            }
                        }
                        else
                        {
                            // normal interpolated -> still respect backslash escapes
                            while (i < n)
                            {
                                sb.Append(code[i]);
                                if (code[i] == '\\' && i + 1 < n)
                                {
                                    sb.Append(code[i + 1]); i += 2; continue;
                                }
                                if (code[i] == '"') { i++; break; }
                                i++;
                            }
                        }
                        continue;
                    }

                    // Normal or verbatim string: parse content
                    bool isVerbatim = (i > 0 && code[i - 1] == '@');
                    // copy any prefix (the @) that immediately precedes the quote
                    // but only copy if it's exactly adjacent (no spaces)
                    int prefixLen = 0;
                    if (i > 0 && (code[i - 1] == '@' || code[i - 1] == '$'))
                    {
                        // only include @ if directly adjacent and not something like ident@
                        int p = i - 1;
                        while (p >= 0 && (code[p] == '@' || code[p] == '$')) p--;
                        if (p == i - prefixLen - 1 || true) { /* allow; we've already filtered interpolated above */ }
                    }

                    // Read literal content into a buffer (unescaped for verbatim it's literal content)
                    int startQuoteIdx = i;
                    i++; // skip opening "
                    var content = new StringBuilder();
                    if (isVerbatim)
                    {
                        // verbatim: "" -> represents a single " inside content
                        while (i < n)
                        {
                            if (code[i] == '"' && i + 1 < n && code[i + 1] == '"')
                            {
                                content.Append('"');
                                i += 2;
                                continue;
                            }
                            if (code[i] == '"')
                            {
                                i++; // consume closing quote
                                break;
                            }
                            content.Append(code[i]);
                            i++;
                        }
                    }
                    else
                    {
                        while (i < n)
                        {
                            if (code[i] == '\\' && i + 1 < n)
                            {
                                // keep escape as two characters in content (so we never split inside)
                                // but for chunking we count the actual logical char (we'll preserve escape in output)
                                // we append the escape as-is but mark it as two-char sequence
                                content.Append(code[i]); // '\'
                                content.Append(code[i + 1]); // e.g. 'n' or '"'
                                i += 2;
                                continue;
                            }
                            if (code[i] == '"')
                            {
                                i++; // consume closing quote
                                break;
                            }
                            content.Append(code[i]);
                            i++;
                        }
                    }

                    string contentStr = content.ToString();

                    // If small enough, emit original (including prefix like @ if present)
                    string prefixText = "";
                    // recompute immediate prefix (only @ for verbatim)
                    if (isVerbatim)
                    {
                        // If there is an @ directly before the quote, include it.
                        if (startQuoteIdx - 1 >= 0 && code[startQuoteIdx - 1] == '@')
                            prefixText = "@";
                    }

                    if (contentStr.Length <= chunkSize)
                    {
                        // Emit possibly prefix + quoted content with proper escaping
                        if (isVerbatim)
                        {
                            sb.Append(prefixText);
                            sb.Append('"');
                            // need to re-escape any " in content as ""
                            sb.Append(contentStr.Replace("\"", "\"\""));
                            sb.Append('"');
                        }
                        else
                        {
                            sb.Append('"');
                            sb.Append(EscapeForNormalLiteral(contentStr));
                            sb.Append('"');
                        }
                    }
                    else
                    {
                        // Split into chunks, trying not to split inside a backslash escape (for normal strings)
                        var parts = new List<string>();
                        if (isVerbatim)
                        {
                            int idx = 0;
                            while (idx < contentStr.Length)
                            {
                                int take = Math.Min(chunkSize, contentStr.Length - idx);
                                // no escape worries for verbatim except we must preserve quotes as doubled when re-emitting
                                parts.Add(contentStr.Substring(idx, take));
                                idx += take;
                            }

                            // Emit: @"part1" + @"part2" + ...
                            for (int pi = 0; pi < parts.Count; pi++)
                            {
                                if (pi > 0) sb.Append(" + ");
                                sb.Append("@\"");
                                sb.Append(parts[pi].Replace("\"", "\"\""));
                                sb.Append('"');
                            }
                        }
                        else
                        {
                            // For normal strings, we must ensure we don't split in the middle of a backslash escape sequence.
                            int idx = 0;
                            while (idx < contentStr.Length)
                            {
                                int len = Math.Min(chunkSize, contentStr.Length - idx);

                                // adjust len backwards if it ends on a solitary backslash char in the pair
                                // Note: content stores backslashes and their escape target as two chars (e.g. "\\n" or "\\\""),
                                // so we avoid splitting between '\' and its following char.
                                // If the last char in the chunk is '\' we move back one.
                                while (len > 0 && contentStr[idx + len - 1] == '\\')
                                {
                                    // If there's only one char left and it's a single backslash, reduce len by 1
                                    len--;
                                }

                                if (len == 0) len = Math.Min(chunkSize, contentStr.Length - idx); // fallback to avoid infinite loop

                                string part = contentStr.Substring(idx, len);
                                parts.Add(part);
                                idx += len;
                            }

                            for (int pi = 0; pi < parts.Count; pi++)
                            {
                                if (pi > 0) sb.Append(" + ");
                                sb.Append('"');
                                sb.Append(EscapeForNormalLiteral(parts[pi]));
                                sb.Append('"');
                            }
                        }
                    }

                    continue;
                }

                // not a quote, copy char
                sb.Append(c);
                i++;
            }

            return sb.ToString();

            static string EscapeForNormalLiteral(string s)
            {
                // s may already contain sequences like \" or \\ from how we accumulated; make sure we produce valid escapes:
                // We'll escape backslashes first, then double quotes.
                return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
            }
        }


        public static string Deimagify(string input)
        {
            return CSharpFormatter.Format(RemoveComments(input)).Code;
        }



        private static string RemoveComments(string code)
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