using C_Obfuscation;

namespace ObfuscatingTool
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string inputCSPROJ = "C:\\Users\\Tex\\OneDrive\\Documents\\Visual Studio 2022\\Projects\\TestCodeForObfuscation\\BSTTestCode\\BSTTestCode\\BSTTestCode.csproj";
            string inputImage = "";
            string outputCSPROJ = "C:\\Users\\Tex\\OneDrive\\Desktop\\ObfuscatedCode";

            List<string> allSourceFiles = InputTool.GetCodeFilePaths(inputCSPROJ);
            string code = Obfuscator.ObfuscateProject(allSourceFiles);

            ProjectBuilder.BuildProject(outputCSPROJ, code);


            //string input = "C:\\Users\\timbr\\source\\repos\\Obfuscator\\TestCode\\Input.cs";
            //string code = Obfuscator.GetObfuscatedNode(input).NormalizeWhitespace().ToFullString();

            //string outputPath = "C:\\Users\\timbr\\source\\repos\\Obfuscator\\TestCode\\Output.cs";
            //File.WriteAllText(outputPath, code);

            //Console.WriteLine(code);

            //string deobfuscatedCode = DeObfuscator.GetDeObfuscatedCode(code);

        }
    }
}

