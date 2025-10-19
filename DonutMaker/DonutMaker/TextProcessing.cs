using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace DonutMaker
{
    static class TextProcessing
    {
        // Collapse whitespace and trim
        public static string Normalize(string text) =>
            string.IsNullOrWhiteSpace(text) ? string.Empty :
            Regex.Replace(text.Trim(), @"\s+", " ");

        // Split on single space after normalization
        public static List<string> SplitWords(string normalizedText) =>
            new List<string>(normalizedText.Length == 0 ? new string[0] : normalizedText.Split(' '));

        // Build a string consisting of 'count' spaces
        public static string Spaces(int count) => new string(' ', count);
    }

    static class Justifier
    {
        // Justify a line to width (for rectangle)
        public static string JustifyLine(List<string> words, int lineLen, int width)
        {
            if (words.Count == 0) return string.Empty;
            if (words.Count == 1) return words[0].PadRight(width);

            int totalSpaces = width - lineLen;
            int gaps = words.Count - 1;
            int spaceWidth = totalSpaces / gaps;
            int extraSpaces = totalSpaces % gaps;

            var sb = new StringBuilder();
            for (int i = 0; i < words.Count; i++)
            {
                sb.Append(words[i]);
                if (i < gaps)
                    sb.Append(TextProcessing.Spaces(spaceWidth + (i < extraSpaces ? 1 : 0)));
            }

            return sb.ToString();
        }

        // Justify a segment (for donut); centers single-word segments
        public static string JustifySegment(List<string> words, int lineLen, int segLen)
        {
            var sb = new StringBuilder(segLen);
            if (words.Count == 0)
                return TextProcessing.Spaces(segLen);

            if (words.Count == 1)
            {
                int leftPad = (segLen - lineLen) / 2;
                sb.Append(TextProcessing.Spaces(leftPad));
                sb.Append(words[0]);
                sb.Append(TextProcessing.Spaces(segLen - leftPad - words[0].Length));
                return sb.ToString();
            }

            int totalSpaces = segLen - lineLen;
            int gaps = words.Count - 1;
            int spaceWidth = totalSpaces / gaps;
            int extra = totalSpaces % gaps;

            for (int i = 0; i < words.Count; i++)
            {
                sb.Append(words[i]);
                if (i < gaps)
                    sb.Append(TextProcessing.Spaces(spaceWidth + (i < extra ? 1 : 0)));
            }

            if (sb.Length < segLen)
                sb.Append(TextProcessing.Spaces(segLen - sb.Length));
            else if (sb.Length > segLen)
                sb.Length = segLen;

            return sb.ToString();
        }
    }
}