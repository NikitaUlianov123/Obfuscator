using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace C_DeObfuscator
{
    public class DoWhileLoopDeobfuscator : CSharpSyntaxRewriter
    {
        private Stack<LoopContext> loopContextStack = new Stack<LoopContext>();

        public override SyntaxNode VisitBlock(BlockSyntax node)
        {
            // Check if this block is an obfuscated do-while loop BEFORE visiting children
            DoWhileLoopComponents components;
            if (IsObfuscatedDoWhileLoop(node, out components))
            {
                // Push this loop's context onto the stack
                loopContextStack.Push(new LoopContext
                (
                    ContinueLabel: components.ContinueLabel,
                    BreakLabel: components.BreakLabel
                ));

                // Visit the body to handle nested loops and replace goto statements
                StatementSyntax processedBody = (StatementSyntax)Visit(components.Body);

                // Pop this loop's context
                loopContextStack.Pop();

                // Build the do-while statement
                DoStatementSyntax doWhileStatement = SyntaxFactory.DoStatement(
                    statement: processedBody,
                    condition: components.Condition
                );

                return doWhileStatement.WithTriviaFrom(node);
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

        private bool IsObfuscatedDoWhileLoop(BlockSyntax block, out DoWhileLoopComponents components)
        {
            components = null;
            var statements = block.Statements;

            // Quick check: Does the first label contain "dowhile" and "outerStartLabel"?
            if (statements.Count < 5)
                return false;

            if (!(statements[0] is LabeledStatementSyntax firstLabel))
                return false;

            string firstLabelText = firstLabel.Identifier.Text;
            if (!firstLabelText.Contains("dowhile") || !firstLabelText.Contains("outerStartLabel"))
                return false;

            // Trust the structure - extract directly by index
            if (!(statements[2] is BlockSyntax innerBlock))
                return false;

            if (!(statements[statements.Count - 1] is LabeledStatementSyntax outerEndLabel))
                return false;

            string breakLabel = outerEndLabel.Identifier.Text;

            // Parse the inner block
            return ParseInnerBlock(innerBlock, breakLabel, out components);
        }

        private bool ParseInnerBlock(BlockSyntax innerBlock, string breakLabel, out DoWhileLoopComponents components)
        {
            components = null;
            var stmts = innerBlock.Statements;

            // Do-while loop inner block structure:
            // 0: startLabel:
            // 1: body (can be a block or single statement)
            // 2: continueLabel:
            // 3: if (condition) goto startLabel

            if (stmts.Count < 4)
                return false;

            int index = 0;

            // Expect: startLabel with "dowhileloop" and "startLabel" pattern
            if (!(stmts[index] is LabeledStatementSyntax startLabel))
                return false;

            string startLabelName = startLabel.Identifier.Text;
            if (!startLabelName.Contains("dowhileloop") || !startLabelName.Contains("startLabel"))
                return false;

            index++;

            // Extract body (statement at current index)
            if (index >= stmts.Count)
                return false;

            StatementSyntax body = stmts[index];
            index++;

            // Expect: continueLabel
            if (index >= stmts.Count || !(stmts[index] is LabeledStatementSyntax continueLabel))
                return false;

            string continueLabelName = continueLabel.Identifier.Text;
            if (!continueLabelName.Contains("dowhileloop") || !continueLabelName.Contains("continueLabel"))
                return false;

            index++;

            // Expect: if (condition) goto startLabel
            if (index >= stmts.Count || !(stmts[index] is IfStatementSyntax conditionIf))
                return false;

            // Extract the condition (it's NOT negated in do-while, unlike while/for loops)
            ExpressionSyntax condition = conditionIf.Condition;

            // Verify the goto points to the start label
            if (!(conditionIf.Statement is GotoStatementSyntax condGoto))
                return false;

            if (!(condGoto.Expression is IdentifierNameSyntax condGotoId) ||
                condGotoId.Identifier.Text != startLabelName)
                return false;

            // Build components
            components = new DoWhileLoopComponents
            {
                Condition = condition,
                Body = body,
                ContinueLabel = continueLabelName,
                BreakLabel = breakLabel
            };

            return true;
        }

        private class DoWhileLoopComponents
        {
            public ExpressionSyntax Condition { get; set; }
            public StatementSyntax Body { get; set; }
            public string ContinueLabel { get; set; }
            public string BreakLabel { get; set; }
        }
    }
}