using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuscator.Tokens
{
    public class NONEToken : AbstactToken
    {
        public NONEToken(string text, int length) : base(text, length) { }
        public override TokenType GetTokenType() => TokenType.NONE;
    }
}
