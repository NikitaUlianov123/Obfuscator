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
        public static string GetDeObfuscatedCode(string code)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
            CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();

            var refs = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            };
            CSharpCompilation compilation = CSharpCompilation.Create("ObfTemp")
                .AddReferences(refs)
                .AddSyntaxTrees(syntaxTree);
            SemanticModel model = compilation.GetSemanticModel(syntaxTree);
            // make rename map 
            Dictionary<ISymbol, string> renameMap = new Dictionary<ISymbol, string>();

            // collector annotates the original root and fills renameMap
            IdentifierCollector collector = new IdentifierCollector(model, renameMap);
            collector.Visit(root);

            // rewriter runs only on the annotated root and uses only the renameMap
            //ObfuscatingRewriter rewriter = new ObfuscatingRewriter(model, renameMap);


            CompilationUnitSyntax newRoot = /*(CompilationUnitSyntax)rewriter.Visit*/(root);
            newRoot = (CompilationUnitSyntax)new ForLoopDeobfuscator().Visit(newRoot);
            newRoot = (CompilationUnitSyntax)new WhileLoopDeobfuscator().Visit(newRoot);
            newRoot = (CompilationUnitSyntax)new DoWhileLoopDeobfuscator().Visit(newRoot);
            newRoot = (CompilationUnitSyntax)new ForEachLoopDeobfuscator().Visit(newRoot);
            newRoot = (CompilationUnitSyntax)new DemorganDeobfuscator().Visit(newRoot);

            return newRoot.NormalizeWhitespace().ToFullString();
        }
        public static string GetDeObfuscatedLiterals(string Code)
        {
            return HardCodeRemoval.Remove(Code);
        }
    }
}
