using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuscator
{
    public class DeObfuscator
    {
        public static void Deobf(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i+=4)
            {
                byte[] curr = bytes[i..(i+4)];


            }
        }
    }
}