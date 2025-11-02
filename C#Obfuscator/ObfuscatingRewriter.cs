using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C_Obfuscation
{

    // Replaces identifiers and string literals
    internal class ObfuscatingRewriter : CSharpSyntaxRewriter
    {
        // semantic model to resolve symbols from roslyn syntax nodes
        private SemanticModel _model;
        // map of symbols to new names
        private Dictionary<ISymbol, string> _map;
        public bool EncodedStringsUsed { get; private set; }

        public ObfuscatingRewriter(SemanticModel model, Dictionary<ISymbol, string> map)
        {
            _model = model;
            _map = map;
            EncodedStringsUsed = false;
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            var sym = _model.GetSymbolInfo(node).Symbol;
            if (sym != null && _map.TryGetValue(sym, out var newName))
            {
                return node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }
            return base.VisitIdentifierName(node);
        }

        public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            var sym = _model.GetDeclaredSymbol(node);
            if (sym != null && _map.TryGetValue(sym, out var newName))
            {
                node = node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }
            return base.VisitVariableDeclarator(node);
        }

        public override SyntaxNode VisitParameter(ParameterSyntax node)
        {
            var sym = _model.GetDeclaredSymbol(node);
            if (sym != null && _map.TryGetValue(sym, out var newName))
            {
                node = node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }
            return base.VisitParameter(node);
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var sym = _model.GetDeclaredSymbol(node);
            if (sym != null && _map.TryGetValue(sym, out var newName))
            {
                node = node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }
            return base.VisitMethodDeclaration(node);
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            var sym = _model.GetDeclaredSymbol(node);
            if (sym != null && _map.TryGetValue(sym, out var newName))
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
                var sym = _model.GetDeclaredSymbol(v);
                if (sym != null && _map.TryGetValue(sym, out var newName))
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
                ExpressionSyntax replacement = SyntaxFactory.ParseExpression($"__ObfHelpers.DecodeString(new byte[]{{{byteElements}}})");
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
                var sym = _model.GetSymbolInfo(token.Parent).Symbol;
                if (sym != null && _map.TryGetValue(sym, out var newName))
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


    }
}
