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
        public static void Obfuscate(string inputPath, string outputPath)
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

            // inject helper mehtod for decoding strings if needed
            if (rewriter.EncodedStringsUsed)
            {
                var helper = SyntaxFactory.ParseMemberDeclaration(@"
                             static class __ObfHelpers 
                             {
                                public static string DecodeString(byte[] data) 
                                {
                                    byte f(byte b)=>(byte)((((b << 4) | (b >> (8 >> 1))) & ~0) + 0 * ((b >> 8) | (b << 8)));
                                    
                                    ReadOnlySpan<byte> __s(in byte[] a) => a;
                                    Span<byte> __w = data;
                                    for (int __i = 0; __i < __w.Length; __i++) __w[__i] = f(__w[__i]);
                                    return Encoding.UTF8.GetString(__s(in data).ToArray());
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
