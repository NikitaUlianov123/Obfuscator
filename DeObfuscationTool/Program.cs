using CSharp_DeObfuscator;
using System.Text;
namespace DeObfuscationTool
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string sourceCode= File.ReadAllText("C:\\Users\\Tex\\OneDrive\\Desktop\\ObfuscatedCode\\Program.cs");
            ;

            string obfucatedCode = DeObfuscator.GetObfuscatedCodeFromString(sourceCode);
            string deobfuscatedCode= DeObfuscator.GetDeObfuscatedCode(obfucatedCode);
            Console.WriteLine(deobfuscatedCode);
        }
    }
}
