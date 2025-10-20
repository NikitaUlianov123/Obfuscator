using Obfuscator.Tokens;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuscator
{
    public abstract class ASTNodeBase
    {
        protected ASTNodeBase(int beginning, int end)
        {
            RootToken = new NONEToken("", 0);
            Beginning = beginning;
            End = end;
        }

        public int Beginning { get; }
        public int End { get; }
        public AbstactToken RootToken { get; protected set; }
    }

}
