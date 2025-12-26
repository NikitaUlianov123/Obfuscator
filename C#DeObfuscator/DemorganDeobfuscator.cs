using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharp_DeObfuscator
{
    public class DemorganDeobfuscator : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            PrefixUnaryExpressionSyntax visitedNode = (PrefixUnaryExpressionSyntax)base.VisitPrefixUnaryExpression(node)!;
            if (visitedNode.Kind() != SyntaxKind.LogicalNotExpression)
            {
                return visitedNode;
            }

            ExpressionSyntax operand = visitedNode.Operand;
            if (operand is ParenthesizedExpressionSyntax parenExpr)
            {
                operand = parenExpr.Expression;
            }

            if (!(operand is BinaryExpressionSyntax binaryExpr))
            {
                return visitedNode;
            }

            SyntaxKind binaryKind = binaryExpr.Kind();
            if (binaryKind != SyntaxKind.LogicalOrExpression && binaryKind != SyntaxKind.LogicalAndExpression)
            {
                return visitedNode;
            }

            ExpressionSyntax left = binaryExpr.Left;
            ExpressionSyntax right = binaryExpr.Right;

            ExpressionSyntax? unnegatedLeft = TryRemoveNegation(left);
            ExpressionSyntax? unnegatedRight = TryRemoveNegation(right);
            if (unnegatedLeft == null || unnegatedRight == null)
            {
                return visitedNode;
            }

            SyntaxKind newKind = binaryKind == SyntaxKind.LogicalOrExpression ? SyntaxKind.LogicalAndExpression : SyntaxKind.LogicalOrExpression;
            BinaryExpressionSyntax result = SyntaxFactory.BinaryExpression(
                newKind,
                unnegatedLeft,
                unnegatedRight
            );

            return result.WithTriviaFrom(visitedNode);
        }

        private ExpressionSyntax? TryRemoveNegation(ExpressionSyntax expr)
        {
            if (expr is ParenthesizedExpressionSyntax parenExpr)
            {
                expr = parenExpr.Expression;
            }

            if (expr is PrefixUnaryExpressionSyntax prefixExpr && prefixExpr.Kind() == SyntaxKind.LogicalNotExpression)
            {
                ExpressionSyntax operand = prefixExpr.Operand;
                if (operand is ParenthesizedExpressionSyntax innerParen)
                {
                    operand = innerParen.Expression;
                }

                return operand;
            }

            return null;
        }
    }
}