using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;

namespace ObfuscatingTool
{
    class InputTool
    {
        public static List<string> GetCodeFilePaths(string csprojPath)
        {
            var projectDir = Path.GetDirectoryName(csprojPath)!;
            var doc = XDocument.Load(csprojPath);

            //gets <Compile> nodes
            var compileNodes = new List<string>();
            foreach (var node in doc.Descendants())
            {
                if (node.Name.LocalName == "Compile")
                {
                    var attr = node.Attribute("Include");
                    if (attr != null)
                    {
                        compileNodes.Add(attr.Value);
                    }
                }
            }

            //gets .cs files if no <Compile> nodes found
            if (compileNodes.Count == 0)
            {
                var files = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories);
                foreach (var f in files)
                {
                    // ignore files in bin/ or obj/ directories
                    var rel = Path.GetRelativePath(projectDir, f);
                    var segments = rel.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                    bool skip = false;
                    foreach (var s in segments)
                    {
                        if (s.Equals("bin", StringComparison.OrdinalIgnoreCase) || s.Equals("obj", StringComparison.OrdinalIgnoreCase) || s.StartsWith(".", StringComparison.OrdinalIgnoreCase))
                        {
                            skip = true;
                            break;
                        }
                    }
                    if (skip) continue;

                    // use relative path so later normalization produces an absolute path correctly
                    compileNodes.Add(rel);
                }
            }

            //To absolute paths
            var result = new List<string>();
            foreach (var path in compileNodes)
            {
                result.Add(Path.GetFullPath(Path.Combine(projectDir, path)));
            }

            return result;
        }
    }
}
