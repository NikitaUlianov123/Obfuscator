using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace C_Obfuscation
{

    // Replaces identifiers and string literals
    internal class ObfuscatingRewriter : CSharpSyntaxRewriter
    {
        // semantic model to resolve symbols from roslyn syntax nodes
        private SemanticModel model;
        // map of symbols to new names
        private Dictionary<ISymbol, string> map;
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
        public ObfuscatingRewriter(SemanticModel model, Dictionary<ISymbol, string> map)
        {
            this.model = model;
            this.map = map;
            EncodedStringsUsed = false;
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            ISymbol? sym = model.GetDeclaredSymbol(node);
            if (sym != null && map.TryGetValue(sym, out var newName))
            {
                IdentifierNameSyntax newnode = node.WithIdentifier(SyntaxFactory.Identifier(newName));
                new
                return 
            }
            return base.VisitIdentifierName(node);
        }

        public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            ISymbol? sym = model.GetDeclaredSymbol(node);
            if (sym != null && map.TryGetValue(sym, out var newName))
            {
                node = node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }
            return base.VisitVariableDeclarator(node);
        }

        public override SyntaxNode VisitParameter(ParameterSyntax node)
        {
            ISymbol? sym = model.GetDeclaredSymbol(node);
            if (sym != null && map.TryGetValue(sym, out var newName))
            {
                node = node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }
            return base.VisitParameter(node);
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            ISymbol? sym = model.GetDeclaredSymbol(node);
            if (sym != null && map.TryGetValue(sym, out var newName))
            {
                node = node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }
            return base.VisitMethodDeclaration(node);
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            ISymbol? sym = model.GetDeclaredSymbol(node);
            if (sym != null && map.TryGetValue(sym, out var newName))
            {
                node = node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }
            return base.VisitPropertyDeclaration(node);
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            var decl = node;
            var vars = new List<VariableDeclaratorSyntax>();
            foreach (var v in node.Declaration.Variables)
            {
                ISymbol? sym = model.GetDeclaredSymbol(v);
                if (sym != null && map.TryGetValue(sym, out var newName))
                {
                    vars.Add(v.WithIdentifier(SyntaxFactory.Identifier(newName)));
                }
                else vars.Add(v);
            }
            return base.VisitFieldDeclaration(node.WithDeclaration(node.Declaration.WithVariables(SyntaxFactory.SeparatedList(vars))));
        }

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

                // encode as byte array literal and call DecodeString
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

                //encode numeric literal as byte array
                object? value = node.Token.Value;
                byte[] bytes = value switch
                {
                    int i => BitConverter.GetBytes(i),
                    long l => BitConverter.GetBytes(l),
                    float f => BitConverter.GetBytes(f),
                    double d => BitConverter.GetBytes(d),
                    short s => BitConverter.GetBytes(s),
                    byte b => new byte[] { b },
                    _ => throw new NotSupportedException($"Unsupported numeric type: {value.GetType()}")
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

        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            // if token maps to a symbol we have renamed, replace text

            if (token.IsKind(SyntaxKind.IdentifierToken))
            {
                ISymbol? sym = GetSymbolFromToken(model, token);
                if (sym != null && map.TryGetValue(sym, out var newName))
                {
                    return SyntaxFactory.Identifier(token.LeadingTrivia, newName, token.TrailingTrivia);
                }
            }
            return base.VisitToken(token);
        }
        byte CircularShift(byte b)
        {
            return (byte)((b << 4) | (b >> 4));
        }
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
        private ISymbol? GetSymbolFromToken(SemanticModel model, SyntaxToken token)
        {
            SyntaxNode? node = token.Parent;

            while (node != null)
            {
                var info = model.GetSymbolInfo(node);
                if (info.Symbol != null)
                    return info.Symbol;

                if (info.CandidateSymbols.Length > 0)
                    return info.CandidateSymbols[0];

                var decl = model.GetDeclaredSymbol(node);
                if (decl != null)
                    return decl;

                node = node.Parent;
            }

            return null;
        }

    }
}
