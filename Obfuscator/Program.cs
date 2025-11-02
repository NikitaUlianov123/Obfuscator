using System.Text.Json;

namespace Obfuscator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string specsPath = "C:\\Users\\logan\\source\\repos\\ObfuscatorCTeam\\Obfuscator\\TokenJson.txt";
            string json = File.ReadAllText(specsPath);
            List<Spec>? specs = JsonSerializer.Deserialize<List<Spec>>(json);
            Lexing lexing = new Lexing(specs);
            var d = lexing.Lexering("C:\\Users\\logan\\Source\\Repos\\ObfuscatorCTeam\\Obfuscator\\codeToRun.txt");
            Parser parsing = new Parser("C:\\Users\\logan\\source\\repos\\ObfuscatorCTeam\\Obfuscator\\GrammarRobotC.txt");
            var w = parsing.Parsing(d, "Program", 0);
             ;
        }
    }
}
