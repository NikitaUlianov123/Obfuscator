using C_Obfuscation;

namespace ObfuscatingTool
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string inputCSPROJ= "C:\\Users\\Tex\\OneDrive\\Documents\\Visual Studio 2022\\Projects\\Obfuscator\\TestCode\\TestCode.csproj";
            string inputImage="";
            string outputCSPROJ="";

            List<string> allSourceFiles = InputTool.GetCodeFilePaths(inputCSPROJ);
            string code = Obfuscator.ObfuscateProject(allSourceFiles);

            ProjectBuilder.BuildProject("C:\\Users\\Tex\\OneDrive\\Desktop", code);
        }
    }
}

