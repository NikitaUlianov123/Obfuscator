using CSharp_DeObfuscator;
using System.Text;
using DonutMaker;
namespace DeObfuscationTool
{
    internal class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine("Deobfuscation Tool Started...");
            Console.WriteLine("Input Obfuscated (File Program.cs):");
            // Read source code from file
            string sourceCode = File.ReadAllText(Console.ReadLine()!);
            // Deimagize the source code
            string deImagedCode = TextFormater.Deimagify(sourceCode);
            string obfucatedCode = DeObfuscator.GetObfuscatedCodeFromString(deImagedCode);
            string deobfuscatedCode= DeObfuscator.GetDeObfuscatedCode(obfucatedCode);           
            string deobfuscatedLiterals = DeObfuscator.GetDeObfuscatedLiterals(deobfuscatedCode);
            Console.WriteLine(deobfuscatedLiterals);
        }
    }
}
