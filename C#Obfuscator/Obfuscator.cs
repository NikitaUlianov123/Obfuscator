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
        // rotates bits left by 4
        static string RotateLeft4Bits(string input)
        {
            char[] result = new char[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                byte b = (byte)input[i];
                byte rotated = (byte)(((b << 4) | (b >> 4)) & 0xFF);
                result[i] = (char)rotated;
            }
            return new string(result);
        }
        public static string ObfuscateProject(List<string> codeFilePaths)
        {
            List<CompilationUnitSyntax> obfuscatedUnits = new List<CompilationUnitSyntax>();
            foreach (var path in codeFilePaths)
            {
                CompilationUnitSyntax obfUnit = GetObfuscatedNode(path);
                obfuscatedUnits.Add(obfUnit);
            }
            CompilationUnitSyntax lastUnit = obfuscatedUnits.Last();           
            if (!lastUnit.Usings.Any(u => u.Name.ToString() == "System.Text"))
            {
                lastUnit = lastUnit.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Text")));
            }
            lastUnit = lastUnit.AddMembers(HelperFuncs());
            //fix
            CompilationUnitSyntax combinedUnit = CombineCompilationUnits(obfuscatedUnits.ToArray());
            string finalCode = combinedUnit.NormalizeWhitespace().ToFullString();
            
            string obfuscatedCode = GenerateObfuscatedHelper(finalCode);
            return obfuscatedCode;
        }

        static CompilationUnitSyntax GetObfuscatedNode(string filepath)
        {
            //get code and parse
            string code = File.ReadAllText(filepath);
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
            return newRoot;
        }       
         static string GenerateObfuscatedHelper(string code)
        {
            StringBuilder finalCode = new StringBuilder();
            finalCode.Append(topCodeRunner);
            finalCode.Append(RotateLeft4Bits(code));
            finalCode.Append(bottomCodeRunner);
            return finalCode.ToString();
        }
         static CompilationUnitSyntax CombineCompilationUnits(CompilationUnitSyntax[] units)
        {
            var usings = new List<UsingDirectiveSyntax>();
            var members = new List<MemberDeclarationSyntax>();
            var externs = new List<ExternAliasDirectiveSyntax>();
            var attributes = new List<AttributeListSyntax>();

            foreach (var unit in units)
            {
                foreach (var u in unit.Usings)
                    usings.Add(u);

                foreach (var m in unit.Members)
                    members.Add(m);

                foreach (var e in unit.Externs)
                    externs.Add(e);

            // collect identifiers to rename
            IdentifierCollector collector = new IdentifierCollector(model, renameMap);
            collector.Visit(root);

            // rename variables and replace literals
            ObfuscatingRewriter rewriter = new ObfuscatingRewriter(model, renameMap);
            CompilationUnitSyntax newRoot = (CompilationUnitSyntax)rewriter.Visit(root);
            
            // add using System.Text; if not present
            if (!newRoot.Usings.Any(u => u.Name.ToString() == "System.Text"))
            {
                newRoot = newRoot.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Text")));
            }

            return SyntaxFactory.CompilationUnit()
                .WithUsings(SyntaxFactory.List(usings))
                .WithExterns(SyntaxFactory.List(externs))
                .WithAttributeLists(SyntaxFactory.List(attributes))
                .WithMembers(SyntaxFactory.List(members))
                .NormalizeWhitespace();
        }
        static MemberDeclarationSyntax HelperFuncs()
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
                _ => throw new NotSupportedException("""""""")
            }))(c.Length)
        }))(k);
    }
}");
            if (helper == null)
            {
                return null;
            }
            return helper;
        }
        static string topCodeRunner = @"using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

class CscRunner
{
    static string RotateRight4Bits(string input)
    {
        char[] result = new char[input.Length];
        for (int i = 0; i < input.Length; i++)
        {
            byte b = (byte)input[i];
            byte rotated = (byte)(((b >> 4) | (b << 4)) & 0xFF);
            result[i] = (char)rotated;
        }
        return new string(result);
    }
    static int Main(string[] args)
    {
        // Source to compile (same program you used previously)
        string source = @""";
        static string bottomCodeRunner = @""";

        // Parse
        var tree = CSharpSyntaxTree.ParseText(RotateRight4Bits(source));

        // Create metadata references from the runtime's TPA list (robust for .NET Core / .NET 5+)
        var tpa = ((string)AppContext.GetData(""TRUSTED_PLATFORM_ASSEMBLIES""))?.Split(Path.PathSeparator) ?? Array.Empty<string>();
        var refs = tpa
            .Where(p => p != null && p.EndsWith("".dll"", StringComparison.OrdinalIgnoreCase))
            .Select(p => MetadataReference.CreateFromFile(p))
            .Cast<MetadataReference>()
            .ToList();

        // Always include the currently loaded assemblies that may not be in TPA
        refs.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        refs.Add(MetadataReference.CreateFromFile(typeof(Console).Assembly.Location));
        refs = refs.DistinctBy(r => ((PortableExecutableReference)r).FilePath).ToList();

        // Create compilation
        var comp = CSharpCompilation.Create(
            assemblyName: ""InMemoryProg"",
            syntaxTrees: new[] { tree },
            references: refs,
            options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        using var ms = new MemoryStream();
        var result = comp.Emit(ms);

        if (!result.Success)
        {
            Console.WriteLine(""Compilation failed:"");
            foreach (var diag in result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                Console.WriteLine(diag.ToString());
            return 1;
        }

        ms.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(ms.ToArray());

        // Capture Console output
        var originalOut = Console.Out;
        var sw = new StringWriter();
        Console.SetOut(sw);

        try
        {
            var entry = assembly.EntryPoint ?? assembly.GetType(""Program"")?.GetMethod(""Main"", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (entry == null) { Console.SetOut(originalOut); Console.WriteLine(""No entry point.""); return 2; }

            object[] invokeArgs = entry.GetParameters().Length == 0 ? null : new object[] { Array.Empty<string>() };
            var ret = entry.Invoke(null, invokeArgs);
            // If Main returned int, you could return it; otherwise return 0
            if (ret is int exitCode) return exitCode;
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.WriteLine(sw.ToString());
        }

        return 0;
    }
}";
    }


}


