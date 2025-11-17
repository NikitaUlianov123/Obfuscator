using C_Obfuscation;

namespace ObfuscatingTool
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string inputCSPROJ= "C:\\Users\\Tex\\OneDrive\\Documents\\Visual Studio 2022\\Projects\\Obfuscator\\C#Obfuscator\\C#Obfuscator.csproj";
            string inputImage="";
            string outputCSPROJ="";

            List<string> allSourceFiles=InputTool.GetCodeFilePaths(inputCSPROJ);
            string code = Obfuscator.ObfuscateProject(allSourceFiles);
            

        }
    }
}

