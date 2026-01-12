using C_Obfuscation;
using DonutMaker;
namespace ObfuscatingTool
{
    internal class Program
    {
        static void Main(string[] args)
        {

            List<string> allSourceFiles = InputTool.GetCodeFilePaths(@"C:\Users\test\Documents\Github\Obfuscator\TestCodeCSharp\BST\TestCode.csproj");
            string code = Obfuscator.ObfuscateProject(allSourceFiles);
            Console.WriteLine(code);
            string imagedCode = TextFormater.GenerateCodeArt(@"c:\\Users\\test\\Documents\\Github\\Obfuscator\\DonutMaker\\TestProject\\TestImages\\cat.png", code, true, @"../../../output.cs");
            Console.WriteLine(imagedCode);




            //Console.WriteLine("Obfuscation Tool Started");
            //Console.WriteLine("Enter csproj file path:");
            //string inputCSPROJ = Console.ReadLine()!;
            //Console.WriteLine("Enter image file path:");
            //string inputImage = Console.ReadLine()!;
            //Console.WriteLine("Enter output csproj file path:");
            //string outputCSPROJ = Console.ReadLine()!;

            //List<string> allSourceFiles = InputTool.GetCodeFilePaths(inputCSPROJ);
            //string code = Obfuscator.ObfuscateProject(allSourceFiles);
            //string imagedCode = TextFormater.GenerateCodeArt(inputImage, code, true, "C:\\Users\\Tex\\OneDrive\\Desktop\\SillyNikitaThingy.txt");
            ////Console.WriteLine(imagedCode);
            //ProjectBuilder.BuildProject(outputCSPROJ, imagedCode);


            //string input = "C:\\Users\\timbr\\source\\repos\\Obfuscator\\TestCode\\Input.cs";
            //string code = Obfuscator.GetObfuscatedNode(input).NormalizeWhitespace().ToFullString();

            //string outputPath = "C:\\Users\\timbr\\source\\repos\\Obfuscator\\TestCode\\Output.cs";
            //File.WriteAllText(outputPath, code);

            //Console.WriteLine(code);

            //string deobfuscatedCode = DeObfuscator.GetDeObfuscatedCode(code);
        }
    }
}

