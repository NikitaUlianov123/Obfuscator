using Microsoft.VisualBasic;

using Obfuscator.Tokens;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Obfuscator
{
    public enum InfoType
    {
        Array,
        Int,
        String,
        Bool,
        Float,
        Char,
        Struct,
        None
    }

    public class LoopScope
    {
        public string StartingLabel;
        public string EndingLabel;
        public LoopScope(string StartingLabel, string EndingLabel)
        {
            this.StartingLabel = StartingLabel;
            this.EndingLabel = EndingLabel;
        }
    }

    public class VaribleStack
    {
        public Dictionary<TokenType, string> TypeToCommand = new Dictionary<TokenType, string>();
        public Dictionary<string, StructInfo> structs = new Dictionary<string, StructInfo>();
        public Dictionary<string, Function> functions = new Dictionary<string, Function>();
        public Dictionary<string, EnumInfo> enums = new Dictionary<string, EnumInfo>();
        public Dictionary<string, int> enumValues = new Dictionary<string, int>();
        public Stack<LoopScope> LoopScopes = new Stack<LoopScope>();
        
        public int functionCount;
        List<VaribleNode> varibleNodes;
        public Dictionary<TokenType, int> TokenToSize = new()
        {
            // ---------- Types ----------
            [TokenType.KW_INT] = 4,
            [TokenType.KW_STRING] = 4
        };
        public Dictionary<TokenType, InfoType> TokenToInfo = new()
        {
            [TokenType.KW_INT] = InfoType.Int,
            [TokenType.KW_STRING] = InfoType.String,
            [TokenType.KW_BOOL] = InfoType.Bool

        };

        public VaribleStack()
        { 
            varibleNodes = new List<VaribleNode>();
            functionCount = 0;
            addScope();
        }
        public void addScope()
        {
            varibleNodes.Add(new VaribleNode());
        }
        public void addLoopScope(string staringLabel, string endingLabel)
        {
            LoopScopes.Push(new LoopScope(staringLabel, endingLabel));
        }
        public void popLoopScope()
        {
            LoopScopes.Pop();
        }
        public void popScope()
        {
            varibleNodes.RemoveAt(varibleNodes.Count - 1);
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
            return new Varible(-1, new PrimativeInfo(InfoType.Int));
        }
        public void addArrayVarible(string nameOfArray, Info dataType, int length, int size)
        {
            ArrayInfo arrayInfo = new ArrayInfo(dataType,length,size);

            varibleNodes[varibleNodes.Count - 1].variblesToInfo.Add(nameOfArray, new Varible(varibleNodes[varibleNodes.Count - 1].currentIndex, arrayInfo));
            varibleNodes[varibleNodes.Count - 1].currentIndex++;
        }
        public void addStructVarible(string nameOfStructVarible, string structName)
        {
            StructInfo structInfo = structs[structName];
            varibleNodes[varibleNodes.Count - 1].variblesToInfo.Add(nameOfStructVarible, new Varible(varibleNodes[varibleNodes.Count - 1].currentIndex, structInfo));
            varibleNodes[varibleNodes.Count - 1].currentIndex++;
        }
        public void addPrimativeVarible(string nameOfVarible, InfoType type)
        {
            PrimativeInfo info = new PrimativeInfo(type);
            varibleNodes[varibleNodes.Count - 1].variblesToInfo.Add(nameOfVarible, new Varible(varibleNodes[varibleNodes.Count - 1].currentIndex, info)); 
            varibleNodes[varibleNodes.Count - 1].currentIndex++;
        }
        public Info findInfo(ASTNodeBase type)
        {
            if(type.RootToken.GetTokenType() == TokenType.ID)
            {
                if(structs.ContainsKey(type.RootToken.text))
                {
                    return structs[type.RootToken.text];
                }
                else if(enums.ContainsKey(type.RootToken.text))
                {
                    return enums[type.RootToken.text];
                }
            }
            else
            {
                return new PrimativeInfo(TokenToInfo[type.RootToken.GetTokenType()]);
            }
            return new NoneInfo();
        }
        public void FirstSemPass(ASTNodeBase Root)
        {
            if(Root.RootToken.GetTokenType() == TokenType.TERMAIL)
            {
                ASTNodeTermial termial = (ASTNodeTermial)Root;
                for(int i = 0; i <  termial.terms.Count; i++)
                {
                    FirstSemPass(termial.terms[i]);
                }
            }
            else if(Root.RootToken.GetTokenType() == TokenType.FUNCTION)
            {
                ASTNodeFunction function = (ASTNodeFunction)Root;
                functions.Add(function.Name.RootToken.text, new Function(function,this,functionCount));
                functionCount++;
            }    
            else if(Root.RootToken.GetTokenType() == TokenType.KW_STRUCT)
            {
                ASTNodeStruct astStruct = (ASTNodeStruct)Root;
                StructInfo structInfo = new StructInfo(astStruct.Name.RootToken.text,astStruct.Body,this);
                structs.Add(astStruct.Name.RootToken.text, structInfo);
            }
            else if(Root.RootToken.GetTokenType() == TokenType.KW_ENUM)
            {
                ASTNodeEnum astEnum = (ASTNodeEnum)Root;
                EnumInfo enumInfo = new EnumInfo(astEnum.Name.RootToken.text);
                enumInfo.ParseEnum(astEnum.EnumBody, enumValues, 0);
                enums.Add(astEnum.Name.RootToken.text, enumInfo);
                ;
            }
        }
        public void SecondSemPass(IRCode IRCodeHandler)
        {
            foreach(KeyValuePair<string, Function> currentFunction in functions)
            {
                currentFunction.Value.ParseParmas((ASTNodeParamList)currentFunction.Value.function.Params, this);
                IRCodeHandler.byteCounter = 0;
                IRCodeHandler.code += $"{currentFunction.Key}: \n";
                addScope();
                for( int i = 0; i < currentFunction.Value.args.Count; i++)
                {
                    varibleNodes[varibleNodes.Count - 1].variblesToInfo.Add(currentFunction.Value.args[i].name, new Varible(varibleNodes[varibleNodes.Count - 1].currentIndex, currentFunction.Value.args[i].info));
                    varibleNodes[varibleNodes.Count - 1].currentIndex++;
                }
                for(int i = currentFunction.Value.args.Count-1; i >= 0; i--)
                {
                    IRCodeHandler.PopVarible(new Varible(i, currentFunction.Value.args[i].info));
                }
                currentFunction.Value.function.Body.IRGen(this,IRCodeHandler);
                popScope();
            }
            ;
        }
        public void ThridSemPass(ASTNodeBase root, IRCode IRCodeHandler)
        {
            IRCodeHandler.code += "MAIN* \n";
            root.IRGen(this, IRCodeHandler);
        }

    }
    public class IRCode
    {
        public string code;
        public int byteCounter;
        public int currentLabel;
        public int currentString;
        public Dictionary<string, int> StringToNumber;
        public Dictionary<string, int> LabelToByte;
        public Dictionary<InfoType, string> InfoTypeToInstructionPush = new()
        {
            [InfoType.Int] = "PUSH_LOCAL_INT ",
            [InfoType.String] = "PUSH_LOCAL_STRING ",
            [InfoType.Array] = "PUSH_LOCAL_INT ",
            [InfoType.Struct] = "PUSH_LOCAL_INT "
        };
        public Dictionary<InfoType, string> InfoTypeToInstructionPop = new()
        {
            [InfoType.Int] = "POP_LOCAL_INT ",
            [InfoType.String] = "POP_LOCAL_STRING ",
            [InfoType.Array] = "POP_LOCAL_INT ",
            [InfoType.Struct] = "POP_LOCAL_INT "
        };
        public Dictionary<InfoType, string> InfoTypeTOInstructionLoadMem = new()
        {
            [InfoType.Int] = "LOAD_HEAP_INT ",
            [InfoType.Bool] = "LOAD_HEAP_BOOL ",
            [InfoType.Char] = "LOAD_HEAP_CHAR ",
            [InfoType.Struct] = "LOAD_HEAP_INT ",
            [InfoType.Array] = "LOAD_HEAP_INT ",
        };
        public Dictionary<InfoType, string> InfoTypeTOInstructionStoreMem = new()
        {
            [InfoType.Int] = "STORE_HEAP_INT ",
            [InfoType.Char] = "STORE_HEAP_CHAR ",
            [InfoType.Bool] = "STORE_HEAP_BOOL ",
            [InfoType.Array] = "STORE_HEAP_INT ",
            [InfoType.Struct] = "STORE_HEAP_INT ",
        };
        public IRCode(string code, int byteCounter)
        {
            this.code = code;
            this.byteCounter = byteCounter;
            LabelToByte = new Dictionary<string, int>();
            StringToNumber = new Dictionary<string, int>();
            currentLabel = 0;
            currentString = 0;
        }
        public void PushConstChar(int value)
        {
            code += $"PUSH_CONST_CHAR {value}\n";
            byteCounter += 2;
        }
        public void PushConstInt(int value)
        {
            code += $"PUSH_CONST_INT {value}\n";
            byteCounter += 5;
        }
        public void AddCommand(string Command)
        {
            code += Command + "\n";
            byteCounter += 1;
        }
        public void Dup()
        {
            code += "DUP \n";
            byteCounter++;
        }
        public void AllocArray(ArrayInfo arrayInfo)
        {
            code += $"ALLOC_ARRAY {arrayInfo.size}\n";
            byteCounter += 5;
        }
        public void AllocStruct(StructInfo structInfo)
        {
            code += $"ALLOC_STRUCT {structInfo.memorySize}\n";
            byteCounter += 5;
        }
        public void PopLocalInt(int value)
        {
            code += $"POP_LOCAL_INT {value}\n";
            byteCounter += 5;
        }
        public string GetNewLabel()
        {
            currentLabel += 1;
            return $"LABEL{currentLabel}";
        }
        public void AddJNZ(string labelToJump)
        {
            code += $"JNZ {labelToJump}\n";
            byteCounter += 5;
        }
        public void AddJNZ(int byteToJump)
        {
            code += $"JNZ {byteToJump}\n";
            byteCounter += 5;
        }
        public void AddJMP(string labelToJump)
        {
            code += $"JMP {labelToJump}\n";
            byteCounter += 5;
        }
        public void AddJMP(int byteToJump)
        {
            code += $"JMP {byteToJump}\n";
            byteCounter += 5;
        }
        public void AddLabelDef(string label)
        {
            code += "#" + label + "\n";
            LabelToByte.Add(code, byteCounter);
        }
        public void AddFunctionCall(Function function)
        {
            code += $"CALL {function.functionNumber}\n";
            byteCounter += 2;
        }
        public void PushString(string value)
        {
            if(!StringToNumber.ContainsKey(value))
            {
                StringToNumber.Add(value, currentString);
                currentString += 1;
            }
            PushConstInt(StringToNumber[value]);
        }
        public void PushVarible(Varible varible)
        {
            code += InfoTypeToInstructionPush[varible.varibleInfo.infoType] + varible.variblesToNumber + "\n";
            byteCounter += 5;
        }
        public void PopVarible(Varible varible)
        {
            code += InfoTypeToInstructionPop[varible.varibleInfo.infoType] + varible.variblesToNumber + "\n";
            byteCounter += 5;
        }
        public void LoadMemory(InfoType typeToLoad)
        {
            code += InfoTypeTOInstructionLoadMem[typeToLoad] + "\n";
            byteCounter++;
        }
        public void StoreMemory(InfoType typeToLoad)
        {
            code += InfoTypeTOInstructionStoreMem[typeToLoad] + "\n";
            byteCounter++;
        }
    }

    public class Varible
    { 
        public int variblesToNumber;
        public Info varibleInfo;
        public Varible(int variblesToNumber, Info varibleInfo)
        {
            this.variblesToNumber = variblesToNumber;
            this.varibleInfo = varibleInfo;
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
    public abstract class Info
    {
        public InfoType infoType;
        public Info(InfoType infoType)
        {
            this.infoType = infoType;
        }
        public abstract int getSize();
    }
    public class PrimativeInfo : Info
    {
        public PrimativeInfo(InfoType infoType) : base(infoType)
        {

        }
        public override int getSize()
        {
            if(infoType == InfoType.Int)
            {
                return 4;
            }
            return 1;
        }
    }
    public class ArrayInfo : Info
    {
        public Info varibleInfo;
        public int length;
        public int size;
        public ArrayInfo(Info varibleInfo, int length, int size) : base(InfoType.Array)
        {
            this.varibleInfo = varibleInfo;
            this.length = length;
            this.size = size;
        }
        public override int getSize()
        {
            return size;
        }
    }
    public class NoneInfo : Info
    {
        public NoneInfo() : base(InfoType.None)
        {

        }
        public override int getSize()
        {
            return 0;
        }
    }
    public class MemberInfo
    {
        public string name;
        public Info info;
        public int offset;
        public MemberInfo(string name, Info info, int offset)
        {
            this.name = name;
            this.info = info;
            this.offset = offset;
        }
    }
    public class Args
    {
        public string name;
        public Info info;
        public Args(string name, Info info)
        {
            this.name = name;
            this.info = info;
        }
    }

    public class Function
    {
        public ASTNodeFunction function;
        public string name;
        public List<Args> args;
        public Info returnType;
        public int functionNumber;
        public Function(ASTNodeFunction function, VaribleStack stack, int functionNumber)
        {
            this.function = function;
            name = function.Name.RootToken.text;
            args = new List<Args>();
            if (function.ReturnType.RootToken.GetTokenType() != TokenType.KW_VOID)
            {
                returnType = stack.findInfo(function.ReturnType);
            }
            returnType = new NoneInfo();
            //ParseParmas((ASTNodeParamList)function.Params, stack);
            ;
            this.functionNumber = functionNumber;
        }
        public void ParseParmas(ASTNodeParamList parmas, VaribleStack stack)
        {
            Info info = stack.findInfo(parmas.Params[0]);
            args.Add(new Args(parmas.Params[1].RootToken.text, info));
            if(parmas.Params.Count == 3)
            {
                ParseParmas((ASTNodeParamList)parmas.Params[2],stack);
            }
        }
    }
    public class EnumInfo : Info
    {
        public Dictionary<string, int> stringToInt;
        public string enumName;
        public EnumInfo(string enumName) : base(InfoType.Char)
        {
            this.enumName = enumName;
            stringToInt = new Dictionary<string, int>();
        }
        public override int getSize()
        {
            return 1;
        }
        public int ParseEnum(ASTNodeBase Body, Dictionary<string, int> stringToInt, int start)
        {
            if(Body.RootToken.GetTokenType() == TokenType.ID)
            {
                stringToInt.Add(Body.RootToken.text, start);
                return start + 1;
            }
            else if(Body.RootToken.GetTokenType() == TokenType.OP_ASSIGN)
            {
                ASTNodeVarAssign nodeVarAssign = (ASTNodeVarAssign)Body;
                stringToInt.Add(nodeVarAssign.Target.RootToken.text,int.Parse(nodeVarAssign.Value.RootToken.text));
                return int.Parse(nodeVarAssign.Value.RootToken.text) + 1;
            }
            else
            {
                ASTNodeTermial nodeTermial = (ASTNodeTermial)Body;
                start = ParseEnum(nodeTermial.terms[0], stringToInt, start);
                start = ParseEnum(nodeTermial.terms[1], stringToInt, start);
                return start;
            }
        }
    }

    public class StructInfo : Info
    {
        public Dictionary<string, MemberInfo> memberInfos;
        public string structName;
        public int memorySize;
        public StructInfo(string structName, ASTNodeBase structBody, VaribleStack stack) : base(InfoType.Struct)
        {
            this.structName = structName;
            memberInfos = new Dictionary<string, MemberInfo>();
            if(structBody.RootToken.GetTokenType() != TokenType.NONE)
            {
                parseMembers((ASTNodeStructBody)structBody, stack, 0);
            }

        }
        public override int getSize()
        {
            return memorySize;
        }
        public void parseMembers(ASTNodeStructBody aSTNode, VaribleStack stack, int offset)
        {
            if (aSTNode.Members[0].RootToken.GetTokenType() == TokenType.RBRACKET)
            {
                ASTNodeArrayDecl arrayDecl = (ASTNodeArrayDecl)aSTNode.Members[0];
                int length = int.Parse(arrayDecl.Size.RootToken.text);
                ArrayInfo arrayInfo;
                if(arrayDecl.TypeNode.RootToken.GetTokenType() == TokenType.ID)
                {
                    if(stack.structs.ContainsKey(arrayDecl.TypeNode.RootToken.text))
                    {
                        arrayInfo = new ArrayInfo(stack.structs[arrayDecl.TypeNode.RootToken.text], length, length * stack.structs[arrayDecl.TypeNode.RootToken.text].memorySize);
                    }
                    else
                    {
                        arrayInfo = new ArrayInfo(stack.enums[arrayDecl.TypeNode.RootToken.text], length, length);
                    }
                }
                else
                {
                    PrimativeInfo primative = new PrimativeInfo(stack.TokenToInfo[arrayDecl.TypeNode.RootToken.GetTokenType()]);
                    arrayInfo = new ArrayInfo(primative, length, primative.getSize());
                }

                memberInfos.Add(arrayDecl.NameNode.RootToken.text, new MemberInfo(arrayDecl.NameNode.RootToken.text, arrayInfo, offset));
                memorySize += arrayInfo.length * arrayInfo.size;
                offset += arrayInfo.length * arrayInfo.size;
                if(aSTNode.Members.Count > 1)
                {
                    parseMembers((ASTNodeStructBody)aSTNode.Members[1], stack, offset);
                }
                return;
            }
            if (aSTNode.Members.Count == 1) return;

            
            Info info = stack.findInfo(aSTNode.Members[0]);
            memberInfos.Add(aSTNode.Members[1].RootToken.text, new MemberInfo(aSTNode.Members[1].RootToken.text, info, offset));
            offset += info.getSize();
            memorySize += info.getSize();
            parseMembers((ASTNodeStructBody)aSTNode.Members[2], stack, offset);
        }

    }



    public abstract class ASTNodeLeftValueBase : ASTNodeBase
    {
        public ASTNodeLeftValueBase(int beginning, int end) : base(beginning,end) 
        {

        }
        public abstract void IRGenLeftValue(VaribleStack stack, IRCode IRCodeHandler);
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

        public abstract void IRGen(VaribleStack stack, IRCode IRCodeHandler);

        
        public abstract void RenamingVariblePass(StackInterpter currentStack); 

        public void RenamingPassMulti(StackInterpter currentStack, List<ASTNodeBase> list)
        {
            for(int i = 0; i < list.Count; i++)
            {
                list[i].RenamingVariblePass(currentStack);
            }
        }
        public void IRGenArgs(VaribleStack varibleStack, IRCode IRCodeHandler, List<ASTNodeBase> args, string command)
        {
            string ret = "";
            for(int i = 0; i <  args.Count; i++)
            {
                args[i].IRGen(varibleStack, IRCodeHandler);
            }
            IRCodeHandler.AddCommand(command);
        }
        public void IRGenMath(VaribleStack varibleStack, IRCode IRCodeHandler, ASTNodeBase left, ASTNodeBase right, string command)
        {
            left.IRGen(varibleStack, IRCodeHandler);
            right.IRGen(varibleStack, IRCodeHandler);
            IRCodeHandler.AddCommand(command);
        }

    }

}
