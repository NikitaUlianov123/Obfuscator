using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing; // for Resize()

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

        public static TextFormat LoadFromText(string path)
        {
            var lines = File.ReadAllLines(path)
                            .Where(l => !string.IsNullOrWhiteSpace(l))
                            .ToArray();

            int height = lines.Length;
            int width = lines[0].Length;

            var mask = new bool[height, width];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    mask[y, x] = lines[y][x] == '1' || lines[y][x] == '#';
                }
            }

            return new TextFormat(mask);
        }

        public static TextFormat LoadFromImage(string path, int width, int height, byte threshold = 128)
        {
            using var image = Image.Load<Rgba32>(path);
            image.Mutate(x => x.Resize(width, height));

            bool[,] mask = new bool[height, width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var pixel = image[x, y];
                    byte gray = (byte)((pixel.R + pixel.G + pixel.B) / 3);
                    mask[y, x] = gray < threshold; // dark = filled
                }
            }
            return new TextFormat(mask);
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
            return input.Replace("\r", " ").Replace("\n", " ");
        }


        public string SimpleFormatText(string text)
        {
            var result = new StringBuilder();
            int i = 0;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (Mask[y, x])
                    {
                        result.Append(text[i % text.Length]);
                        i++;
                    }
                    else
                    {
                        result.Append(' ');
                    }
                }
                result.AppendLine();
            }

            return result.ToString();
        }

        public string SimpleFormatText2(string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("Input text must not be empty.", nameof(text));

            // Split input text into words including spaces (keep spaces)
            var words = text.Split(new[] { ' ' }, StringSplitOptions.None);
            int wordIndex = 0;

            var result = new StringBuilder();

            for (int y = 0; y < Height; y++)
            {
                int x = 0;
                while (x < Width)
                {
                    if (!Mask[y, x])
                    {
                        result.Append(' ');
                        x++;
                        continue;
                    }

                    // Find how many consecutive filled cells remain in this row
                    int startX = x;
                    while (x < Width && Mask[y, x]) x++;
                    int available = x - startX;

                    // Place as many words as fit in the available space
                    var lineBuilder = new StringBuilder();
                    while (lineBuilder.Length < available)
                    {
                        string word = words[wordIndex % words.Length];
                        bool isSpace = string.IsNullOrEmpty(word); // preserves spaces

                        int needed = isSpace ? 1 : word.Length;

                        if (needed > available - lineBuilder.Length)
                            break; // word doesn't fit, move to next row

                        if (!isSpace)
                        {
                            lineBuilder.Append(word);
                        }
                        else
                        {
                            lineBuilder.Append(' ');
                        }

                        if (!isSpace)
                            lineBuilder.Append(' '); // preserve spacing between words

                        wordIndex++;
                    }

                    // Trim to exactly available cells
                    string finalSegment = lineBuilder.ToString();
                    if (finalSegment.Length > available)
                        finalSegment = finalSegment.Substring(0, available);

                    // Append to result
                    result.Append(finalSegment);

                    // Already moved x to the next 0
                }
                result.AppendLine();
            }

            return result.ToString();
        }
    }
}