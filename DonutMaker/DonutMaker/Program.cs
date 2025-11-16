using System;

namespace DonutMaker
{
    class Program
    {
        static void Main()
        {


            //fmt = TextFormat.LoadFromText(@"../../../TestFormats/AlternatingLines.txt");
            //formatted = fmt.SimpleFormatText2(input);
            //Console.WriteLine(formatted);

            //Console.WriteLine("---------------------------------------------------------------------------------------------------------");

            //fmt = TextFormat.LoadFromText(@"../../../TestFormats/Blocks.txt");
            //formatted = fmt.SimpleFormatText2(input);
            //Console.WriteLine(formatted);

            //Console.WriteLine("---------------------------------------------------------------------------------------------------------");

            //fmt = TextFormat.LoadFromText(@"../../../TestFormats/Donut.txt");
            //formatted = fmt.SimpleFormatText2(input);
            //Console.WriteLine(formatted);


            Console.WriteLine("---------------------------------------------------------------------------------------------------------");

            //string input = File.ReadAllText(@"../../../TestRobotC/MathQuiz.txt");
            string input = File.ReadAllText(@"../../../TestCSharp/Program.txt");
            string[] split = TextFormat.SplitPreprocessor(input);
            string code = TextFormat.MakeOneLine(TextFormat.RemoveComments(split[1]));
            TextFormat.GenerateCodeArt2(@"../../../TestImages/cat.png", code, @"../../../TestOutput/Mountain.txt", 70);
            //Console.Write(formatted);
            //Console.WriteLine(code);


            //File.WriteAllText(@"../../../TestOutput/Mountain.txt", split[0] + "\n" + formatted + code);

            ;


        }
    }
}