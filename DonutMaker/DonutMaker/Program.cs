using System;

namespace DonutMaker
{
    class Program
    {
        static void Main()
        {
            Console.SetBufferSize(Console.BufferWidth, 20000);//to prevent it from cutting off the top of large images

            //string input = File.ReadAllText(@"../../../TestRobotC/MathQuiz.txt");
            //string input = File.ReadAllText(@"../../../TestCSharp/ForLoop.txt");
            //string input = File.ReadAllText(@"../../../TestCSharp/Simple.txt");
            string input = File.ReadAllText(@"../../../TestCSharp/LinkedList.txt");

            string result = TextFormater.GenerateCodeArt(@"../../../TestImages/miata.png", input, true, @"../../../TestOutput/Output.txt");

            Console.WriteLine(result);
            Console.WriteLine(result);

            Console.WriteLine("------------------------------------");

            File.WriteAllText(@"../../../TestOutput/DeObfOutput.txt", TextFormater.Deimagify(File.ReadAllText(@"../../../TestOutput/Output.txt")));
        }
    }
}