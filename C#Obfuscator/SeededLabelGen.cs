using System;
using System.Text;

namespace C_Obfuscation
{
    public class SeededLabelGen
    {
        private readonly Random random;
        private readonly int labelCount = 0;

        public SeededLabelGen(int seed) => random = new Random(seed);

        public string NextLabel(string prefix = "__obf")
        {
            const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var sb = new StringBuilder();
            for (int i = 0; i < 24; i++)
            {
                sb.Append(alphabet[random.Next(alphabet.Length)]);
            }
            return $"{prefix}_{sb}";
        }
    }

}