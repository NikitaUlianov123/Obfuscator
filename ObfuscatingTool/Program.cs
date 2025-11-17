using C_Obfuscation;

namespace ObfuscatingTool
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string inputCSPROJ= "C:\\Users\\timbr\\source\\repos\\Obfuscator\\TestCode\\TestCode.csproj";
            string inputImage="";
            string outputCSPROJ="";

            List<string> allSourceFiles = InputTool.GetCodeFilePaths(inputCSPROJ);
            string code = Obfuscator.ObfuscateProject(allSourceFiles);

            ProjectBuilder.BuildProject("C:\\Users\\timbr\\Desktop\\ObfuscatedProject", code);
        }
    }
}
