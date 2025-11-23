using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace C_Obfuscation
{
    // Rewriter that applies renames using annotations (no SemanticModel usage here).
    // Also performs the literal encoding/obfuscation (kept as in your original).
    internal class ObfuscatingRewriter : CSharpSyntaxRewriter
    {
        // map: symbolKey -> newName
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

        // Helper: get rename key annotation from a node, if present
        private static string? TryGetRenameKey(SyntaxNode? node)
        {
            if (node == null) return null;
            var a = node.GetAnnotations("RenameKey").FirstOrDefault();
            return a?.Data;
        }

        // Helper: rename an identifier token preserving trivia
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

        // -----------------------------
        // Declarations / identifier nodes
        // -----------------------------
        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            string? key = TryGetRenameKey(node);
            if (key != null && renameMap.TryGetValue(key, out var newName))
            {
                return node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }

            return base.VisitIdentifierName(node);
        }

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

        // -----------------------------
        // Tokens: ensure any identifier tokens that weren't handled get renamed
        // -----------------------------
        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            // For identifier tokens, check parent annotation
            if (token.IsKind(SyntaxKind.IdentifierToken))
            {
                return RenameIdentifierTokenIfNeeded(token);
            }

            return base.VisitToken(token);
        }

        // -----------------------------
        // Literal obfuscation (kept as your original logic)
        // -----------------------------
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

        private byte CircularShift(byte b)
        {
            return (byte)((b << 4) | (b >> 4));
        }

        // fallback decode helper - left unchanged from prior
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
