using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_DeObfuscator
{
    public class WhileLoopDeobfuscator : CSharpSyntaxRewriter
    {
        private Stack<LoopContext> loopContextStack = new Stack<LoopContext>();

        public override SyntaxNode VisitBlock(BlockSyntax node)
        {
            // Check if this block is an obfuscated while loop BEFORE visiting children
            WhileLoopComponents components;
            if (IsObfuscatedWhileLoop(node, out components))
            {
                // Push this loop's context onto the stack
                loopContextStack.Push(new LoopContext
                {
                    ContinueLabel = components.ContinueLabel,
                    BreakLabel = components.BreakLabel
                });

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

                // Check against the current loop context (top of stack)
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

            // The obfuscated pattern should have at least 5 statements in the outer block:
            // outerStartLabel:, if (!true) goto outerStart, innerBlock, goto outerStart, outerEndLabel:
            if (statements.Count < 5)
                return false;

            // Check first statement is a label containing "whileLoop" and "outerStartLabel"
            if (statements[0] is not LabeledStatementSyntax outerStartLabel)
                return false;

            string outerStartName = outerStartLabel.Identifier.Text;
            if (!ContainsWhileLoopPattern(outerStartName, "outerStartLabel"))
                return false;

            // Check last statement is the corresponding outer end label
            if (statements[statements.Count - 1] is not LabeledStatementSyntax outerEndLabel)
                return false;

            string outerEndName = outerEndLabel.Identifier.Text;
            if (!ContainsWhileLoopPattern(outerEndName, "outerEndLabel"))
                return false;

            // Verify middle pattern: if (!true) goto outerStart
            if (statements[1] is not IfStatementSyntax outerIf)
                return false;

            if (outerIf.Condition is not PrefixUnaryExpressionSyntax outerNot ||
                outerNot.Kind() != SyntaxKind.LogicalNotExpression ||
                outerNot.Operand is not LiteralExpressionSyntax outerLit ||
                outerLit.Kind() != SyntaxKind.TrueLiteralExpression)
                return false;

            if (statements[2] is not BlockSyntax innerBlock)
                return false;

            if (statements[3] is not GotoStatementSyntax)
                return false;

            // Extract the break label (outer end label)
            string breakLabel = outerEndName;

            // Parse the inner block
            return ParseInnerBlock(innerBlock, breakLabel, out components);
        }

        private bool ParseInnerBlock(BlockSyntax innerBlock, string breakLabel, out WhileLoopComponents components)
        {
            components = null;
            var stmts = innerBlock.Statements;

            // While loop inner block structure:
            // 1. startLabel:
            // 2. if (!condition) goto breakLabel
            // 3. body (can be a block or single statement)
            // 4. continueLabel:
            // 5. goto startLabel

            if (stmts.Count < 5)
                return false;

            int index = 0;

            // Expect: startLabel with "whileLoop" and "startLabel" pattern
            if (stmts[index] is not LabeledStatementSyntax startLabel)
                return false;

            string startLabelName = startLabel.Identifier.Text;
            if (!ContainsWhileLoopPattern(startLabelName, "startLabel"))
                return false;

            index++;

            // Expect: if (!condition) goto breakLabel
            if (stmts[index] is not IfStatementSyntax conditionIf)
                return false;

            if (conditionIf.Condition is not PrefixUnaryExpressionSyntax notExpr ||
                notExpr.Kind() != SyntaxKind.LogicalNotExpression)
                return false;

            ExpressionSyntax condition = notExpr.Operand;

            // Remove parentheses if present
            if (condition is ParenthesizedExpressionSyntax parenCond)
                condition = parenCond.Expression;

            // Verify the goto points to the break label
            if (conditionIf.Statement is not GotoStatementSyntax condGoto)
                return false;

            if (condGoto.Expression is not IdentifierNameSyntax condGotoId ||
                condGotoId.Identifier.Text != breakLabel)
                return false;

            index++;

            // The next statement is the body (could be a block or single statement)
            if (index >= stmts.Count)
                return false;

            StatementSyntax body = stmts[index];
            index++;

            // Expect: continueLabel
            if (index >= stmts.Count || stmts[index] is not LabeledStatementSyntax continueLabel)
                return false;

            string continueLabelName = continueLabel.Identifier.Text;
            if (!ContainsWhileLoopPattern(continueLabelName, "continueLabel"))
                return false;

            index++;

            // Expect: goto startLabel
            if (index >= stmts.Count || stmts[index] is not GotoStatementSyntax finalGoto)
                return false;

            if (finalGoto.Expression is not IdentifierNameSyntax finalGotoId ||
                finalGotoId.Identifier.Text != startLabelName)
                return false;

            // Verify we've consumed all statements
            if (index != stmts.Count - 1)
                return false;

            components = new WhileLoopComponents
            {
                Condition = condition,
                Body = body,
                ContinueLabel = continueLabelName,
                BreakLabel = breakLabel
            };

            return true;
        }

        // Helper method to check if a label contains the while loop pattern
        private bool ContainsWhileLoopPattern(string labelName, string expectedPart)
        {
            // The label should contain both "whileLoop" and the expected part
            // (e.g., "startLabel", "continueLabel", "outerStartLabel", "outerEndLabel")
            return labelName.Contains("whileLoop") && labelName.Contains(expectedPart);
        }

        private class WhileLoopComponents
        {
            public ExpressionSyntax Condition { get; set; }
            public StatementSyntax Body { get; set; }
            public string ContinueLabel { get; set; }
            public string BreakLabel { get; set; }
        }

        private class LoopContext
        {
            public string ContinueLabel { get; set; }
            public string BreakLabel { get; set; }
        }
    }
}
