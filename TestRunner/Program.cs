using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace C_Obfuscation
{
    partial class Program
    {
        static void Main(string[] args)
        {
            var inputPath = "C:\\Users\\timbr\\source\\repos\\Obfuscator\\TestCode\\Input.cs";
            var outputPath = "C:\\Users\\timbr\\source\\repos\\Obfuscator\\TestCode\\Output.cs";

            Obfuscator.ObfuscateCSFile(inputPath, outputPath);
        }
    }
}
