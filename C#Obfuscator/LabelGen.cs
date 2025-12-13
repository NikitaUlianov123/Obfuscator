using System;
using System.Text;

namespace C_Obfuscation
{
    public static class LabelGen
    {
        private const byte Key = 0x5A; 

        public static string Encrypt(string input)
        {
            //byte[] data = Encoding.UTF8.GetBytes(input);

            //for (int i = 0; i < data.Length; i++)
            //{
            //    byte b = data[i];
            //    int r = i & 7;
            //    b = (byte)((b << r) | (b >> (8 - r)));
            //    b ^= (byte)(Key + i * 13);
            //    data[i] = b;
            //}

            //for (int i = 0; i < data.Length; i++)
            //{
            //    if ((i & 1) == 0) continue;

            //    byte b = data[i];
            //    byte high = (byte)(b >> 4);
            //    byte low = (byte)(b & 0xF);

            //    data[i] = (byte)((low << 4) | high);
            //}

            //return Convert.ToBase64String(data);
            return input;
        }
    }
}