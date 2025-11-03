using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

namespace Obfuscator
{
    public class DefineReplacer
    {
        public static void TransformDefinedFilePath(string codePath)
        {
            string code = File.ReadAllText(codePath);

            File.WriteAllText(codePath, TransformDefineText(code));
        }
        

        public static string TransformDefinedCode(string code)
        {
            return TransformDefineText(code);
        }
        private static string TransformDefineText(string code)
        {

            string codeStr = string.Empty;
            ;
            string newline = code.Contains("\r\n") ? "\r\n" : "\n";
            string[] lines = code.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

            for (int lineNum = lines.Length - 1; lineNum >= 0; lineNum--)
            {
                if (lines[lineNum].StartsWith("#define "))
                {
                    string[] defineArgs = lines[lineNum].Split(' ');

                    string newName = string.Empty;
                    string originalName = string.Empty;

                    for (int i = 1; i < defineArgs.Length; i++)
                    {
                        if (defineArgs[i].Length == 0) continue;

                        if (newName == string.Empty)
                        {
                            newName = defineArgs[i];
                        }
                        else
                        {
                            originalName = defineArgs[i];
                            break;
                        }
                    }

                    string escapedName = Regex.Escape(newName);
                    string pattern = $@"(?<=\(|\)){escapedName}(?=\r?\n)|(?<![A-Za-z_]){escapedName}(?![A-Za-z_])";

                    codeStr = Regex.Replace(codeStr, pattern, match => originalName);
                }
                codeStr = lines[lineNum] + (lineNum < lines.Length - 1 ? newline : string.Empty) + codeStr;
            }
            ;
            return codeStr;
            
        }
    }
}
