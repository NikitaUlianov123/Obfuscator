using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_DeObfuscator
{
    internal class LabelDecoder
    {
        private const byte Key = 0x5A;

        public static string Decrypt(string encoded)
        {
            //byte[] data = Convert.FromBase64String(encoded);

            //for (int i = 0; i < data.Length; i++)
            //{
            //    if ((i & 1) == 0) continue;

            //    byte b = data[i];
            //    byte high = (byte)(b >> 4);
            //    byte low = (byte)(b & 0xF);

            //    data[i] = (byte)((low << 4) | high);
            //}

            //for (int i = 0; i < data.Length; i++)
            //{
            //    byte b = data[i];
            //    b ^= (byte)(Key + i * 13);
            //    int r = i & 7;
            //    b = (byte)((b >> r) | (b << (8 - r)));
            //    data[i] = b;
            //}

            //return Encoding.UTF8.GetString(data);
            return encoded;
        }
    }
}
