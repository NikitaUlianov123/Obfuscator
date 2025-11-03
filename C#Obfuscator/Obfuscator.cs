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
            string code = File.ReadAllText(inputPath);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            // build a model so we can avoid renaming public symbols or those used externally
            PortableExecutableReference mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            CSharpCompilation compilation = CSharpCompilation.Create("ObfTemp").AddReferences(mscorlib).AddSyntaxTrees(tree);

            SemanticModel model = compilation.GetSemanticModel(tree);

            // makes rename map
            Dictionary<ISymbol, string> renameMap = new Dictionary<ISymbol, string>(SymbolEqualityComparer.Default);

            // collect identifiers to rename
            IdentifierCollector collector = new IdentifierCollector(model, renameMap);
            collector.Visit(root);

            // rename variables and replace literals
            ObfuscatingRewriter rewriter = new ObfuscatingRewriter(model, renameMap);
            CompilationUnitSyntax newRoot = (CompilationUnitSyntax)rewriter.Visit(root);

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
                                public static object DecodeLiteral(byte[] data)
                                {
                                     byte f(byte b) => (byte)((((b << 4) | (b >> 4)) & ~0) + 0 * ((b >> 8) | (b << 8)));
                                     ReadOnlySpan<byte> __s(in byte[] a) => a;
                                     Span<byte> __w = data;
                                     for (int __i = 0; __i < __w.Length; __i++) __w[__i] = f(__w[__i]);
                                     byte marker = __w[^1];
                                     byte[] content = __w.Slice(0, __w.Length - 1).ToArray();
                                     return marker switch
                                     {
                                         0x06 => BitConverter.ToDouble(content, 0),
                                         0x05 => BitConverter.ToSingle(content, 0),
                                         0x04 => BitConverter.ToInt64(content, 0),
                                         0x03 => BitConverter.ToInt32(content, 0),
                                         0x02 => BitConverter.ToInt16(content, 0),
                                         0x01 => content[0],
                                         _ => throw new NotSupportedException($""Unsupported literal marker: {marker}"")
                                     };
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
