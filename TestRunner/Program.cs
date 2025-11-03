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


            var inputPath = "C:\\Users\\Tex\\OneDrive\\Documents\\Visual Studio 2022\\Projects\\Obfuscator\\TestCode\\Input.cs";
            var outputPath = "C:\\Users\\Tex\\OneDrive\\Documents\\Visual Studio 2022\\Projects\\Obfuscator\\TestCode\\Output.cs";

            Obfuscator.Obfuscate(inputPath, outputPath);
        }
    }
}
