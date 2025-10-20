using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuscator
{
    public class ASTNodeTermial : ASTNodeBase
    {
        public List<ASTNodeBase> terms { get; set; }
        public ASTNodeTermial(int beginning, int end, List<ASTNodeBase> childern) : base(beginning, end)
        {
            terms = childern;
        }
    }
}
