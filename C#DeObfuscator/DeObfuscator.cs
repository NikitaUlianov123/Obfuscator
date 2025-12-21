using C_Obfuscation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C_DeObfuscator
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
            Dictionary<string, string> renameMap = new Dictionary<string, string>();

            // collector annotates the original root and fills renameMap
            IdentifierCollector collector = new IdentifierCollector(model, renameMap);
            collector.Visit(root);

            // rewriter runs only on the annotated root and uses only the renameMap
            ObfuscatingRewriter rewriter = new ObfuscatingRewriter(model, renameMap);


            CompilationUnitSyntax newRoot = (CompilationUnitSyntax)rewriter.Visit(root);
            newRoot = (CompilationUnitSyntax)new ForLoopDeobfuscator().Visit(newRoot);
            newRoot = (CompilationUnitSyntax)new WhileLoopDeobfuscator().Visit(newRoot);

            return newRoot.NormalizeWhitespace().ToFullString();
        }
    }
}
