using Obfuscator.Tokens;

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
            RootToken = new TERMAILToken("",end);
            terms = childern;
        }
        public override void RenamingVariblePass(StackInterpter currentStack)
        {
            RenamingPassMulti(currentStack, terms);
        }

        public override string CodeGen()
        {
            string output = "";
            for(int i =  0; i < terms.Count; i++)
            {
                output += terms[i].CodeGen();
            }
            return output;
        }
        public override void IRGen(VaribleStack stack, IRCode IRCodeHandler)
        {
            for (int i = 0; i < terms.Count; i++)
            {
                terms[i].IRGen(stack,IRCodeHandler);
            }
        }
    }
}
