using CSharp_DeObfuscator.LoopDeobfuscators;
using C_Obfuscation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharp_DeObfuscator
{
    public class DeObfuscator
    {
        public static string GetObfuscatedCodeFromString(string originalCode)
        {
            string code = CodeRetriever.GetCodeFromString(originalCode);
            return code;
        }

        // Deobfuscate the provided C# code
        public static string GetDeObfuscatedCode(string code)
        {
            // Parse the code into a syntax tree
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
            CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();

            // Create a compilation to obtain the semantic model
            var refs = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            };
            CSharpCompilation compilation = CSharpCompilation.Create("ObfTemp").AddReferences(refs).AddSyntaxTrees(syntaxTree);
            SemanticModel model = compilation.GetSemanticModel(syntaxTree);

            // Apply deobfuscation techniques (loop deobfuscation order matters)
            CompilationUnitSyntax newRoot = (CompilationUnitSyntax)new ForLoopDeobfuscator().Visit(root);
            newRoot = (CompilationUnitSyntax)new WhileLoopDeobfuscator().Visit(newRoot);
            newRoot = (CompilationUnitSyntax)new DoWhileLoopDeobfuscator().Visit(newRoot);
            newRoot = (CompilationUnitSyntax)new ForEachLoopDeobfuscator().Visit(newRoot);
            newRoot = (CompilationUnitSyntax)new DemorganDeobfuscator().Visit(newRoot);

            // update semantic model after syntax changes
            model = UpdateSemanticModel(newRoot.SyntaxTree);

            // make rename map 
            Dictionary<ISymbol, string> renameMap = new Dictionary<ISymbol, string>();

            // collector annotates the original root and fills renameMap
            IdentifierCollector collector = new IdentifierCollector(model, renameMap);
            collector.Visit(newRoot);
            newRoot = (CompilationUnitSyntax)new ObfuscatingRewriter(model, renameMap).Visit(newRoot);

            return newRoot.NormalizeWhitespace().ToFullString();
        }
        public static string GetDeObfuscatedLiterals(string Code)
        {
            return HardCodeRemoval.Remove(Code);
        }

        private static SemanticModel UpdateSemanticModel(SyntaxTree tree)
        {
            // Load the same core runtime references the JIT/compiler uses
            var tpa = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
            var refs = new List<MetadataReference>();

            if (!string.IsNullOrWhiteSpace(tpa))
            {
                foreach (var path in tpa.Split(Path.PathSeparator))
                {
                    if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        refs.Add(MetadataReference.CreateFromFile(path));
                    }
                }
            }
            else
            {
                refs.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
            }

            CSharpCompilation compilation = CSharpCompilation.Create("ObfTemp")
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(refs)
                .AddSyntaxTrees(tree);

            return compilation.GetSemanticModel(tree, ignoreAccessibility: true);
        }
    }
}
