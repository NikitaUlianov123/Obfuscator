using System;
using System.Text;

namespace C_Obfuscation
{
    internal static class LabelEncoder
    {
        private const byte Key = 0x5A;

        public static string Encrypt(string input)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);

            // XOR each byte with the key
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(bytes[i] ^ Key);
            }

            // Convert to hex string for valid identifier
            string hex = BitConverter.ToString(bytes).Replace("-", "");

            // Ensure it starts with underscore (valid C# identifier)
            return "_" + hex;
        }
    }
}