using C_Obfuscation;
using DonutMaker;
namespace ObfuscatingTool
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //Get inputs
            Console.WriteLine("Obfuscation Tool Started");
            Console.WriteLine("Enter csproj file path:");
            string inputCSPROJ = Console.ReadLine()!;
            Console.WriteLine("Enter image file path:");
            string inputImage = Console.ReadLine()!;
            Console.WriteLine("Enter output csproj file path:");
            string outputCSPROJ = Console.ReadLine()!;

            //Process inputs
            List<string> allSourceFiles = InputTool.GetCodeFilePaths(inputCSPROJ);
            //Obfuscate code
            string code = Obfuscator.ObfuscateProject(allSourceFiles);
            //Generate image code
            string imagedCode = TextFormater.GenerateCodeArt(inputImage, code, true, "C:\\Users\\Tex\\OneDrive\\Desktop\\SillyNikitaThingy.txt");
            //Generate output
            ProjectBuilder.BuildProject(outputCSPROJ, code);

        }
    }
}

