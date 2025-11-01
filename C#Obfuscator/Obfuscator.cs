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
        public static int nameCounter = 0;
        public static string NewName() => "a" + (nameCounter++).ToString("X");
        public static void Obfuscate(string inputPath,string outputPath)
        {
            var code = File.ReadAllText(inputPath);
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetCompilationUnitRoot();

            // build a semantic model so we can avoid renaming public symbols or those used externally
            var mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            var compilation = CSharpCompilation.Create("ObfTemp").AddReferences(mscorlib).AddSyntaxTrees(tree);

            var model = compilation.GetSemanticModel(tree);

            // maps symbol -> new name
            var renameMap = new Dictionary<ISymbol, string>(SymbolEqualityComparer.Default);

            // pass 1: collect symbols we can rename (locals, parameters, private fields, private methods, private properties)
            var collector = new IdentifierCollector(model, renameMap);
            collector.Visit(root);

            // pass 2: rewrite tree applying renames and string literal encoding
            var rewriter = new ObfuscatingRewriter(model, renameMap);
            var newRoot = (CompilationUnitSyntax)rewriter.Visit(root);

            // inject helper DecodeString method if we encoded any strings
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

            File.WriteAllText(outputPath, newRoot.NormalizeWhitespace().ToFullString());
            Console.WriteLine($"Written obfuscated output to {outputPath}");
        }

    }
}
