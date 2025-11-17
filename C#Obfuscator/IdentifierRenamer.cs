using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace C_Obfuscation
{
    internal class ObfuscatingRewriter : CSharpSyntaxRewriter
    {
        // Now we use string keys, not ISymbol keys
        private readonly Dictionary<string, string> renameMap;

        public bool EncodedStringsUsed { get; private set; }

        public ObfuscatingRewriter(Dictionary<string, string> renameMap)
        {
            this.renameMap = renameMap;
        }

        
        private string GetSymbolKey(SyntaxNode node)
        {
            return node.ToString(); 
        }

        

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            string key = GetSymbolKey(node);

            if (renameMap.TryGetValue(key, out var newName))
            {
                return node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }

            return base.VisitIdentifierName(node);
        }

        public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            string key = GetSymbolKey(node);

            if (renameMap.TryGetValue(key, out var newName))
            {
                node = node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }

            return base.VisitVariableDeclarator(node);
        }

        public override SyntaxNode VisitParameter(ParameterSyntax node)
        {
            string key = GetSymbolKey(node);

            if (renameMap.TryGetValue(key, out var newName))
            {
                node = node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }

            return base.VisitParameter(node);
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            string key = GetSymbolKey(node);

            if (renameMap.TryGetValue(key, out var newName))
            {
                node = node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }

            return base.VisitMethodDeclaration(node);
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            string key = GetSymbolKey(node);

            if (renameMap.TryGetValue(key, out var newName))
            {
                node = node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }

            return base.VisitPropertyDeclaration(node);
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            var vars = new List<VariableDeclaratorSyntax>();

            foreach (var v in node.Declaration.Variables)
            {
                string key = GetSymbolKey(v);

                if (renameMap.TryGetValue(key, out var newName))
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

                byte[] bytes = Encoding.UTF8.GetBytes(text);
                for (int i = 0; i < bytes.Length; i++)
                    bytes[i] = (byte)((bytes[i] << 4) | (bytes[i] >> 4));

                EncodedStringsUsed = true;

                string arr = string.Join(", ", bytes);
                return SyntaxFactory.ParseExpression($"_O.D(new byte[]{{{arr}}})");
            }

            if (node.IsKind(SyntaxKind.CharacterLiteralExpression))
            {
                string text = node.Token.ValueText;
                if (string.IsNullOrEmpty(text)) return node;

                byte[] bytes = Encoding.UTF8.GetBytes(text);
                bytes[0] = (byte)((bytes[0] << 4) | (bytes[0] >> 4));

                EncodedStringsUsed = true;

                string arr = string.Join(", ", bytes);
                return SyntaxFactory.ParseExpression($"_O.D(new byte[]{{{arr}}})[0]");
            }

            return base.VisitLiteralExpression(node);
        }
    }
}
