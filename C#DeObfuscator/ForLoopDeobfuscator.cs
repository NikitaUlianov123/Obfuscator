using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Collections.Generic;

namespace C_DeObfuscator
{
    public class ForLoopDeobfuscator : CSharpSyntaxRewriter
    {
        private Stack<LoopContext> loopContextStack = new Stack<LoopContext>();

        public override SyntaxNode VisitBlock(BlockSyntax node)
        {
            ForLoopComponents components;
            if (IsObfuscatedForLoop(node, out components))
            {
                loopContextStack.Push(new LoopContext
                {
                    ContinueLabel = components.ContinueLabel,
                    BreakLabel = components.BreakLabel
                });

                StatementSyntax processedBody = (StatementSyntax)Visit(components.Body);

                loopContextStack.Pop();

                ForStatementSyntax forStatement = SyntaxFactory.ForStatement(
                    declaration: components.Declaration,
                    initializers: components.Initializers,
                    condition: components.Condition,
                    incrementors: components.Incrementors,
                    statement: processedBody
                );

                return forStatement.WithTriviaFrom(node);
            }

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

        private bool IsObfuscatedForLoop(BlockSyntax block, out ForLoopComponents? components)
        {
            components = null;
            var statements = block.Statements;

            // Quick check: Does the first label contain "forloop" and "outerStartLabel"?
            if (statements.Count < 5)
                return false;

            if (!(statements[0] is LabeledStatementSyntax firstLabel))
                return false;

            string firstLabelText = firstLabel.Identifier.Text;
            if (!firstLabelText.Contains("forLoop") || !firstLabelText.Contains("outerStartLabel"))
                return false;

            if (!(statements[2] is BlockSyntax innerBlock))
                return false;

            if (!(statements[statements.Count - 1] is LabeledStatementSyntax outerEndLabel))
                return false;

            string breakLabel = outerEndLabel.Identifier.Text;

            return ParseInnerBlock(innerBlock, breakLabel, out components);
        }

        private bool ParseInnerBlock(BlockSyntax innerBlock, string breakLabel, out ForLoopComponents? components)
        {
            components = null;
            var stmts = innerBlock.Statements;
            int index = 0;

            // Extract optional variable declaration
            VariableDeclarationSyntax declaration = null;
            if (index < stmts.Count && stmts[index] is LocalDeclarationStatementSyntax declStmt)
            {
                declaration = declStmt.Declaration;
                index++;
            }

            // Extract optional initializers
            List<ExpressionSyntax> initializers = new List<ExpressionSyntax>();
            while (index < stmts.Count && stmts[index] is ExpressionStatementSyntax exprStmt)
            {
                initializers.Add(exprStmt.Expression);
                index++;
            }

            // We should now be at the startLabel
            if (index >= stmts.Count || !(stmts[index] is LabeledStatementSyntax startLabel))
                return false;

            string startLabelName = startLabel.Identifier.Text;
            index++;

            // Extract condition from if statement
            if (index >= stmts.Count || !(stmts[index] is IfStatementSyntax conditionIf))
                return false;

            if (!(conditionIf.Condition is PrefixUnaryExpressionSyntax notExpr) ||
                notExpr.Kind() != SyntaxKind.LogicalNotExpression)
                return false;

            ExpressionSyntax condition = notExpr.Operand;
            if (condition is ParenthesizedExpressionSyntax parenCond)
                condition = parenCond.Expression;

            index++;

            // Collect body statements until we hit the continue label
            List<StatementSyntax> bodyStatements = new List<StatementSyntax>();
            string continueLabel = null;

            while (index < stmts.Count)
            {
                if (stmts[index] is LabeledStatementSyntax label)
                {
                    continueLabel = label.Identifier.Text;
                    index++;
                    break;
                }
                bodyStatements.Add(stmts[index]);
                index++;
            }

            if (continueLabel == null)
                return false;

            // Extract incrementors
            List<ExpressionSyntax> incrementors = new List<ExpressionSyntax>();
            while (index < stmts.Count && stmts[index] is ExpressionStatementSyntax incrStmt)
            {
                incrementors.Add(incrStmt.Expression);
                index++;
            }

            // Build the body
            StatementSyntax body;
            if (bodyStatements.Count == 0)
            {
                body = SyntaxFactory.Block();
            }
            else if (bodyStatements.Count == 1 && bodyStatements[0] is BlockSyntax singleBlock)
            {
                body = singleBlock;
            }
            else if (bodyStatements.Count == 1)
            {
                body = bodyStatements[0];
            }
            else
            {
                body = SyntaxFactory.Block(bodyStatements);
            }

            components = new ForLoopComponents
            {
                Declaration = declaration,
                Initializers = SyntaxFactory.SeparatedList(initializers),
                Condition = condition,
                Incrementors = SyntaxFactory.SeparatedList(incrementors),
                Body = body,
                ContinueLabel = continueLabel,
                BreakLabel = breakLabel
            };

            return true;
        }

        private class ForLoopComponents
        {
            public VariableDeclarationSyntax Declaration { get; set; }
            public SeparatedSyntaxList<ExpressionSyntax> Initializers { get; set; }
            public ExpressionSyntax Condition { get; set; }
            public SeparatedSyntaxList<ExpressionSyntax> Incrementors { get; set; }
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