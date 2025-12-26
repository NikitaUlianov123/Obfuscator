using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_DeObfuscator
{
    public sealed record LoopContext(string ContinueLabel, string BreakLabel);
}
