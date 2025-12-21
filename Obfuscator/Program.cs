using System.Globalization;
using System.Text.Json;

namespace Obfuscator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string[] filePath = Directory.GetCurrentDirectory().Split("\\");

            string relativePath = string.Join('\\', filePath, 0, filePath.Length - 3);

            DefineReplacer.TransformDefinedFilePath($"{relativePath}\\codeToRun.txt");
            ;
            string specsPath = $"{relativePath}\\TokenJson.txt";
            string json = File.ReadAllText(specsPath);
            List<Spec>? specs = JsonSerializer.Deserialize<List<Spec>>(json);
            Lexing lexing = new Lexing(specs);
            var d = lexing.Lexering($"{relativePath}\\codeToRun.txt");
            Parser parsing = new Parser($"{relativePath}\\GrammarRobotC.txt");
            var w = parsing.Parsing(d, "Body", 0);
            string temp = w.CodeGen();
            string ddd = w.IRGen(new VaribleStack());
            File.WriteAllText("C:\\Users\\logan\\Source\\Repos\\ObfuscatorCTeam\\Obfuscator\\OutputTextFile.txt", ddd);

            ;
        }
    }
}
