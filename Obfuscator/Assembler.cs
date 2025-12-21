using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuscator
{
    public static class Assembler
    {
        static Dictionary<string, byte> opCodes = new Dictionary<string, byte>()
        {
            // 3. STACK OPERATIONS
            { "PUSH_CONST_INT",    0x00 },
            { "PUSH_CONST_CHAR",   0x01 },
            { "PUSH_CONST_STRING", 0x02 },
            { "PUSH_CONST_BOOL",   0x03 },

            { "POP_LOCAL_INT",     0x04 },
            { "POP_LOCAL_CHAR",    0x05 },
            { "POP_LOCAL_STRING",  0x06 },
            { "POP_LOCAL_BOOL",    0x07 },
            { "DUP",               0x08 },
            { "POP",               0x09 },
            { "DELAY",             0x0A },

            { "PUSH_LOCAL_INT",    0x0B },
            { "PUSH_LOCAL_CHAR",   0x0C },
            { "PUSH_LOCAL_STRING", 0x0D },
            { "PUSH_LOCAL_BOOL",   0x0E },

            // 4. ARITHMETIC OPERATIONS
            { "ADD",               0x10 },
            { "SUB",               0x11 },
            { "MUL",               0x12 },
            { "DIV",               0x13 },
            { "NEG",               0x14 },
            { "MOD",               0x15 },

            // 5. COMPARISON AND BOOLEAN
            { "LT",                0x20 },
            { "GT",                0x21 },
            { "EQ",                0x22 },
            { "NE",                0x23 },
            { "LE",                0x24 },
            { "GE",                0x25 },
            { "AND",               0x26 },
            { "OR",                0x27 },

            // 6. STRUCT SUPPORT
            { "NEW_STRUCT",        0x40 },
            { "STORE_FIELD",       0x41 },
            { "LOAD_FIELD",        0x42 },

            // 7. HARDWARE INTRINSICS
            { "MOTOR_SET",         0x50 },
            { "MOTOR_GET",         0x51 },
            { "ENCODER_SET",       0x52 },
            { "ENCODER_GET",       0x53 },
            { "SENSOR_GET",        0x54 },

            // Display intrinsics
            { "DISP_STRING",       0x60 },
            { "DISP_TEXTLINE",     0x61 },
            { "CLEAR_SCREEN",      0x62 },

            // 8. FUNCTION CALLS
            { "CALL",              0x70 },
            { "RET",               0x71 },

            // 9. VM CONTROL FLOW
            { "JMP",               0x80 },
            { "JZ",                0x81 },
            { "JNZ",               0x82 },
        };

        public static byte[] Assemble(string irCode)
        {
            string[] lines = irCode.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

            List<byte> bytecode = new List<byte>();

            foreach (string line in lines)
            {
                string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (opCodes.TryGetValue(parts[0], out byte opcode))
                {
                    bytecode.Add(opcode);
                    for (int i = 1; i < 4; i++)
                    {
                        if (i >= parts.Length)
                        {
                            bytecode.Add(0);
                            continue;
                        }

                        if (byte.TryParse(parts[i], out byte value))
                        {
                            bytecode.Add(value);
                        }
                        else if (parts[i].StartsWith("\"") && parts[i].EndsWith("\""))
                        {
                            string strValue = parts[i].Trim('"');
                            byte[] strBytes = Encoding.UTF8.GetBytes(strValue);
                            bytecode.AddRange(BitConverter.GetBytes(strBytes.Length));
                            bytecode.AddRange(strBytes);
                        }
                        else if (bool.TryParse(parts[i], out bool boolValue))
                        {
                            bytecode.Add((byte)(boolValue ? 1 : 0));
                        }
                        else
                        {
                            throw new Exception($"Invalid operand: {parts[i]}");
                        }
                    }
                }
                else
                {
                    throw new Exception($"Unknown IR instruction: {parts[0]}");
                }
            }

            return bytecode.ToArray();
        }
    }
}