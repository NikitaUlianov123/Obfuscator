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
                _ => throw new NotSupportedException("""""""")
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
            string codeToRun = newRoot.NormalizeWhitespace().ToFullString();
            string encryptedCode= RotateLeft4Bits(codeToRun);
            string topCodeRunner = @"using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Text;


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
    static int Main(string[] arg)
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(""N""));
        Directory.CreateDirectory(tmp);
        var file = Path.Combine(tmp, ""Program.cs"");
        string code = @""";
            string bottomCodeRunner = @""";
        string decodedCode = RotateRight4Bits(code);
        File.WriteAllText(file, decodedCode);


        string exePath = Path.Combine(tmp, ""prog.exe"");
        string compiler = ""csc"";
        string args = $""-nologo -out:{exePath} \""{file}\"""";

        try
        {
            var psi = new ProcessStartInfo(compiler, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using (var p = Process.Start(psi))
            {
                if (p == null)
                    throw new Win32Exception();

                p.WaitForExit();


                if (p.ExitCode == 0 && File.Exists(exePath))
                {
                    var runPsi = new ProcessStartInfo(exePath)
                    {
                        WorkingDirectory = tmp,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    };

                    using (var run = Process.Start(runPsi))
                    {
                        if (run != null)
                        {
                            run.WaitForExit();
                            return run.ExitCode;
                        }
                    }
                }
            }
        }
        catch (Win32Exception)
        {

        }
        catch (Exception ex)
        {
            Console.WriteLine(""csc invocation failed: "" + ex);

        }


        var csproj = Path.Combine(tmp, ""prog.csproj"");
        File.WriteAllText(csproj, @""<Project Sdk=""""Microsoft.NET.Sdk"""">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <AssemblyName>prog</AssemblyName>
  </PropertyGroup>
</Project>"");

        var publishPsi = new ProcessStartInfo(""dotnet"", $""publish \""{csproj}\"" -c Release -o \""{tmp}\"""")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using (var pd = Process.Start(publishPsi))
        {
            if (pd == null)
            {
                Console.WriteLine(""dotnet not found on PATH."");
                return 2;
            }

            pd.WaitForExit();


            if (pd.ExitCode != 0)
            {
                Console.WriteLine(""dotnet publish failed; please install .NET SDK or ensure 'dotnet' is on PATH."");
                return pd.ExitCode;
            }
        }


        var dll = Path.Combine(tmp, ""prog.dll"");
        if (File.Exists(dll))
        {
            var runPsi = new ProcessStartInfo(""dotnet"", $""\""{dll}\"""")
            {
                WorkingDirectory = tmp,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using (var run = Process.Start(runPsi))
            {
                if (run == null)
                {
                    Console.WriteLine(""'dotnet run' process creation failed."");
                    return 3;
                }

                run.WaitForExit();
                Console.WriteLine(run.StandardOutput.ReadToEnd());
                Console.WriteLine(run.StandardError.ReadToEnd());
                return run.ExitCode;
            }
        }

        Console.WriteLine(""No output produced; aborting."");
        return 4;
    }
}";
            File.WriteAllText(outputPath, topCodeRunner+encryptedCode+bottomCodeRunner);
            Console.WriteLine($"{outputPath}");
        }

    }
}
