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


            if (node.IsKind(SyntaxKind.LogicalAndExpression))
            {
                var left = node.Left;
                var right = node.Right;
                var newLeft = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, SyntaxFactory.ParenthesizedExpression(left));
                var newRight = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, SyntaxFactory.ParenthesizedExpression(right));
                var newNode = SyntaxFactory.BinaryExpression(SyntaxKind.LogicalOrExpression, newLeft, newRight);
            }

            return base.VisitBinaryExpression(node);
        }
    }
}
