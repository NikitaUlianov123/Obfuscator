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

            Console.WriteLine("How many units wide would you like your image?");
            Console.WriteLine("(Default is 70, if the aspect ratio looks off, pick something smaller. If it's too small, pick something bigger)");
            int imageWidth;
            string widthInput = Console.ReadLine()!;
            int.TryParse(widthInput, out imageWidth);

            //Process inputs
            List<string> allSourceFiles = InputTool.GetCodeFilePaths(inputCSPROJ);
            //Obfuscate code
            string code = Obfuscator.ObfuscateProject(allSourceFiles);
            //Generate image code
            string imagedCode = TextFormater.GenerateCodeArt(inputImage, code, cellsWide: imageWidth);//, true, "C:\\Users\\Tex\\OneDrive\\Desktop\\SillyNikitaThingy.txt");

            Console.WriteLine(imagedCode);
            //Generate output
            ProjectBuilder.BuildProject(outputCSPROJ, imagedCode);

        }
    }
}