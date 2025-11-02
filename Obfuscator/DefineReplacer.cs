using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Obfuscator
{
    public class DefineReplacer
    {
        public static void TransformDefines(string codePath)
        {
            string code = File.ReadAllText(codePath);
            //Doesn't work on macros
            string codeStr = string.Empty;
            ;
            string[] lines = code.Split('\n');
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
                    codeStr = Regex.Replace(codeStr, $@"\b{newName}\b", originalName);
                }
                codeStr = lines[lineNum] + codeStr;
            }
            ;
            File.WriteAllText(codePath, codeStr);


        }
    }
}
