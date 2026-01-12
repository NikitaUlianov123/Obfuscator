using C_Obfuscation;
using DonutMaker;
namespace ObfuscatingTool
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Obfuscation Tool Started");
            Console.WriteLine("Enter csproj file path:");
            string inputCSPROJ = Console.ReadLine()!;
            Console.WriteLine("Enter image file path:");
            string inputImage = Console.ReadLine()!;
            Console.WriteLine("Enter output csproj file path:");
            string outputCSPROJ = Console.ReadLine()!;

            List<string> allSourceFiles = InputTool.GetCodeFilePaths(inputCSPROJ);
            string code = Obfuscator.ObfuscateProject(allSourceFiles);
            string imagedCode = TextFormater.GenerateCodeArt(inputImage, code, true, "C:\\Users\\Tex\\OneDrive\\Desktop\\SillyNikitaThingy.txt");
            Console.WriteLine(imagedCode);
            ProjectBuilder.BuildProject(outputCSPROJ, imagedCode);

        }
    }
}

