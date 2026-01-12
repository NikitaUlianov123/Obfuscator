using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ObfuscatingTool
{
    internal class ProjectBuilder
    {
        // Builds a new C# project with the obfuscated code
        public static void BuildProject(string outputDir, string obfuscatedCode)
        {            
            string csprojPath = Path.Combine(outputDir, "ObfuscatedProject.csproj");
            CreateProgramCs(outputDir, obfuscatedCode);
            CreateCsproj(csprojPath);
        }

        // Creates a basic .csproj file for the obfuscated project
        private static void CreateCsproj(string path)
        {
            XDocument csproj = new XDocument(
                new XElement("Project",
                    new XAttribute("Sdk", "Microsoft.NET.Sdk"),
                    new XElement("PropertyGroup",
                        new XElement("OutputType", "Exe"),
                        new XElement("TargetFramework", "net8.0"),
                        new XElement("ImplicitUsings", "enable"),
                        new XElement("Nullable", "enable")
                    ),
                    new XElement("ItemGroup",
                        new XElement("PackageReference", new XAttribute("Include", "Microsoft.CodeAnalysis"), new XAttribute("Version", "4.14.0"))
                    )
                )
            );

            csproj.Save(path);
        }

        // Creates the Program.cs file with the obfuscated code
        private static void CreateProgramCs(string outputDir, string obfuscatedCode)
        {
            string programCsPath = Path.Combine(outputDir, "Program.cs");
            File.WriteAllText(programCsPath, obfuscatedCode);
        }
    }
}
