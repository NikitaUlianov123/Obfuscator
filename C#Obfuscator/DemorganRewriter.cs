using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_Obfuscator
{
    internal class DemorganRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            if (node == null)
            {
                return null;
            }
            
            ExpressionSyntax left = (ExpressionSyntax)Visit(node.Left);
            ExpressionSyntax right = (ExpressionSyntax)Visit(node.Right);
            SyntaxKind kind = node.Kind();

            if(kind == SyntaxKind.LogicalAndExpression || kind == SyntaxKind.LogicalOrExpression)
            {
                PrefixUnaryExpressionSyntax newLeft = LogicalNot(left);
                PrefixUnaryExpressionSyntax newRight = LogicalNot(right);
                SyntaxKind newKind = kind == SyntaxKind.LogicalAndExpression ? SyntaxKind.LogicalOrExpression : SyntaxKind.LogicalAndExpression;
                LogicalNot(SyntaxFactory.BinaryExpression(newKind, newLeft, newRight));
                ExpressionSyntax newBinary = LogicalNot(SyntaxFactory.BinaryExpression(newKind, newLeft, newRight));
                return newBinary;
            }

            return base.VisitBinaryExpression(node);
        }

        private static PrefixUnaryExpressionSyntax LogicalNot(ExpressionSyntax expr) => SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, SyntaxFactory.ParenthesizedExpression(expr));
    }
}
