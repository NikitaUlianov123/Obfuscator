using Obfuscator.Tokens;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Obfuscator
{
    public enum TokenType
    {
        // Logical / relational (multi-char)
        OP_ANDAND = 0,   // &&
        OP_OROR = 1,   // ||
        OP_EQEQ = 2,   // ==

        // Keywords
        KW_RETURN = 3,   // return
        KW_IF = 4,   // if
        KW_WHILE = 5,   // while
        KW_TASK = 6,   // task
        KW_MAIN = 7,   // main
        KW_VOID = 8,   // void
        KW_INT = 9,   // int
        KW_STRING = 10,  // string
        KW_BOOL = 11,  // bool
        KW_FLOAT = 12,  // float
        KW_STRUCT = 13,  // struct
        KW_FOR = 14,  // for
        KW_SWITCH = 15,  // switch
        KW_CASE = 16,  // case
        KW_DEFAULT = 17,  // default
        KW_BREAK = 18,  // break
        KW_CONTINUE = 19,  // continue

        // RobotC builtins
        KW_MOTOR = 20, // motor
        KW_NMOTORENCODER = 21, // nMotorEncoder
        KW_SENSORVALUE = 22, // SensorValue

        // RobotC constants / ports (split)
        KW_MOTOR_A = 23, // motorA
        KW_MOTOR_B = 24, // motorB
        KW_MOTOR_C = 25, // motorC
        KW_PORT1 = 26, // port1
        KW_PORT2 = 27, // port2
        KW_PORT3 = 28, // port3
        KW_PORT4 = 29, // port4

        // Literals / identifiers
        NUMBER = 30, // \d+(\.\d+)?
        CHARVAL = 31, // '(...)'
        STRINGVAL = 32, // "..."
        BOOLVAL = 33, // true|false
        ID = 34, // [A-Za-z_]\w*

        // Compound ops
        OP_PLUSEQ = 35, // +=
        OP_MINUSEQ = 36, // -=
        OP_PLUSPLUS = 37, // ++
        OP_MINUSMINUS = 38, // --

        // Single-char ops
        OP_ASSIGN = 39, // =
        OP_PLUS = 40, // +
        OP_MINUS = 41, // -
        OP_STAR = 42, // *
        OP_SLASH = 43, // /
        OP_PERCENT = 44, // %
        OP_LT = 45, // <
        OP_GT = 46, // >
        OP_DOT = 47, // .

        // Punctuation
        LPAREN = 48, // (
        RPAREN = 49, // )
        LBRACE = 50, // {
        RBRACE = 51, // }
        LBRACKET = 52, // [
        RBRACKET = 53, // ]
        COMMA = 54, // ,
        SEMI = 55, // ;
        COLON = 56, // :

        // Non-regex tokens (emitted by the lexer)
        EOF = 57,
        ERROR = 58,
        WHITE_SPACE = 59,
        KW_DEFINE = 60,
        KW_DELAY = 61,
        KW_CLEAR_PIXEL = 62,
        KW_DISPLAY_BIG_STRING_AT = 63,
        KW_DISPLAY_TEXT_LINE = 64,
        KW_DISPLAY_CENTERED_BIG_TEXT_LINE = 65,
        KW_DISPLAY_CENTERED_TEXT_LINE = 66,
        KW_DISPLAY_BIG_TEXT_LINE = 67,
        KW_DISPLAY_RIC_FILE = 68,
        KW_DISPLAY_STRING = 69,
        KW_DISPLAY_STRING_AT = 70,
        KW_DRAW_ELLIPSE = 71,
        KW_DRAW_INVERT_ELLIPSE = 72,
        KW_DRAW_INVERT_RECT = 73,
        KW_DRAW_LINE = 74,
        KW_DRAW_RECT = 75,
        KW_ERASE_DISPLAY = 76,
        KW_ERASE_ELLIPSE = 77,
        KW_ERASE_LINE = 78,
        KW_ERASE_RECT = 79,
        KW_FILL_ELLIPSE = 80,
        KW_FILL_RECT = 81,
        KW_GET_LCD_SCREEN_SIZE = 82,
        KW_GET_PIXEL = 83,   // covers both overloads
        KW_INVERT_LINE = 84,
        KW_INVERT_PIXEL = 85,
        KW_SCROLL_TEXT = 86,   // covers both overloads
        KW_SET_PIXEL = 87,
        TERMAIL = 88,

        GREENCOLOR = 89,
        BLUECOLOR = 90,
        REDCOLOR = 91,
        BLACKCOLOR = 92,
        WHITECOLOR = 93,
        YELLOWCOLOR = 94,
        ELSE = 95,
        ELSE_IF = 96,
        KW_ENUM = 97,
        NOT_EQUALS = 98,
        ABS = 99,
        RANDOM = 100,
        kExitButton = 101,
        kRightButton = 102,
        kLeftButton = 103,
        kEnterButton = 104,
        nNxtButtonPressed = 104,
        NONE,
        FUNCTION_CALL,
        FUNCTION,
        STRUCTBODY,
        CE,
    }



    public class Lexing
    {


        public Lexing(List<Spec> Specs) 
        {
            this.Specs = Specs;
        }
        public static readonly Dictionary<TokenType, Func<string, int, AbstactToken>> TokenFactory = new()
        {
            // End/input control
            [TokenType.EOF] = (text, len) => new EOFToken(text, len),
            [TokenType.ERROR] = (text, len) => new ERRORToken(text, len),

            // Identifiers & literals
            [TokenType.ID] = (text, len) => new IDToken(text, len),
            [TokenType.NUMBER] = (text, len) => new NUMBERToken(text, len),
            [TokenType.CHARVAL] = (text, len) => new CHARVALToken(text, len),
            [TokenType.BOOLVAL] = (text, len) => new BOOLVALToken(text, len),
            [TokenType.STRINGVAL] = (text, len) => new STRINGVALToken(text, len),

            // Types
            [TokenType.KW_INT] = (text, len) => new KW_INTToken(text, len),
            [TokenType.KW_STRING] = (text, len) => new KW_STRINGToken(text, len),
            [TokenType.KW_BOOL] = (text, len) => new KW_BOOLToken(text, len),
            [TokenType.KW_FLOAT] = (text, len) => new KW_FLOATToken(text, len),
            [TokenType.KW_VOID] = (text, len) => new KW_VOIDToken(text, len),
            [TokenType.KW_ENUM] = (text, len) => new KW_ENUMToken(text, len),

            // Decls/structs
            [TokenType.KW_STRUCT] = (text, len) => new KW_STRUCTToken(text, len),

            // Control flow
            [TokenType.KW_IF] = (text, len) => new KW_IFToken(text, len),
            [TokenType.ELSE] = (text, len) => new KW_ELSEToken(text, len),
            [TokenType.ELSE_IF] = (text, len) => new KW_ELSE_IFToken(text, len),
            [TokenType.KW_WHILE] = (text, len) => new KW_WHILEToken(text, len),
            [TokenType.KW_FOR] = (text, len) => new KW_FORToken(text, len),
            [TokenType.KW_SWITCH] = (text, len) => new KW_SWITCHToken(text, len),
            [TokenType.KW_CASE] = (text, len) => new KW_CASEToken(text, len),
            [TokenType.KW_DEFAULT] = (text, len) => new KW_DEFAULTToken(text, len),
            [TokenType.KW_RETURN] = (text, len) => new KW_RETURNToken(text, len),
            [TokenType.KW_BREAK] = (text, len) => new KW_BREAKToken(text, len),
            [TokenType.KW_CONTINUE] = (text, len) => new KW_CONTINUEToken(text, len),

            // Task
            [TokenType.KW_TASK] = (text, len) => new KW_TASKToken(text, len),
            [TokenType.KW_MAIN] = (text, len) => new KW_MAINToken(text, len),

            //Colors

            [TokenType.REDCOLOR] = (text, len) => new REDCOLORToken(text, len),
            [TokenType.BLUECOLOR] = (text, len) => new BLUECOLORToken(text, len),
            [TokenType.YELLOWCOLOR] = (text, len) => new YELLOWCOLORToken(text, len),
            [TokenType.WHITECOLOR] = (text, len) => new WHITECOLORToken(text, len),
            [TokenType.BLACKCOLOR] = (text, len) => new BLUECOLORToken(text, len),
            [TokenType.GREENCOLOR] = (text, len) => new GREENCOLORToken(text, len),

            //Buttons


            [TokenType.kEnterButton] = (text, len) => new KENTERBUTTONToken(text, len),
            [TokenType.kExitButton] = (text, len) => new KEXITBUTTONToken(text, len),
            [TokenType.kLeftButton] = (text, len) => new KLEFTBUTTONToken(text, len),
            [TokenType.kRightButton] = (text, len) => new KRIGHTBUTTONToken(text, len),
            [TokenType.nNxtButtonPressed] = (text, len) => new NXTBUTTONPRESSEDToken(text, len),

            // RobotC-specific identifiers
            [TokenType.KW_MOTOR] = (text, len) => new KW_MOTORToken(text, len),
            [TokenType.KW_NMOTORENCODER] = (text, len) => new KW_NMOTORENCODERToken(text, len),
            [TokenType.KW_SENSORVALUE] = (text, len) => new KW_SENSORVALUEToken(text, len),

            // RobotC enums / ports
            [TokenType.KW_MOTOR_A] = (text, len) => new KW_MOTOR_AToken(text, len),
            [TokenType.KW_MOTOR_B] = (text, len) => new KW_MOTOR_BToken(text, len),
            [TokenType.KW_MOTOR_C] = (text, len) => new KW_MOTOR_CToken(text, len),
            [TokenType.KW_PORT1] = (text, len) => new KW_PORT1Token(text, len),
            [TokenType.KW_PORT2] = (text, len) => new KW_PORT2Token(text, len),
            [TokenType.KW_PORT3] = (text, len) => new KW_PORT3Token(text, len),
            [TokenType.KW_PORT4] = (text, len) => new KW_PORT4Token(text, len),

            // Operators
            [TokenType.OP_ASSIGN] = (text, len) => new OP_ASSIGNToken(text, len),
            [TokenType.OP_PLUS] = (text, len) => new OP_PLUSToken(text, len),
            [TokenType.OP_MINUS] = (text, len) => new OP_MINUSToken(text, len),
            [TokenType.OP_STAR] = (text, len) => new OP_STARToken(text, len),
            [TokenType.OP_SLASH] = (text, len) => new OP_SLASHToken(text, len),
            [TokenType.OP_PERCENT] = (text, len) => new OP_PERCENTToken(text, len),
            [TokenType.NOT_EQUALS] = (text, len) => new NOTEQUALSToken(text, len),

            [TokenType.OP_ANDAND] = (text, len) => new OP_ANDANDToken(text, len),
            [TokenType.OP_OROR] = (text, len) => new OP_ORORToken(text, len),
            [TokenType.OP_EQEQ] = (text, len) => new OP_EQEQToken(text, len),
            [TokenType.OP_GT] = (text, len) => new OP_GTToken(text, len),
            [TokenType.OP_LT] = (text, len) => new OP_LTToken(text, len),

            [TokenType.OP_PLUSEQ] = (text, len) => new OP_PLUSEQToken(text, len),
            [TokenType.OP_MINUSEQ] = (text, len) => new OP_MINUSEQToken(text, len),
            [TokenType.OP_PLUSPLUS] = (text, len) => new OP_PLUSPLUSToken(text, len),
            [TokenType.OP_MINUSMINUS] = (text, len) => new OP_MINUSMINUSToken(text, len),

            [TokenType.OP_DOT] = (text, len) => new OP_DOTToken(text, len),

            // Punctuation
            [TokenType.LPAREN] = (text, len) => new LPARENToken(text, len),
            [TokenType.RPAREN] = (text, len) => new RPARENToken(text, len),
            [TokenType.LBRACE] = (text, len) => new LBRACEToken(text, len),
            [TokenType.RBRACE] = (text, len) => new RBRACEToken(text, len),
            [TokenType.LBRACKET] = (text, len) => new LBRACKETToken(text, len),
            [TokenType.RBRACKET] = (text, len) => new RBRACKETToken(text, len),
            [TokenType.COMMA] = (text, len) => new COMMAToken(text, len),
            [TokenType.SEMI] = (text, len) => new SEMIToken(text, len),
            [TokenType.COLON] = (text, len) => new COLONToken(text, len),
            [TokenType.WHITE_SPACE] = (text, len) => new WHITE_SPACEToken(text, len),
            [TokenType.KW_DEFINE] = (text, len) => new KW_DEFINEToken(text, len),
            [TokenType.KW_DELAY] = (text, len) => new KW_DELAYToken(text, len),

            [TokenType.KW_CLEAR_PIXEL] = (text, len) => new KW_CLEAR_PIXELToken(text, len),
            [TokenType.KW_DISPLAY_BIG_STRING_AT] = (text, len) => new KW_DISPLAY_BIG_STRING_ATToken(text, len),
            [TokenType.KW_DISPLAY_TEXT_LINE] = (text, len) => new KW_DISPLAY_TEXT_LINEToken(text, len),
            [TokenType.KW_DISPLAY_CENTERED_BIG_TEXT_LINE] = (text, len) => new KW_DISPLAY_CENTERED_BIG_TEXT_LINEToken(text, len),
            [TokenType.KW_DISPLAY_CENTERED_TEXT_LINE] = (text, len) => new KW_DISPLAY_CENTERED_TEXT_LINEToken(text, len),
            [TokenType.KW_DISPLAY_BIG_TEXT_LINE] = (text, len) => new KW_DISPLAY_BIG_TEXT_LINEToken(text, len),
            [TokenType.KW_DISPLAY_RIC_FILE] = (text, len) => new KW_DISPLAY_RIC_FILEToken(text, len),
            [TokenType.KW_DISPLAY_STRING] = (text, len) => new KW_DISPLAY_STRINGToken(text, len),
            [TokenType.KW_DISPLAY_STRING_AT] = (text, len) => new KW_DISPLAY_STRING_ATToken(text, len),

            [TokenType.KW_DRAW_ELLIPSE] = (text, len) => new KW_DRAW_ELLIPSEToken(text, len),
            [TokenType.KW_DRAW_INVERT_ELLIPSE] = (text, len) => new KW_DRAW_INVERT_ELLIPSEToken(text, len),
            [TokenType.KW_DRAW_INVERT_RECT] = (text, len) => new KW_DRAW_INVERT_RECTToken(text, len),
            [TokenType.KW_DRAW_LINE] = (text, len) => new KW_DRAW_LINEToken(text, len),
            [TokenType.KW_DRAW_RECT] = (text, len) => new KW_DRAW_RECTToken(text, len),

            [TokenType.KW_ERASE_DISPLAY] = (text, len) => new KW_ERASE_DISPLAYToken(text, len),
            [TokenType.KW_ERASE_ELLIPSE] = (text, len) => new KW_ERASE_ELLIPSEToken(text, len),
            [TokenType.KW_ERASE_LINE] = (text, len) => new KW_ERASE_LINEToken(text, len),
            [TokenType.KW_ERASE_RECT] = (text, len) => new KW_ERASE_RECTToken(text, len),

            [TokenType.KW_FILL_ELLIPSE] = (text, len) => new KW_FILL_ELLIPSEToken(text, len),
            [TokenType.KW_FILL_RECT] = (text, len) => new KW_FILL_RECTToken(text, len),

            [TokenType.KW_GET_LCD_SCREEN_SIZE] = (text, len) => new KW_GET_LCD_SCREEN_SIZEToken(text, len),
            [TokenType.KW_GET_PIXEL] = (text, len) => new KW_GET_PIXELToken(text, len),

            [TokenType.KW_INVERT_LINE] = (text, len) => new KW_INVERT_LINEToken(text, len),
            [TokenType.KW_INVERT_PIXEL] = (text, len) => new KW_INVERT_PIXELToken(text, len),

            [TokenType.KW_SCROLL_TEXT] = (text, len) => new KW_SCROLL_TEXTToken(text, len),
            [TokenType.KW_SET_PIXEL] = (text, len) => new KW_SET_PIXELToken(text, len),
            [TokenType.ABS] = (text, len) => new KW_ABSToken(text, len),
            [TokenType.RANDOM] = (text, len) => new KW_RANDOMToken(text, len),
        };
        public List<Spec> Specs;
        public AbstactToken GetType(ReadOnlySpan<char> input, int index)
        {
            AbstactToken highest = TokenFactory[TokenType.ERROR].Invoke("", 0);
            input = input.Slice(index);
            int highestPres = int.MaxValue;
            for (int i = 0; i < Specs.Count; i++)
            {
                if (Regex.IsMatch(input, Specs[i].Pattern))
                {
                    if (highestPres > Specs[i].Precedence)
                    {
                        Match match = Regex.Match(input.ToString(), Specs[i].Pattern);
                        if (match.Index == 0)
                        {
                            highest = TokenFactory[Specs[i].Type].Invoke(match.Value, match.Length);
                            highestPres = Specs[i].Precedence;
                        }
                    }
                }
            }
            return highest;
        }

        public List<AbstactToken> Lexering(string path)
        {
            string[] lines = File.ReadAllLines(path);
            List<AbstactToken> tokens = new List<AbstactToken>();
            for (int i = 0; i < lines.Length; i++)
            {
                int currentIndex = 0;
                while (currentIndex < lines[i].Length)
                {
                    AbstactToken currentToken = GetType(lines[i], currentIndex);
                    if (currentToken.GetTokenType() != TokenType.ERROR)
                    {
                        tokens.Add(currentToken);
                    }
                    else
                    {
                        throw new Exception("Unknown ");
                    }
                    currentIndex += currentToken.length;
                }
            }
            return tokens;
        }
    }
}
