using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace CSharp_DeObfuscator.LoopDeobfuscators
{
    public class DoWhileLoopDeobfuscator : CSharpSyntaxRewriter
    {
        private Stack<LoopContext> loopContextStack = new Stack<LoopContext>();

        public override SyntaxNode? VisitBlock(BlockSyntax node)
        {
            DoWhileLoopComponents? components;
            if (TryGetDoWhileLoopComponents(node, out components) && components != null)
            {
                loopContextStack.Push(new LoopContext(
                    ContinueLabel: components.ContinueLabel,
                    BreakLabel: components.BreakLabel
                ));

                StatementSyntax processedBody = (StatementSyntax)Visit(components.Body)!;

                loopContextStack.Pop();

                DoStatementSyntax doWhileStatement = SyntaxFactory.DoStatement(
                    statement: processedBody,
                    condition: components.Condition
                );

                return doWhileStatement.WithTriviaFrom(node);
            }

            return base.VisitBlock(node);
        }

        public override SyntaxNode? VisitGotoStatement(GotoStatementSyntax node)
        {
            if (node.Kind() == SyntaxKind.GotoStatement && node.Expression is IdentifierNameSyntax identifier)
            {
                string labelName = identifier.Identifier.Text;

                if (loopContextStack.Count > 0)
                {
                    LoopContext currentContext = loopContextStack.Peek();

                    if (currentContext.ContinueLabel != null && labelName == currentContext.ContinueLabel)
                    {
                        return SyntaxFactory.ContinueStatement().WithTriviaFrom(node);
                    }

                    if (currentContext.BreakLabel != null && labelName == currentContext.BreakLabel)
                    {
                        return SyntaxFactory.BreakStatement().WithTriviaFrom(node);
                    }
                }
            }

            return base.VisitGotoStatement(node);
        }

        private bool TryGetDoWhileLoopComponents(BlockSyntax block, out DoWhileLoopComponents? components)
        {
            components = null;
            SyntaxList<StatementSyntax> statements = block.Statements;

            if (statements.Count < 5)
            {
                return false;
            }

            if (!(statements[0] is LabeledStatementSyntax firstLabel))
            {
                return false;
            }

            string firstLabelText = LabelDecoder.Decrypt(firstLabel.Identifier.Text);
            if (!firstLabelText.Contains("dowhile") || !firstLabelText.Contains("outerStartLabel"))
            {
                return false;
            }

            BlockSyntax? innerBlock = statements[2] as BlockSyntax;
            LabeledStatementSyntax? outerEndLabel = statements[statements.Count - 1] as LabeledStatementSyntax;

            if (innerBlock == null || outerEndLabel == null)
            {
                return false;
            }

            string breakLabel = outerEndLabel.Identifier.Text;

            components = ParseInnerBlock(innerBlock, breakLabel);
            return true;
        }

        private DoWhileLoopComponents ParseInnerBlock(BlockSyntax innerBlock, string breakLabel)
        {
            SyntaxList<StatementSyntax> statements = innerBlock.Statements;

            LabeledStatementSyntax startLabel = (LabeledStatementSyntax)statements[0];
            string startLabelName = startLabel.Identifier.Text;

            StatementSyntax body = statements[1];

            LabeledStatementSyntax continueLabel = (LabeledStatementSyntax)statements[2];
            string continueLabelName = continueLabel.Identifier.Text;

            IfStatementSyntax conditionIf = (IfStatementSyntax)statements[3];
            ExpressionSyntax condition = conditionIf.Condition;

            return new DoWhileLoopComponents(
                Condition: condition,
                Body: body,
                ContinueLabel: continueLabelName,
                BreakLabel: breakLabel
            );
        }

        private sealed record DoWhileLoopComponents(
            ExpressionSyntax Condition,
            StatementSyntax Body,
            string ContinueLabel,
            string BreakLabel
        );
    }
}