using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharp_DeObfuscator
{
    public class DemorganDeobfuscator : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            // Visit children first
            PrefixUnaryExpressionSyntax visitedNode = (PrefixUnaryExpressionSyntax)base.VisitPrefixUnaryExpression(node)!;

            // Check if this is a logical NOT expression
            if (visitedNode.Kind() != SyntaxKind.LogicalNotExpression)
            {
                return visitedNode;
            }

            // Get the operand (what's being negated)
            ExpressionSyntax operand = visitedNode.Operand;

            // Remove parentheses if present
            if (operand is ParenthesizedExpressionSyntax parenExpr)
            {
                operand = parenExpr.Expression;
            }

            // Check if the operand is a binary expression (AND or OR)
            if (!(operand is BinaryExpressionSyntax binaryExpr))
            {
                return visitedNode;
            }

            SyntaxKind binaryKind = binaryExpr.Kind();

            // Check if it's !(A || B) or !(A && B)
            if (binaryKind != SyntaxKind.LogicalOrExpression && binaryKind != SyntaxKind.LogicalAndExpression)
            {
                return visitedNode;
            }

            // Check if both sides are negated
            ExpressionSyntax left = binaryExpr.Left;
            ExpressionSyntax right = binaryExpr.Right;

            // Try to extract negated expressions
            ExpressionSyntax? unnegatedLeft = TryRemoveNegation(left);
            ExpressionSyntax? unnegatedRight = TryRemoveNegation(right);

            // Both sides must be negated for De Morgan's reversal
            if (unnegatedLeft == null || unnegatedRight == null)
            {
                return visitedNode;
            }

            // Apply De Morgan's Law in reverse:
            // !((!A) || (!B)) -> A && B
            // !((!A) && (!B)) -> A || B
            SyntaxKind newKind = binaryKind == SyntaxKind.LogicalOrExpression
                ? SyntaxKind.LogicalAndExpression
                : SyntaxKind.LogicalOrExpression;

            BinaryExpressionSyntax result = SyntaxFactory.BinaryExpression(
                newKind,
                unnegatedLeft,
                unnegatedRight
            );

            return result.WithTriviaFrom(visitedNode);
        }

        private ExpressionSyntax? TryRemoveNegation(ExpressionSyntax expr)
        {
            // Remove parentheses
            if (expr is ParenthesizedExpressionSyntax parenExpr)
            {
                expr = parenExpr.Expression;
            }

            // Check if it's a NOT expression
            if (expr is PrefixUnaryExpressionSyntax prefixExpr &&
                prefixExpr.Kind() == SyntaxKind.LogicalNotExpression)
            {
                ExpressionSyntax operand = prefixExpr.Operand;

                // Remove parentheses from the operand
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