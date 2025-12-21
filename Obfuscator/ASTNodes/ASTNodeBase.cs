using Obfuscator.Tokens;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Obfuscator
{
    public class VaribleStack
    {
        List<VaribleNode> varibleNodes;
        public VaribleStack()
        { 
            varibleNodes = new List<VaribleNode>();
        }
        public void addScope()
        {
            varibleNodes.Add(new VaribleNode());
        }
        public int findVaribles(string name)
        {
            for(int i = 0; i < varibleNodes.Count; i++)
            {
                if(varibleNodes[i].variblesToNumber.ContainsKey(name))
                {
                    return varibleNodes[i].variblesToNumber[name];
                }
            }
            return -1;
        }
        public void addVarible(string name, )
        {

        }
    }
    public class VaribleNode
    {
        public Dictionary<string, int> variblesToNumber;
        public VaribleNode()
        {
            variblesToNumber = new Dictionary<string, int>();
        }

    }
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

        public abstract string CodeGen();

        public abstract string IRGen(VaribleStack stack);

        
        public abstract void RenamingVariblePass(StackInterpter currentStack); 

        public void RenamingPassMulti(StackInterpter currentStack, List<ASTNodeBase> list)
        {
            for(int i = 0; i < list.Count; i++)
            {
                list[i].RenamingVariblePass(currentStack);
            }
        }
    }

}
