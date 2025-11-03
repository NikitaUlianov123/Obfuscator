using Obfuscator.Tokens;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuscator.ASTNodes
{
    public class ASTNodeMismatch : ASTNodeBase
    {
        public ASTNodeMismatch(int begin, int end, AbstactToken token) : base(begin, end)
        {
            RootToken = token; // TokenType.NONE (or a dedicated token)
        }
        public override void RenamingVariblePass(StackInterpter currentStack)
        {
            throw new NotImplementedException();
        }
    }
}
