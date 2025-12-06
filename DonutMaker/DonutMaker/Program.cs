using System;

namespace DonutMaker
{
    class Program
    {
        static void Main()
        {
            //string input = File.ReadAllText(@"../../../TestRobotC/MathQuiz.txt");
            //string input = File.ReadAllText(@"../../../TestCSharp/ForLoop.txt");
            //string input = File.ReadAllText(@"../../../TestCSharp/Simple.txt");
            string input = File.ReadAllText(@"../../../TestCSharp/LinkedList.txt");

            Console.WriteLine(TextFormater.GenerateCodeArt(@"../../../TestImages/miata.png", input, true, @"../../../TestOutput/Output.txt"));

            Console.WriteLine("------------------------------------");

            File.WriteAllText(@"../../../TestOutput/DeObfOutput.txt", TextFormater.Deimagify(File.ReadAllText(@"../../../TestOutput/Output.txt")));
        }
    }
}