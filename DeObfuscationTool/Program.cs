using C_DeObfuscator;
using System.Text;
namespace DeObfuscationTool
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string code= File.ReadAllText("C:\\Users\\Tex\\OneDrive\\Desktop\\ObfuscatedCode\\Program.cs");
            ;
            string deobfuscatedCode = C_DeObfuscator.DeObfuscator.GetObfuscatedCodeFromString(code);
        }
    }
}
