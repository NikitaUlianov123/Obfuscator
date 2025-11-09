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

            string input = File.ReadAllText(@"../../../TestRobotC/MathQuiz.txt");
            string[] split = TextFormat.SplitPreprocessor(input);
            string code = TextFormat.MakeOneLine(TextFormat.RemoveComments(split[1]));
            string formatted = TextFormat.ConvertToAscii(@"../../../TestImages/Mountain.png", code.Length);
            Console.Write(formatted);
            Console.WriteLine(code);


            File.WriteAllText(@"../../../TestOutput/Mountain.png", split[0] + formatted + code);

            ;


        }
    }
}