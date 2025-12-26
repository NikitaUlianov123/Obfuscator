using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

namespace C_DeObfuscator
{
    public class WhileLoopDeobfuscator : CSharpSyntaxRewriter
    {
        private Stack<LoopContext> loopContextStack = new Stack<LoopContext>();

        public override SyntaxNode VisitBlock(BlockSyntax node)
        {
            WhileLoopComponents components;
            if (IsObfuscatedWhileLoop(node, out components))
            {
                loopContextStack.Push(new LoopContext
                (
                    ContinueLabel: components.ContinueLabel,
                    BreakLabel: components.BreakLabel
                ));

                // Visit the body to handle nested loops and replace goto statements
                StatementSyntax processedBody = (StatementSyntax)Visit(components.Body);

                // Pop this loop's context
                loopContextStack.Pop();

                // Build the while statement
                WhileStatementSyntax whileStatement = SyntaxFactory.WhileStatement(
                    condition: components.Condition,
                    statement: processedBody
                );

                return whileStatement.WithTriviaFrom(node);
            }

            // If not an obfuscated loop, visit children normally
            return base.VisitBlock(node);
        }

        public override SyntaxNode VisitGotoStatement(GotoStatementSyntax node)
        {
            if (node.Kind() == SyntaxKind.GotoStatement && node.Expression is IdentifierNameSyntax identifier)
            {
                string labelName = identifier.Identifier.Text;

                if (loopContextStack.Count > 0)
                {
                    LoopContext currentContext = loopContextStack.Peek();

                    // Check if this goto is a continue
                    if (currentContext.ContinueLabel != null && labelName == currentContext.ContinueLabel)
                    {
                        return SyntaxFactory.ContinueStatement().WithTriviaFrom(node);
                    }

                    // Check if this goto is a break
                    if (currentContext.BreakLabel != null && labelName == currentContext.BreakLabel)
                    {
                        return SyntaxFactory.BreakStatement().WithTriviaFrom(node);
                    }
                }
            }

            return base.VisitGotoStatement(node);
        }

        private bool IsObfuscatedWhileLoop(BlockSyntax block, out WhileLoopComponents components)
        {
            components = null;
            var statements = block.Statements;

            if (statements.Count < 5)
                return false;

            if (!(statements[0] is LabeledStatementSyntax firstLabel))
                return false;

            string firstLabelText = firstLabel.Identifier.Text;
            if (!firstLabelText.Contains("whileLoop") || !firstLabelText.Contains("outerStartLabel"))
                return false;

            if (!(statements[2] is BlockSyntax innerBlock))
                return false;

            if (!(statements[statements.Count - 1] is LabeledStatementSyntax outerEndLabel))
                return false;

            string breakLabel = outerEndLabel.Identifier.Text;

            return ParseInnerBlock(innerBlock, breakLabel, out components);
        }

        private bool ParseInnerBlock(BlockSyntax innerBlock, string breakLabel, out WhileLoopComponents components)
        {
            components = null;
            var stmts = innerBlock.Statements;

            if (stmts.Count < 5)
                return false;

            // 0: startLabel
            // 1: if (!condition) goto breakLabel
            // 2: body
            // 3: continueLabel
            // 4: goto startLabel

            // Extract start label
            if (!(stmts[0] is LabeledStatementSyntax startLabel))
                return false;

            string startLabelName = startLabel.Identifier.Text;

            // Extract condition from the if statement
            if (!(stmts[1] is IfStatementSyntax conditionIf))
                return false;

            if (!(conditionIf.Condition is PrefixUnaryExpressionSyntax notExpr) ||
                notExpr.Kind() != SyntaxKind.LogicalNotExpression)
                return false;

            ExpressionSyntax condition = notExpr.Operand;

            // Remove parentheses if present
            if (condition is ParenthesizedExpressionSyntax parenCond)
                condition = parenCond.Expression;

            // Extract body (statement at index 2)
            StatementSyntax body = stmts[2];

            // Extract continue label
            if (!(stmts[3] is LabeledStatementSyntax continueLabel))
                return false;

            string continueLabelName = continueLabel.Identifier.Text;

            // Build components
            components = new WhileLoopComponents
            {
                Condition = condition,
                Body = body,
                ContinueLabel = continueLabelName,
                BreakLabel = breakLabel
            };

            return true;
        }

        private class WhileLoopComponents
        {
            public ExpressionSyntax Condition { get; set; }
            public StatementSyntax Body { get; set; }
            public string ContinueLabel { get; set; }
            public string BreakLabel { get; set; }
        }
    }
}