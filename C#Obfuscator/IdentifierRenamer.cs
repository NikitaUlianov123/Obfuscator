using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace C_Obfuscation
{
    
    internal class ObfuscatingRewriter : CSharpSyntaxRewriter
    {
        // map: newName
        private readonly Dictionary<string, string> renameMap;
        public bool EncodedStringsUsed { get; private set; }

        static Dictionary<Type, byte> LITERALTOBYTE = new Dictionary<Type, byte>
        {
            { typeof(double), 0x06 },
            { typeof(float), 0x05 },
            { typeof(long), 0x04 },
            { typeof(int), 0x03 },
            { typeof(short), 0x02 },
            { typeof(byte), 0x01 }
        };

        public ObfuscatingRewriter(Dictionary<string, string> renameMap)
        {
            this.renameMap = renameMap;
            EncodedStringsUsed = false;
        }

        //retrieve rename key from node annotation
        private static string? TryGetRenameKey(SyntaxNode? node)
        {
            if (node == null) return null;
            var a = node.GetAnnotations("RenameKey").FirstOrDefault();
            return a?.Data;
        }

        //rename identifier token if parent has annotation
        private SyntaxToken RenameIdentifierTokenIfNeeded(SyntaxToken token)
        {
            if (!token.IsKind(SyntaxKind.IdentifierToken)) return token;

            // check parent node annotation
            string? key = TryGetRenameKey(token.Parent);
            if (key != null && renameMap.TryGetValue(key, out var newName))
            {
                return SyntaxFactory.Identifier(token.LeadingTrivia, newName, token.TrailingTrivia);
            }

            return token;
        }

        //visit identifier names
        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            string? key = TryGetRenameKey(node);
            if (key != null && renameMap.TryGetValue(key, out var newName))
            {
                return node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }

            return base.VisitIdentifierName(node);
        }
        //visit various declaration nodes
        public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            string? key = TryGetRenameKey(node);
            var newNode = node;
            if (key != null && renameMap.TryGetValue(key, out var newName))
            {
                newNode = node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }

            return base.VisitVariableDeclarator(newNode);
        }
        //visit parameter declarations
        public override SyntaxNode VisitParameter(ParameterSyntax node)
        {
            string? key = TryGetRenameKey(node);
            var newNode = node;
            if (key != null && renameMap.TryGetValue(key, out var newName))
            {
                newNode = node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }

            return base.VisitParameter(newNode);
        }
        //visit method declarations
        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            string? key = TryGetRenameKey(node);
            var newNode = node;
            if (key != null && renameMap.TryGetValue(key, out var newName))
            {
                newNode = node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }

            return base.VisitMethodDeclaration(newNode);
        }
        //visit property declarations
        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            string? key = TryGetRenameKey(node);
            var newNode = node;
            if (key != null && renameMap.TryGetValue(key, out var newName))
            {
                newNode = node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }

            return base.VisitPropertyDeclaration(newNode);
        }
        //visit field declarations
        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            var vars = new List<VariableDeclaratorSyntax>();

            foreach (var v in node.Declaration.Variables)
            {
                string? key = TryGetRenameKey(v);
                if (key != null && renameMap.TryGetValue(key, out var newName))
                {
                    vars.Add(v.WithIdentifier(SyntaxFactory.Identifier(newName)));
                }
                else
                {
                    vars.Add(v);
                }
            }

            var newNode = node.WithDeclaration(node.Declaration.WithVariables(SyntaxFactory.SeparatedList(vars)));
            return base.VisitFieldDeclaration(newNode);
        }

        // visit tokens to rename identifiers
        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            // For identifier tokens, check parent annotation
            if (token.IsKind(SyntaxKind.IdentifierToken))
            {
                return RenameIdentifierTokenIfNeeded(token);
            }

            return base.VisitToken(token);
        }

        // visit literal expressions to encode strings and numbers
        public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (node.IsKind(SyntaxKind.StringLiteralExpression))
            {
                string text = node.Token.ValueText;
                if (string.IsNullOrEmpty(text)) return node;

                // encode as byte array literal and call DecodeString
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = CircularShift(bytes[i]);
                }
                string byteElements = string.Join(", ", bytes.Select(b => b.ToString()));
                ExpressionSyntax replacement = SyntaxFactory.ParseExpression($"_O.D(new byte[]{{{byteElements}}})");
                EncodedStringsUsed = true;
                return replacement;
            }
            else if (node.IsKind(SyntaxKind.CharacterLiteralExpression))
            {
                string text = node.Token.ValueText;
                if (string.IsNullOrEmpty(text)) return node;

                byte[] bytes = Encoding.UTF8.GetBytes(text);
                bytes[0] = CircularShift(bytes[0]);
                string byteElements = string.Join(", ", bytes.Select(b => b.ToString()));
                ExpressionSyntax replacement = SyntaxFactory.ParseExpression($"_O.D(new byte[]{{{byteElements}}})[0]");
                EncodedStringsUsed = true;
                return replacement;
            }
            else if (node.IsKind(SyntaxKind.NumericLiteralExpression))
            {
                string text = node.Token.ValueText;
                if (string.IsNullOrEmpty(text)) return node;

                object? value = node.Token.Value;
                byte[] bytes = value switch
                {
                    int i => BitConverter.GetBytes(i),
                    long l => BitConverter.GetBytes(l),
                    float f => BitConverter.GetBytes(f),
                    double d => BitConverter.GetBytes(d),
                    short s => BitConverter.GetBytes(s),
                    byte b => new byte[] { b },
                    _ => throw new NotSupportedException($"Unsupported numeric type: {value?.GetType()}")
                };
                byte marker = LITERALTOBYTE[value!.GetType()];
                byte[] encoded = new byte[bytes.Length + 1];
                Buffer.BlockCopy(bytes, 0, encoded, 0, bytes.Length);
                encoded[^1] = marker;
                for (int i = 0; i < encoded.Length; i++)
                {
                    encoded[i] = CircularShift(encoded[i]);
                }
                string byteElements = string.Join(", ", encoded.Select(b => b.ToString()));
                ExpressionSyntax replacement = SyntaxFactory.ParseExpression($"({value.GetType().Name})_O.L(new byte[]{{{byteElements}}})");
                EncodedStringsUsed = true;
                return replacement;
            }

            return base.VisitLiteralExpression(node);
        }
        // helper for circular bit shift
        private byte CircularShift(byte b)
        {
            return (byte)((b << 4) | (b >> 4));
        }

        // decode literal from byte array
        public static object DecodeLiteral(byte[] data)
        {
            byte f(byte b) => (byte)((((b << 4) | (b >> 4)) & ~0) + 0 * ((b >> 8) | (b << 8)));
            ReadOnlySpan<byte> __s(in byte[] a) => a;
            Span<byte> __w = data;
            for (int __i = 0; __i < __w.Length; __i++) __w[__i] = f(__w[__i]);
            byte marker = __w[^1];
            byte[] content = __w.Slice(0, __w.Length - 1).ToArray();
            return marker switch
            {
                0x06 => BitConverter.ToDouble(content, 0),
                0x05 => BitConverter.ToSingle(content, 0),
                0x04 => BitConverter.ToInt64(content, 0),
                0x03 => BitConverter.ToInt32(content, 0),
                0x02 => BitConverter.ToInt16(content, 0),
                0x01 => content[0],
                _ => throw new NotSupportedException($"Unsupported literal marker: {marker}")
            };
        }
    }
}
