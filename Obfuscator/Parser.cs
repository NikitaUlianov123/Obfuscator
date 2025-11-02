using Obfuscator.ASTNodes;
using Obfuscator.Tokens;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuscator
{
    public class Parser
    {
        private static ASTNodeBase AssignNode(int beginning, int end, List<ASTNodeBase> nodes)
        {
            if (nodes.Count > 3)
            {
                return new ASTNodeVarDec(beginning, end, nodes);
            }
            return new ASTNodeVarAssign(beginning, end, nodes);
        }
        public GrammarParser grammar;
        public Parser(string grammarFilePath)
        {
            grammar = new GrammarParser(grammarFilePath);
        }

        private ASTNodeBase MakeTerminal(int beginning, int end, List<ASTNodeBase> nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].RootToken.GetTokenType() == TokenType.NONE)
                {
                    nodes.RemoveAt(i);
                    i--;
                }
            }
            if (nodes.Count == 0) return new ASTNodeNone(beginning, end, new NONEToken("", 0));

            if (nodes.Count == 1) return nodes.First();

            return new ASTNodeTermial(beginning, end, nodes);
        }
        
        public Dictionary<string, TokenType> StringToTokenType = new()
        {
            // ---------- Types ----------
            ["\"int\""] = TokenType.KW_INT,
            ["\"string\""] = TokenType.KW_STRING,
            ["\"bool\""] = TokenType.KW_BOOL,
            ["\"float\""] = TokenType.KW_FLOAT,
            ["\"void\""] = TokenType.KW_VOID,

            // Identifier (used as variable/function name; in T -> "id" your reducer can build ASTTypeId)
            ["\"id\""] = TokenType.ID,

            // ---------- Literals ----------
            ["\"number\""] = TokenType.NUMBER,
            ["\"charVal\""] = TokenType.CHARVAL,
            ["\"boolVal\""] = TokenType.BOOLVAL,
            ["\"stringVal\""] = TokenType.STRINGVAL,
            ["\"none\""] = TokenType.NONE,

            // ---------- Control flow ----------
            ["\"if\""] = TokenType.KW_IF,
            ["\"while\""] = TokenType.KW_WHILE,
            ["\"for\""] = TokenType.KW_FOR,
            ["\"switch\""] = TokenType.KW_SWITCH,
            ["\"case\""] = TokenType.KW_CASE,
            ["\"default\""] = TokenType.KW_DEFAULT,
            ["\"break\""] = TokenType.KW_BREAK,
            ["\"continue\""] = TokenType.KW_CONTINUE,
            ["\"return\""] = TokenType.KW_RETURN,
            ["\"#define\""] = TokenType.KW_DEFINE,
            ["\"delay\""] = TokenType.KW_DELAY,

            // ---------- Task / Program structure ----------
            ["\"task\""] = TokenType.KW_TASK,
            ["\"main\""] = TokenType.KW_MAIN,
            ["\"struct\""] = TokenType.KW_STRUCT,

            // ---------- RobotC builtins ----------
            ["\"motor\""] = TokenType.KW_MOTOR,
            ["\"nMotorEncoder\""] = TokenType.KW_NMOTORENCODER,
            ["\"SensorValue\""] = TokenType.KW_SENSORVALUE,

            // ---------- RobotC ports/constants ----------
            ["\"motorA\""] = TokenType.KW_MOTOR_A,
            ["\"motorB\""] = TokenType.KW_MOTOR_B,
            ["\"motorC\""] = TokenType.KW_MOTOR_C,

            ["\"port1\""] = TokenType.KW_PORT1,
            ["\"port2\""] = TokenType.KW_PORT2,
            ["\"port3\""] = TokenType.KW_PORT3,
            ["\"port4\""] = TokenType.KW_PORT4,

            // ---------- Operators (plain forms only) ----------
            ["\"=\""] = TokenType.OP_ASSIGN,

            ["\"+\""] = TokenType.OP_PLUS,
            ["\"-\""] = TokenType.OP_MINUS,
            ["\"*\""] = TokenType.OP_STAR,
            ["\"/\""] = TokenType.OP_SLASH,
            ["\"%\""] = TokenType.OP_PERCENT,

            ["\"&&\""] = TokenType.OP_ANDAND,
            ["\"||\""] = TokenType.OP_OROR,
            ["\"==\""] = TokenType.OP_EQEQ,
            ["\">\""] = TokenType.OP_GT,
            ["\"<\""] = TokenType.OP_LT,

            ["\"+=\""] = TokenType.OP_PLUSEQ,
            ["\"-=\""] = TokenType.OP_MINUSEQ,
            ["\"++\""] = TokenType.OP_PLUSPLUS,
            ["\"--\""] = TokenType.OP_MINUSMINUS,

            ["\".\""] = TokenType.OP_DOT,

            // ---------- Punctuation ----------
            ["\"(\""] = TokenType.LPAREN,
            ["\")\""] = TokenType.RPAREN,
            ["\"{\""] = TokenType.LBRACE,
            ["\"}\""] = TokenType.RBRACE,
            ["\"[\""] = TokenType.LBRACKET,
            ["\"]\""] = TokenType.RBRACKET,
            ["\",\""] = TokenType.COMMA,
            [";"] = TokenType.SEMI,
            ["\":\""] = TokenType.COLON,
        };



        public static readonly Dictionary<string, Func<int, int, AbstactToken, ASTNodeBase>> LeafFactoryRobotC
    = new()
    {
        // ---- TYPES ----
        ["\"int\""] = (b, e, t) => new ASTTypeInt(b, e, t),
        ["\"string\""] = (b, e, t) => new ASTTypeString(b, e, t),
        ["\"bool\""] = (b, e, t) => new ASTTypeBool(b, e, t),
        ["\"float\""] = (b, e, t) => new ASTTypeFloat(b, e, t),
        ["\"void\""] = (b, e, t) => new ASTTypeVoid(b, e, t),

        // ---- IDENTIFIERS (non-type positions) ----
        ["\"id\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),

        // ---- LITERALS ----
        ["\"number\""] = (b, e, t) => new ASTLitNumber(b, e, t),
        ["\"charVal\""] = (b, e, t) => new ASTLitChar(b, e, t),
        ["\"boolVal\""] = (b, e, t) => new ASTLitBool(b, e, t),
        ["\"stringVal\""] = (b, e, t) => new ASTLitString(b, e, t),
        ["\"none\""] = (b, e, t) => new ASTLitNone(b, e, t),

        // ---- ROBOTC PORT LITERALS ----
        ["\"motorA\""] = (b, e, t) => new ASTLitMotorA(b, e, t),
        ["\"motorB\""] = (b, e, t) => new ASTLitMotorB(b, e, t),
        ["\"motorC\""] = (b, e, t) => new ASTLitMotorC(b, e, t),

        ["\"port1\""] = (b, e, t) => new ASTLitPort1(b, e, t),
        ["\"port2\""] = (b, e, t) => new ASTLitPort2(b, e, t),
        ["\"port3\""] = (b, e, t) => new ASTLitPort3(b, e, t),
        ["\"port4\""] = (b, e, t) => new ASTLitPort4(b, e, t),

        // ---- BUILTIN NAMES used as primaries (treated as identifiers at leaf level) ----
        ["\"SensorValue\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\"nMotorEncoder\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\"motor\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),

        // ---- task/main/struct names as leaves where needed ----
        ["\"task\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\"main\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\"struct\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),

        // ============================
        // OPERATORS (leaf token nodes)
        // ============================

        // Assignment / arithmetic
        ["\"=\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\"+\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\"-\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\"*\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\"/\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\"%\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),

        // Logical / comparison
        ["\"&&\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\"||\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\"==\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\"<\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\"\">\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),

        // Compound / increment
        ["\"+=\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\"-=\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\"++\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\"--\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),

        // Member access
        ["\".\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),

        // =====================
        // PUNCTUATION as leaves
        // =====================
        ["\"(\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\")\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\"{\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\"}\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\"[\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\"]\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\",\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        [";"] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\":\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\"if\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\"return\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\"while\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\"for\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\"#define\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
        ["\"delay\""] = (b, e, t) => new ASTNodeIdentifier(b, e, t),
    };


        // ---------------------------------
        // INTERNAL / COMPOSITE NODE FACTORY
        // ---------------------------------
        public static readonly Dictionary<string, Func<int, int, List<ASTNodeBase>, ASTNodeBase>> ASTFactoryRobotC
           = new()
           {
               // Assign / VarDecl
               ["\"=\""] = (b, e, ch) => AssignNode(b, e, ch),

               // Arithmetic (E'' level)
               ["\"\""] = (b, e, ch) => new ASTNodeAdd(b, e, ch),
               ["\"-\""] = (b, e, ch) => new ASTNodeSub(b, e, ch),
               ["\"*\""] = (b, e, ch) => new ASTNodeMul(b, e, ch),
               ["\"/\""] = (b, e, ch) => new ASTNodeDiv(b, e, ch),
               ["\"%\""] = (b, e, ch) => new ASTNodeMod(b, e, ch),
               ["\"+\""] = (b, e, ch) => new ASTNodeAdd(b, e, ch),
               ["\"++\""] = (b, e, ch) => new ASTNodePlusPlus(b, e, ch),
               ["\"--\""] = (b, e, ch) => new ASTNodeMinusMinus(b, e, ch),

               // Boolean / Comparison (CE, CE')
               ["\"&&\""] = (b, e, ch) => new ASTNodeAnd(b, e, ch),
               ["\"||\""] = (b, e, ch) => new ASTNodeOr(b, e, ch),
               ["\"==\""] = (b, e, ch) => new ASTNodeEq(b, e, ch),
               ["\">\""] = (b, e, ch) => new ASTNodeGt(b, e, ch),
               ["\"<\""] = (b, e, ch) => new ASTNodeLt(b, e, ch),

               // Member access
               ["\".\""] = (b, e, ch) => new ASTNodeMemberAccess(b, e, ch),

               // Control flow
               ["\"if\""] = (b, e, ch) => new ASTNodeIf(b, e, ch),
               ["\"while\""] = (b, e, ch) => new ASTNodeWhile(b, e, ch),
               ["\"for\""] = (b, e, ch) => new ASTNodeFor(b, e, ch),
               ["\"switch\""] = (b, e, ch) => new ASTNodeSwitch(b, e, ch),

               // Return / Break / Continue
               ["\"return\""] = (b, e, ch) => new ASTNodeReturn(b, e, ch),
               ["\"break\""] = (b, e, ch) => new ASTNodeBreak(b, e, ch[0].RootToken),
               ["\"continue^\""] = (b, e, ch) => new ASTNodeContinue(b, e, ch[0].RootToken),

               // Special l-values: motor[...] =^ E''  / nMotorEncoder[...] =^ E''
               // (These are easiest to construct in reduce actions that recognize the exact pattern,
               // but if you already tag the root as 'motor' or 'nMotorEncoder', you can use:)
               ["\"motor\""] = (b, e, ch) => new ASTNodeMotorAssign(b, e, ch),
               ["\"nMotorEncoder\""] = (b, e, ch) => new ASTNodeEncoderAssign(b, e, ch),

               // Functions / Structs / Task
               // If your reduce action uses the return type token as the root, these will fire:
               ["\"void\""] = (b, e, ch) => new ASTNodeFunction(b, e, ch),
               ["\"int\""] = (b, e, ch) => new ASTNodeFunction(b, e, ch),
               ["\"string\""] = (b, e, ch) => new ASTNodeFunction(b, e, ch),
               ["\"bool\""] = (b, e, ch) => new ASTNodeFunction(b, e, ch),
               ["\"float\""] = (b, e, ch) => new ASTNodeFunction(b, e, ch),
               ["\"struct\""] = (b, e, ch) => new ASTNodeStruct(b, e, ch),
               ["\"id\""] = (b, e, ch) => new ASTNodeFunction(b, e, ch),
               ["\"task\""] = (b, e, ch) => new ASTNodeTaskMain(b, e, ch),
               ["\"#define\""] = (b, e, ch) => new ASTDefineNode(b, e, ch),
               ["\"delay\""] = (b, e, ch) => new ASTDelayNode(b, e, ch),

               // Aggregates / lists (use these keys only if your reducer emits them as synthetic roots)
               ["\"Body\""] = (b, e, ch) => new ASTNodeBlock(b, e, ch),
               ["\"CaseList\""] = (b, e, ch) => new ASTNodeCaseList(b, e, ch),
               ["\"CaseItem\""] = (b, e, ch) => new ASTNodeCaseItem(b, e, ch),
               ["\"ForHeader\""] = (b, e, ch) => new ASTNodeForHeader(b, e, ch),
               ["\"ProgramBody\""] = (b, e, ch) => new ASTNodeProgramBody(b, e, ch),
               ["\"Program\""] = (b, e, ch) => new ASTNodeProgram(b, e, ch),
               ["\"Parma\""] = (b, e, ch) => new ASTNodeParamList(b, e, ch),
               ["\"StructBody\""] = (b, e, ch) => new ASTNodeStructBody(b, e, ch),

               // Calls & arg lists (if you mark '(' as the root for calls)
               ["\"(\""] = (b, e, ch) => new ASTNodeCall(b, e, ch),
               ["\"ListOfStatements\""] = (b, e, ch) => new ASTNodeArgList(b, e, ch),

           };


        public static readonly HashSet<TokenType> SkipTokens = new()
        {
            TokenType.SEMI,
            TokenType.COLON,
            TokenType.COMMA,
            TokenType.OP_DOT,
            TokenType.LPAREN,
            TokenType.RPAREN,
            TokenType.LBRACE,
            TokenType.RBRACE
        };
        public ASTNodeBase Parsing(List<AbstactToken> tokens, string starting, int current)
        {
            if(starting == "Define")
            {
                ;
            }
            for (int i = 0; i < grammar.grammarPairs[starting].Count; i++)
            {
                bool isMatch = true;
                int prevStart = current;
                List<ASTNodeBase> childern = new List<ASTNodeBase>();
                for (int d = 0; d < grammar.grammarPairs[starting][i].text.Count; d++)
                {
                    var e = grammar.grammarPairs[starting][i];
                    if (current >= tokens.Count)
                    {
                        isMatch = false;
                        break;
                    }
                    while (tokens[current].GetTokenType() == TokenType.WHITE_SPACE)
                    {
                        current++;
                        if (current >= tokens.Count)
                        {
                            break;
                        }
                    }
                    if (tokens[current].GetTokenType() == TokenType.OP_PLUS)
                    {
                        ;
                    }
                    var temp = StringToTokenType.ContainsKey(grammar.grammarPairs[starting][i].text[d]);
                    //var temps = StringToTokenType[grammar.grammarPairs[starting][i].text[d]] == tokens[current].GetTokenType();
                    if (grammar.grammarPairs[starting][i].text[d] == "\"none\"")
                    {
                        childern.Add(new ASTNodeNone(current, current, new NONEToken("", 0)));
                    }
                    else if (StringToTokenType.ContainsKey(grammar.grammarPairs[starting][i].text[d]) && StringToTokenType[grammar.grammarPairs[starting][i].text[d]] == tokens[current].GetTokenType())
                    {
                        if (SkipTokens.Contains(tokens[current].GetTokenType()))
                        {
                            current++;
                            continue;
                        }

                        childern.Add(LeafFactoryRobotC[grammar.grammarPairs[starting][i].text[d]].Invoke(current, current + 1, tokens[current]));
                        current++;
                    }
                    else if (grammar.grammarPairs.ContainsKey(grammar.grammarPairs[starting][i].text[d]))
                    {
                        ASTNodeBase node = Parsing(tokens, grammar.grammarPairs[starting][i].text[d], current);
                        if (node.RootToken.GetTokenType() == TokenType.ERROR)
                        {
                            isMatch = false;
                            break;
                        }
                        current = node.End;
                        childern.Add(node);
                    }
                    else
                    {
                        isMatch = false;
                        break;
                    }
                }
                if (isMatch)
                {
                    if (grammar.grammarPairs[starting][i].exToken == null)
                    {
                        return MakeTerminal(prevStart, current, childern);
                    }
                    else if (ASTFactoryRobotC.ContainsKey(grammar.grammarPairs[starting][i].exToken))
                    {
                        return ASTFactoryRobotC[grammar.grammarPairs[starting][i].exToken].Invoke(prevStart, current, childern);
                    }
                    else if(ASTFactoryRobotC.ContainsKey(starting))
                    {
                        return ASTFactoryRobotC[starting].Invoke(prevStart, current, childern);
                    }
                }
                current = prevStart;
            }
            return new ASTNodeMismatch(0, 0, new ERRORToken("", 0));
        }
        public Parser()
        {


        }
    }
}
