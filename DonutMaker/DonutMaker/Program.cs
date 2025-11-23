using System;

namespace DonutMaker
{
    class Program
    {
        static void Main()
        {
            //string input = File.ReadAllText(@"../../../TestRobotC/MathQuiz.txt");
            //string input = File.ReadAllText(@"../../../TestCSharp/Program.txt");
            string input = File.ReadAllText(@"../../../TestCSharp/Simple.txt");

            Console.WriteLine(TextFormater.GenerateCodeArt(@"../../../TestImages/miata.png", input, true, @"../../../TestOutput/Output.txt"));

            Console.WriteLine("------------------------------------");

            Console.WriteLine(TextFormater.CondenseAndFormatCSharp(TextFormater.RemoveComments(File.ReadAllText(@"../../../TestOutput/Output.txt"))));
        }
    }
}