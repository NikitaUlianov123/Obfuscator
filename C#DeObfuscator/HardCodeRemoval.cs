using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CSharp_DeObfuscator
{
    internal class HardCodeRemoval
    {
        public static string Remove(string code)
        {
            string literalsRemoved = RemoveLiterals(code);
            string helperClassRemoved = RemoveOHelperClass(literalsRemoved);
            return helperClassRemoved;
        }
        static string RemoveLiterals(string source)
        {
            bool changed;
            do
            {
                changed = false;

                // 1️⃣ Numeric literals first (must collapse nesting)
                source = Regex.Replace(
                    source,
                    @"\(Int32\)\s*_O\.L\s*\(\s*new\s+byte\[\]\s*\{([^{}]*)\}\s*\)",
                    m =>
                    {
                        if (m.Groups[1].Value.Contains("_O."))
                            return m.Value;

                        var bytes = ExtractBytes(m.Groups[1].Value)
                            .Select(DecodeByte)
                            .ToArray();

                        int value = BitConverter.ToInt32(bytes, 0);
                        changed = true;
                        return value.ToString();
                    });

                // 2️⃣ Char literals
                source = Regex.Replace(
                    source,
                    @"_O\.E\s*\(\s*new\s+byte\[\]\s*\{([^{}]*)\}\s*\)",
                    m =>
                    {
                        if (m.Groups[1].Value.Contains("_O."))
                            return m.Value;

                        byte b = DecodeByte(ExtractBytes(m.Groups[1].Value)[0]);
                        changed = true;
                        return EscapeChar((char)b);
                    });

                // 3️⃣ String literals
                source = Regex.Replace(
                    source,
                    @"_O\.D\s*\(\s*new\s+byte\[\]\s*\{([^{}]*)\}\s*\)",
                    m =>
                    {
                        if (m.Groups[1].Value.Contains("_O."))
                            return m.Value;

                        var bytes = ExtractBytes(m.Groups[1].Value)
                            .Select(DecodeByte)
                            .ToArray();

                        string s = Encoding.UTF8.GetString(bytes);
                        changed = true;
                        return EscapeString(s);
                    });

            } while (changed);

            return source;
        }
        public static string RemoveOHelperClass(string source)
        {
            // Match attributes + modifiers + class _O
            var match = Regex.Match(
                source,
                @"(?ms)^[ \t]*(?:\[[^\]]+\][ \t\r\n]*)*(?:public|internal|private|protected|static|sealed|abstract|partial|\s)+class\s+_O\b");

            if (!match.Success)
                return source;

            int start = match.Index;

            // Find the opening brace after the class declaration
            int braceStart = source.IndexOf('{', match.Index + match.Length);
            if (braceStart < 0)
                throw new InvalidOperationException("Malformed _O class (no opening brace).");

            int depth = 0;
            int i = braceStart;

            for (; i < source.Length; i++)
            {
                if (source[i] == '{') depth++;
                else if (source[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        i++; // include closing brace
                        break;
                    }
                }
            }

            if (depth != 0)
                throw new InvalidOperationException("Malformed _O class (unbalanced braces).");

            var sb = new StringBuilder(source.Length);
            sb.Append(source, 0, start);
            sb.Append(source, i, source.Length - i);

            return sb.ToString();
        }
        static byte DecodeByte(int x)
        {
            byte b = (byte)(x & 0xFF);
            return (byte)((b >> 4) | (b << 4));
        }

        static int[] ExtractBytes(string text)
        {
            return text.Split(',')
                       .Select(t => int.Parse(t.Trim()))
                       .ToArray();
        }

        static string EscapeString(string s) =>
            "\"" + s
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t") + "\"";

        static string EscapeChar(char c) => c switch
        {
            '\\' => "'\\\\'",
            '\'' => "'\\'''",
            '\n' => "'\\n'",
            '\r' => "'\\r'",
            '\t' => "'\\t'",
            _ => $"'{c}'"
        };
    }
}
