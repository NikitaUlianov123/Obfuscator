using C_Obfuscation;

namespace ObfuscatingTool
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string inputCSPROJ = "C:\\Users\\timbr\\source\\repos\\SimpleConsoleApp\\SimpleConsoleApp\\SimpleConsoleApp.csproj";
            string inputImage = "";
            string outputCSPROJ = "C:\\Users\\timbr\\Desktop\\ObfuscatedProject";

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

