using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharp_DeObfuscator
{
    internal class LabelDecoder
    {
        private const byte Key = 0x5A;

        public static string Decrypt(string encoded)
        {
            try
            {
                // Remove the underscore prefix
                if (encoded.StartsWith("_"))
                {
                    encoded = encoded.Substring(1);
                }

                // Convert hex string back to bytes
                int length = encoded.Length / 2;
                byte[] bytes = new byte[length];

                for (int i = 0; i < length; i++)
                {
                    bytes[i] = Convert.ToByte(encoded.Substring(i * 2, 2), 16);
                }

                // XOR each byte with the key to decrypt
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = (byte)(bytes[i] ^ Key);
                }

                return Encoding.UTF8.GetString(bytes);
            }
            catch (Exception)
            {
                // If decryption fails, return the original encoded string
                return encoded;
            }
        }
    }
}
