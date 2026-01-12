using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CSharp_DeObfuscator
{
    internal class CodeRetriever
    {
        public static string GetCodeFromString(string originalCode)
        {
            string encodedSourceCode = GetSourceAssignmentLine(originalCode);
            string encodedCode = ExtractAndRotateBeforeConcat(encodedSourceCode);
            string finalCode = RemoveInsertedSeparators(encodedCode);
            return finalCode;
        }
        static string RotateRight4Bits(string input)
        {
            char[] result = new char[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                byte b = (byte)input[i];
                byte rotated = (byte)(((b >> 4) | (b << 4)) & 0xFF);
                result[i] = (char)rotated;
            }
            return new string(result);
        }
        static string RemoveInsertedSeparators(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var sb = new StringBuilder(input.Length / 2 + 1);

            for (int i = 0; i < input.Length; i++)
            {
                // Keep even-indexed characters
                if ((i & 1) == 0)
                    sb.Append(input[i]);
            }

            return sb.ToString();
        }
        static string ExtractAndRotateBeforeConcat(string sourceLine)
        {
            if (string.IsNullOrEmpty(sourceLine))
                return string.Empty;

            var regex = new Regex(
                @"R\s*\(\s*@?""((?:[^""]|"""")*)""\s*\)",
                RegexOptions.Singleline
            );

            var sb = new StringBuilder();

            foreach (Match match in regex.Matches(sourceLine))
            {
                // Get raw literal exactly as-is
                string encoded = match.Groups[1].Value;

                // Rotate this chunk BEFORE concatenating
                string rotated = RotateRight4Bits(encoded);

                // Append to result
                sb.Append(rotated);
            }

            return sb.ToString();
        }
        static string GetSourceAssignmentLine(string fullCode)
        {
            if (string.IsNullOrEmpty(fullCode))
                return null;

            var pattern = @"
(?sx)
\bstring\s+source\s*=\s*
(?<rhs>
    (?:
        @"" (?:[^""]|"""")* ""
      | "" (?:\\.|[^""\\])* ""
      | '(?:\\.|[^'\\])+'
      | (?!;) .
    )*
)
\s*;
";

            var m = Regex.Match(fullCode, pattern, RegexOptions.IgnorePatternWhitespace);
            return m.Success ? m.Groups["rhs"].Value : null;
        }
    }
}
