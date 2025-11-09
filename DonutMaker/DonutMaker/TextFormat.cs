using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing; // for Resize()

using System.Text;
using System.Text.RegularExpressions;

namespace DonutMaker
{
    public class TextFormat
    {
        public int Width { get; }
        public int Height { get; }
        public bool[,] Mask { get; }

        public bool this[int y, int x] => Mask[y, x];

        public TextFormat(bool[,] mask)
        {
            Mask = mask;
            Height = mask.GetLength(0);
            Width = mask.GetLength(1);
        }

        public static string RemoveComments(string code)
        {
            if (string.IsNullOrEmpty(code))
                return code;

            // Regex:
            // 1. Match // comments (until line end)
            // 2. Match /* ... */ comments (including multiline)
            string pattern = @"(//[^\r\n]*)|(/\*[\s\S]*?\*/)";
            return Regex.Replace(code, pattern, "");
        }

        public static string ConvertToAscii(string path, int width)
        {
            using var image = Image.Load<Rgba32>(path);

            // Maintain aspect ratio
            double ratio = (double)image.Height / image.Width;
            int height = (int)(width * ratio * 0.5); // 0.5 = adjust for font aspect ratio

            // Resize the image
            image.Mutate(x => x.Resize(width, height));

            var sb = new StringBuilder();
            const string chars = "@%#*+=-:. "; // darkest → lightest

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = image[x, y];
                    byte gray = (byte)((pixel.R + pixel.G + pixel.B) / 3);

                    int index = gray * (chars.Length - 1) / 255;
                    sb.Append(chars[chars.Length - 1 - index]); // invert if you want dark=solid
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public static string MakeOneLine(string input)
        { 
            return input.Replace("\r", "").Replace("\n", "").Replace("\t", "");
        }
    }
}