using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

namespace CSharp_DeObfuscator.LoopDeobfuscators
{
    public class WhileLoopDeobfuscator : CSharpSyntaxRewriter
    {
        private Stack<LoopContext> loopContextStack = new Stack<LoopContext>();

        public override SyntaxNode? VisitBlock(BlockSyntax node)
        {
            WhileLoopComponents? components;
            if (TryGetWhileLoopComponents(node, out components) && components != null)
            {
                loopContextStack.Push(new LoopContext
                (
                    ContinueLabel: components.ContinueLabel,
                    BreakLabel: components.BreakLabel
                ));

                StatementSyntax processedBody = (StatementSyntax)Visit(components.Body)!;

                loopContextStack.Pop();

                WhileStatementSyntax whileStatement = SyntaxFactory.WhileStatement(
                    condition: components.Condition,
                    statement: processedBody
                );

                return whileStatement.WithTriviaFrom(node);
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

        private bool TryGetWhileLoopComponents(BlockSyntax block, out WhileLoopComponents? components)
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
            if (!firstLabelText.Contains("whileLoop") || !firstLabelText.Contains("outerStartLabel"))
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

        private WhileLoopComponents ParseInnerBlock(BlockSyntax innerBlock, string breakLabel)
        {
            SyntaxList<StatementSyntax> statements = innerBlock.Statements;

            LabeledStatementSyntax startLabel = (LabeledStatementSyntax)statements[0];
            IfStatementSyntax conditionIf = (IfStatementSyntax)statements[1];
            PrefixUnaryExpressionSyntax notExpr = (PrefixUnaryExpressionSyntax)conditionIf.Condition;
            ExpressionSyntax condition = notExpr.Operand;

            if (condition is ParenthesizedExpressionSyntax parenCond)
            {
                condition = parenCond.Expression;
            }

            StatementSyntax body = statements[2];

            LabeledStatementSyntax continueLabel = (LabeledStatementSyntax)statements[3];
            string continueLabelName = continueLabel.Identifier.Text;

            return new WhileLoopComponents(
                Condition: condition,
                Body: body,
                ContinueLabel: continueLabelName,
                BreakLabel: breakLabel
            );
        }

        private sealed record WhileLoopComponents(
            ExpressionSyntax Condition,
            StatementSyntax Body,
            string ContinueLabel,
            string BreakLabel
        );
    }
}