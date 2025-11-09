using C_Obfuscator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_Obfuscation
{
    public static class Obfuscator
    {
        //counts unique names
        public static int nameCounter = 0;
        // generates a new obfuscated name
        public static string NewName() => "a" + (nameCounter++).ToString("X");
        public static void Obfuscate(string inputPath,string outputPath)
        {
            //get code and parse
            var code = File.ReadAllText(inputPath);
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetCompilationUnitRoot();

            // build a model so we can avoid renaming public symbols or those used externally
            var mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            var compilation = CSharpCompilation.Create("ObfTemp").AddReferences(mscorlib).AddSyntaxTrees(tree);

            var model = compilation.GetSemanticModel(tree);

            // makes rename map
            var renameMap = new Dictionary<ISymbol, string>(SymbolEqualityComparer.Default);

            // collect identifiers to rename
            var collector = new IdentifierCollector(model, renameMap);
            collector.Visit(root);

            // rename variables and replace literals
            var rewriter = new ObfuscatingRewriter(model, renameMap);
            var newRoot = (CompilationUnitSyntax)rewriter.Visit(root);

            var demorgan = new DemorganRewriter();
            newRoot = (CompilationUnitSyntax)demorgan.Visit(newRoot);


            // inject helper mehtod for decoding strings if needed
            if (rewriter.EncodedStringsUsed)
            {
                var helper = SyntaxFactory.ParseMemberDeclaration(@"
                             static class __ObfHelpers 
                             {
                                public static string DecodeString(byte[] data) 
                                {
                                    return Encoding.UTF8.GetString(data);
                                }
                             }");
                // add using System.Text; if not present
                if (!newRoot.Usings.Any(u => u.Name.ToString() == "System.Text"))
                {
                    newRoot = newRoot.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Text")));
                }
                newRoot = newRoot.AddMembers(helper);
            }

            //output to file
            File.WriteAllText(outputPath, newRoot.NormalizeWhitespace().ToFullString());
            Console.WriteLine($"{outputPath}");
        }

    }
}
