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
        public Dictionary<TokenType, string> TypeToCommand = new Dictionary<TokenType, string>();
        List<VaribleNode> varibleNodes;

        public VaribleStack()
        { 
            varibleNodes = new List<VaribleNode>();
            addScope();
        }
        public void addScope()
        {

            varibleNodes.Add(new VaribleNode());
        }
        public Varible findVaribles(string name)
        {
            for(int i = 0; i < varibleNodes.Count; i++)
            {
                if(varibleNodes[i].variblesToInfo.ContainsKey(name))
                {
                    return varibleNodes[i].variblesToInfo[name];
                }
            }
            return new Varible(-1, TokenType.ERROR);
        }
        public void addVarible(string name, TokenType token)
        {
            varibleNodes[varibleNodes.Count-1].variblesToInfo.Add(name , (new Varible(varibleNodes[varibleNodes.Count-1].currentIndex, token)));
            varibleNodes[varibleNodes.Count - 1].currentIndex++;
        }
    }
    public class Varible
    { 
        public int variblesToNumber;
        public TokenType varibleType;
        public Varible(int variblesToNumber, TokenType varibleType)
        {
            this.variblesToNumber = variblesToNumber;
            this.varibleType = varibleType;
        }
    }
    public class VaribleNode
    {
        public Dictionary<string, Varible> variblesToInfo;
        public int currentIndex;
        public VaribleNode()
        {
            variblesToInfo = new Dictionary<string, Varible>();
            currentIndex = 0;
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
