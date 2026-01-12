using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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

        public static (byte[] code, byte[] heap) Assemble(string irCode)
        {
            string[] lines = irCode.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

            bool isInFunction = false;
            List<byte> bytecode = [];
            List<byte> heap = [];

            foreach (string line in lines)
            {
                string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (opCodes.TryGetValue(parts[0], out byte opcode))
                {
                    List<byte> current = [];
                    current.Add(opcode);
                    for (int i = 1; i < 4; i++)
                    {
                        if (i >= parts.Length)
                        {
                            current.Add(0);
                            continue;
                        }

                        if (byte.TryParse(parts[i], out byte value))
                        {
                            current.Add(value);
                        }
                        else if (short.TryParse(parts[i], out short shorty))
                        {
                            current.Add((byte)(shorty >> 8));
                            current.Add((byte)(shorty));
                            i++;
                        }
                        else if (parts[i].StartsWith("\"") && parts[i].EndsWith("\""))
                        {
                            string strValue = parts[i].Trim('"');
                            byte[] strBytes = Encoding.UTF8.GetBytes(strValue);
                            current.AddRange(BitConverter.GetBytes(strBytes.Length));
                            current.AddRange(strBytes);
                        }
                        else if (bool.TryParse(parts[i], out bool boolValue))
                        {
                            current.Add((byte)(boolValue ? 1 : 0));
                        }
                        else
                        {
                            throw new Exception($"Invalid operand: {parts[i]}");
                        }
                    }
                    if (isInFunction)
                    {
                        heap.AddRange(current);
                    }
                    else
                    {
                        bytecode.AddRange(current);
                    }
                }
                else if (parts[0][^1] == ':')// function definition
                {
                    isInFunction = true;
                }
                else if (parts[0][^1] == '*')// main definition
                {
                    isInFunction = false;
                }
                else
                {
                    throw new Exception($"Unknown IR instruction: {parts[0]}");
                }
            }

            return (bytecode.ToArray(), heap.ToArray());
        }

        public static void PackageFile(string irCode, string outputPath)
        {
            using RSA rsa = RSA.Create();

            string publicPem = rsa.ExportSubjectPublicKeyInfoPem();
            string privatePem = rsa.ExportPkcs8PrivateKeyPem();

            File.WriteAllText("public.pem", publicPem);
            File.WriteAllText("private.pem", privatePem);

            rsa.ImportFromPem(File.ReadAllText("private.pem"));

            (byte[] code, byte[] heap) assembled = Assemble(irCode);

            string encrypted = Convert.ToBase64String(rsa.Encrypt(
                Encoding.UTF8.GetBytes(irCode),
                RSAEncryptionPadding.OaepSHA256
            ));

            File.WriteAllText(outputPath, encrypted + "\n");

            File.AppendAllText(outputPath, Convert.ToBase64String(assembled.code) + "\n");
            File.AppendAllText(outputPath, Convert.ToBase64String(assembled.heap));
            ;
        }

        public static (byte[] code, byte[] heap, string original) UnpackageFile(string inputPath)
        {
            string[] lines = File.ReadAllLines(inputPath);
            if (lines.Length < 3)
            {
                throw new Exception("Invalid package file format.");
            }

            using RSA rsa = RSA.Create();
            rsa.ImportFromPem(File.ReadAllText("private.pem"));

            string decrypted = Encoding.UTF8.GetString(rsa.Decrypt(
                Convert.FromBase64String(lines[0]),
                RSAEncryptionPadding.OaepSHA256
            ));


            byte[] code = Convert.FromBase64String(lines[1]);
            byte[] heap = Convert.FromBase64String(lines[2]);

            return (code, heap, decrypted);
        }
    }
}