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
            Console.WriteLine(code);
            return GetDeObfuscatedCode(code);
        }
        public static string GetDeObfuscatedCode(string code)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
            CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();

            CompilationUnitSyntax newRoot = (CompilationUnitSyntax)new ForLoopDeobfuscator().Visit(root);
            newRoot = (CompilationUnitSyntax)new WhileLoopDeobfuscator().Visit(newRoot);
            return newRoot.NormalizeWhitespace().ToFullString();
        }
    }
}
