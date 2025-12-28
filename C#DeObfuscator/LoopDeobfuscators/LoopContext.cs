using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharp_DeObfuscator.LoopDeobfuscators
{
    public sealed record LoopContext(string ContinueLabel, string BreakLabel);
}
