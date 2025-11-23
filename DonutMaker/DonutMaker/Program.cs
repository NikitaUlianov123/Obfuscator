using System;

namespace DonutMaker
{
    class Program
    {
        static void Main()
        {
            //string input = File.ReadAllText(@"../../../TestRobotC/MathQuiz.txt");
            string input = File.ReadAllText(@"../../../TestCSharp/Program.txt");

            Console.WriteLine(TextFormater.GenerateCodeArt(@"../../../TestImages/miata.png", input, true, @"../../../TestOutput/Mountain.txt"));
        }
    }
}