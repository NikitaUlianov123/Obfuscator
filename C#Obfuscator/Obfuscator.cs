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
            newRoot = (CompilationUnitSyntax)new LoopRewriter().Visit(newRoot);
            newRoot = (CompilationUnitSyntax)new DemorganRewriter().Visit(newRoot);


            // inject helper mehtod for decoding strings if needed
            if (rewriter.EncodedStringsUsed)
            {
                var helper = SyntaxFactory.ParseMemberDeclaration(@"static class _O
{
    private static byte R(byte x)
    {
        int t = x;
        return (byte)(((t << 4) | (t >> 4)) & 0xFF);
    }

    public static string D(byte[] a)
    {
        var b = (byte[])a.Clone();
        for (int i = 0; i < b.Length; i++) b[i] = R(b[i]);
        return Encoding.UTF8.GetString(b);
    }

    public static object L(byte[] z)
    {
        var q = (byte[])z.Clone();
        Func<byte, byte> f = x => R(x);
        for (int i = 0; i < q.Length; i++) q[i] = f(q[i]);
        if (q.Length == 0) throw new ArgumentException(new string(new[] { 'E', 'm', 'p', 't', 'y', ' ', 'L', 'i', 't', 'e', 'r', 'a', 'l' }));
        byte k = q[^1];
        byte[] c = new byte[q.Length - 1];
        if (c.Length > 0) Array.Copy(q, c, c.Length);

        return ((Func<byte, object>)(m => m switch
        {
            0x06 => BitConverter.ToDouble(c, 0),
            0x05 => BitConverter.ToSingle(c, 0),
            0x04 => BitConverter.ToInt64(c, 0),
            0x03 => BitConverter.ToInt32(c, 0),
            0x02 => BitConverter.ToInt16(c, 0),
            0x01 => c.Length > 0 ? (object)c[0] : (object)0,
            0x30 => BitConverter.ToInt32(c, 0),
            0x60 => c.Length >= 8 ? BitConverter.ToDouble(c, 0)
                     : (c.Length >= 4 ? BitConverter.ToInt32(c, 0) : (object)c),
            _ => ((Func<int, object>)(len => len switch
            {
                8 => BitConverter.ToDouble(c, 0),
                4 => BitConverter.ToInt32(c, 0),
                2 => BitConverter.ToInt16(c, 0),
                1 => c[0],
                0 => 0,
                _ => throw new NotSupportedException($""??? {len}"")
            }))(c.Length)
        }))(k);
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


