using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace C_Obfuscation
{
    // Replaces identifiers and string literals
    public class ObfuscatingRewriter : CSharpSyntaxRewriter
    {
        // semantic model to resolve symbols from roslyn syntax nodes
        private readonly SemanticModel _model;
        // map of symbol-key -> new name
        private readonly Dictionary<ISymbol, string> _map;
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
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _map = map ?? throw new ArgumentNullException(nameof(map));
            EncodedStringsUsed = false;
        }
        private static ISymbol Canonicalize(ISymbol sym) => sym.OriginalDefinition ?? sym;

        

        // try to get map key for a syntax node by resolving its symbol
        // Defensive: ensure node belongs to model.SyntaxTree and handle exceptions
        private ISymbol? TryGetRenameSymbol(SyntaxNode? node)
        {
            if (node is null) return null;
            if (node.SyntaxTree != _model.SyntaxTree) return null;

            try
            {
                var info = _model.GetSymbolInfo(node);
                ISymbol? sym = info.Symbol;

                if (sym is null && info.CandidateSymbols.Length > 0)
                    sym = info.CandidateSymbols.FirstOrDefault();

                // For some declaration-ish nodes, SymbolInfo can be null; try declared symbol.
                if (sym is null)
                    sym = _model.GetDeclaredSymbol(node);

                if (sym is null) return null;
                return Canonicalize(sym);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        // helper to safely resolve declared symbol for a node (returns null on mismatch/exception)
        private ISymbol? TryGetDeclaredSymbol(SyntaxNode node)
        {
            if (node.SyntaxTree != _model.SyntaxTree) return null;
            try
            {
                var sym = _model.GetDeclaredSymbol(node);
                return sym is null ? null : Canonicalize(sym);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        // helper to safely resolve symbol info for a node (returns null on mismatch/exception)
        private ISymbol? TryGetSymbolInfo(SyntaxNode node)
        {
            if (node.SyntaxTree != _model.SyntaxTree) return null;
            try
            {
                var info = _model.GetSymbolInfo(node);
                var sym = info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
                return sym is null ? null : Canonicalize(sym);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            var sym = TryGetRenameSymbol(node);
            node = (IdentifierNameSyntax)base.VisitIdentifierName(node);
            if (sym != null && _map.TryGetValue(sym, out var newName))
            {
                return node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }
            return node;
        }

        public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            var sym = TryGetDeclaredSymbol(node);
            node = (VariableDeclaratorSyntax)base.VisitVariableDeclarator(node);
            if (sym != null && _map.TryGetValue(sym, out var newName))
            {
                node = node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }
            return node;
        }

        public override SyntaxNode VisitParameter(ParameterSyntax node)
        {
            var sym = TryGetDeclaredSymbol(node);
            node = (ParameterSyntax)base.VisitParameter(node);
            if (sym != null && _map.TryGetValue(sym, out var newName))
            {
                node = node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }
            return node;
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var sym = TryGetDeclaredSymbol(node);
            node = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node);
            if (sym != null && _map.TryGetValue(sym, out var newName))
            {
                node = node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }
            return node;
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            var sym = TryGetDeclaredSymbol(node);
            node = (PropertyDeclarationSyntax)base.VisitPropertyDeclaration(node);
            if (sym != null && _map.TryGetValue(sym, out var newName))
            {
                node = node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }
            return node;
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            // Note: FieldDeclaration declares one or more variables, handled by VisitVariableDeclarator.
            // Keep this override if you had other logic; otherwise base is fine.
            return base.VisitFieldDeclaration(node);
        }

        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            // Your original approach renames identifier tokens based on parent node symbol.
            // Keep that behavior but lookup by ISymbol directly.
            if (token.IsKind(SyntaxKind.IdentifierToken))
            {
                var parent = token.Parent;

                if (parent != null && parent.SyntaxTree == _model.SyntaxTree)
                {
                    try
                    {
                        // Prefer SymbolInfo for usage sites; fall back to declared symbol where needed.
                        var sym = TryGetSymbolInfo(parent) ?? TryGetDeclaredSymbol(parent);

                        if (sym != null && _map.TryGetValue(sym, out var newName))
                        {
                            return SyntaxFactory.Identifier(token.LeadingTrivia, newName, token.TrailingTrivia);
                        }
                    }
                    catch (ArgumentException)
                    {
                        // fall through
                    }
                }
            }

            return base.VisitToken(token);
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

        // decode literal from byte array (kept for ease-of-testing if needed)
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
