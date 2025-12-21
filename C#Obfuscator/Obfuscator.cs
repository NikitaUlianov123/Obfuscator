using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace C_Obfuscation
{
    public static class Obfuscator
    {
        // counts unique names
        public static int nameCounter = 0;
        // holds the last unencoded obfuscated code for debugging
        public static string ObfuscatedCode = "";
        // generates a new obfuscated name
        public static string NewName(string oldname)
        {
            char[] result = new char[oldname.Length];

            for (int i = 0; i < oldname.Length; i++)
            {
                char c = oldname[i];

                // If digit
                if (char.IsDigit(c))
                {
                    int digit = c - '0';
                    int newDigit = (digit + 5) % 9;
                    result[i] = (char)('0' + newDigit);
                }
                // If lowercase letter
                else if (c >= 'a' && c <= 'z')
                {
                    result[i] = (char)('a' + (c - 'a' + 13) % 26);
                }
                // If uppercase letter
                else if (c >= 'A' && c <= 'Z')
                {
                    result[i] = (char)('A' + (c - 'A' + 13) % 26);
                }
                // Anything else
                else
                {
                    result[i] = c;
                }
            }

            return new string(result);
        }
        

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
        static string InsertSeparators(string input, char separator = '|')
        {
            if (string.IsNullOrEmpty(input))
                return input;
            return string.Join(separator, input.ToCharArray());
        }


        public static string ObfuscateProject(List<string> codeFilePaths)
        {
            List<CompilationUnitSyntax> obfuscatedUnits = new List<CompilationUnitSyntax>();
            foreach (var path in codeFilePaths)
            {
                CompilationUnitSyntax obfUnit = GetObfuscatedNode(path);
                obfuscatedUnits.Add(obfUnit);
            }

            // Merge units
            CompilationUnitSyntax combinedUnit = CombineCompilationUnits(obfuscatedUnits.ToArray());

            // Ensure the helper class exists in the combined unit (so obfuscated user code can call _O)
            if (!combinedUnit.Members.OfType<ClassDeclarationSyntax>().Any(c => c.Identifier.Text == "_O"))
            {
                combinedUnit = combinedUnit.AddMembers(HelperFuncs());
            }

            // userCode is the obfuscated user program (NOT the runner)
            string userCode = combinedUnit.NormalizeWhitespace().ToFullString();
            ObfuscatedCode = userCode; // keep plain obfuscated code for debugging if needed

            // encode only the user code and insert into the runner template
            //string encodedUserCode = RotateLeft4Bits(userCode);
            string finalRunner = GenerateObfuscatedHelper(userCode);

            return finalRunner;
        }

        static CompilationUnitSyntax GetObfuscatedNode(string filepath)
        {
            // get code and parse
            string code = File.ReadAllText(filepath);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            // build a model so we can avoid renaming public symbols or those used externally           
            var refs = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            };

            CSharpCompilation compilation = CSharpCompilation.Create("ObfTemp")
                .AddReferences(refs)
                .AddSyntaxTrees(tree);

            SemanticModel model = compilation.GetSemanticModel(tree);

            // make rename map 
            Dictionary<string, string> renameMap = new Dictionary<string, string>();

            // collector annotates the original root and fills renameMap
            IdentifierCollector collector = new IdentifierCollector(model, renameMap);
            collector.Visit(root);

            // rewriter runs only on the annotated root and uses only the renameMap
            ObfuscatingRewriter rewriter = new ObfuscatingRewriter(model, renameMap);
            DemorganRewriter demorganRewriter = new DemorganRewriter();
            LoopRewriter loopRewriter = new LoopRewriter(model);
            
            CompilationUnitSyntax newRoot = (CompilationUnitSyntax)loopRewriter.Visit(root);

            newRoot = (CompilationUnitSyntax)demorganRewriter.Visit(newRoot);

            model = UpdateSemanticModel(newRoot.SyntaxTree, newRoot);
            rewriter = new ObfuscatingRewriter(model, renameMap);

            newRoot = (CompilationUnitSyntax)rewriter.Visit(newRoot);
            return newRoot;
        }

        private static SemanticModel UpdateSemanticModel(SyntaxTree tree, SyntaxNode newRoot)
        {
            
            var refs = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            };

            CSharpCompilation compilation = CSharpCompilation.Create("ObfTemp")
                .AddReferences(refs)
                .AddSyntaxTrees(tree);
                
            return compilation.GetSemanticModel(tree);
        }

        static string GenerateObfuscatedHelper(string encodedUserCode)
        {
            // we will append encoded user code into that spot and then append bottomCodeRunner
            var sb = new StringBuilder();
            sb.Append(topCodeRunner);
            sb.Append(ChunkToVerbatimConcat(encodedUserCode));
            sb.Append(bottomCodeRunner);
            return sb.ToString();
        }
        public static string ChunkToVerbatimConcat(string input, int chunkSize = 10, char separator = '|')
        {
            string separated = InsertSeparators(input, separator);
            string rotated = RotateLeft4Bits(separated);
            StringInfo strInfo = new StringInfo(rotated);
            var parts = new List<string>();
            for (int i = 0; i < strInfo.LengthInTextElements; i += chunkSize)
            {
                int len = Math.Min(chunkSize, strInfo.LengthInTextElements - i);
                string chunk = strInfo.SubstringByTextElements(i, len);
                string escaped = chunk.Replace("\"", "\"\"");
                parts.Add($@"RotateRight4Bits(@""{escaped}"")");
            }
            return "string source = " + string.Join(" + ", parts) + ";";
        }

        static CompilationUnitSyntax CombineCompilationUnits(CompilationUnitSyntax[] units)
        {
            //get all usings, members, externs, attributes
            var usings = new List<UsingDirectiveSyntax>();
            var members = new List<MemberDeclarationSyntax>();
            var externs = new List<ExternAliasDirectiveSyntax>();
            var attributes = new List<AttributeListSyntax>();

            //combine
            foreach (var unit in units)
            {
                usings.AddRange(unit.Usings);
                members.AddRange(unit.Members);
                externs.AddRange(unit.Externs);
                attributes.AddRange(unit.AttributeLists);
            }

            //ensure required usings exist
            var requiredUsings = new[]
            {
        "System",
        "System.Text",
        "System.Linq",
        "System.Collections.Generic"
    };
            // add any missing required usings
            foreach (var u in requiredUsings)
            {
                if (!usings.Any(e => e.Name.ToString() == u))
                {
                    usings.Add(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(u)));
                }
            }
            //create new compilation unit
            return SyntaxFactory.CompilationUnit()
                .WithUsings(SyntaxFactory.List(usings))
                .WithExterns(SyntaxFactory.List(externs))
                .WithAttributeLists(SyntaxFactory.List(attributes))
                .WithMembers(SyntaxFactory.List(members))
                .NormalizeWhitespace();
        }


        static MemberDeclarationSyntax HelperFuncs()
        {
            // insert helper functions
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
                _ => throw new NotSupportedException()
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


        // we will append encoded user code into that spot and then append bottomCodeRunner
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
    static string RemoveInsertedSeparators(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var sb = new StringBuilder((input.Length + 1) / 2);

        for (int i = 0; i < input.Length; i += 2)
            sb.Append(input[i]);

        return sb.ToString();
    }
    static int Main(string[] args)
    {
        // Source to compile (same program you used previously)
        ";
        static string bottomCodeRunner = @"

        // Parse
        var tree = CSharpSyntaxTree.ParseText(RemoveInsertedSeparators(source));

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
