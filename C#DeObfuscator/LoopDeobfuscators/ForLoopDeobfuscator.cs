using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Collections.Generic;

namespace CSharp_DeObfuscator.LoopDeobfuscators
{
    public class ForLoopDeobfuscator : CSharpSyntaxRewriter
    {


        //Stack to keep track of nested loops
        private Stack<LoopContext> loopContextStack = new Stack<LoopContext>();

        //Visits blocks to identify and revert obfuscated for loops
        public override SyntaxNode? VisitBlock(BlockSyntax node)
        {
            // Try to extract for loop components from the block
            ForLoopComponents? components;
            if (TryGetForLoopComponents(node, out components) && components != null)
            {

                //Push current loop context onto the stack

                loopContextStack.Push(new LoopContext
                (
                    ContinueLabel: components.ContinueLabel,
                    BreakLabel: components.BreakLabel
                ));


                // Process the body of the loop for possible nested loops

                StatementSyntax processedBody = (StatementSyntax)Visit(components.Body)!;

                loopContextStack.Pop();


                // Reconstruct the for statement

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


        // Visits goto statements to replace them with break/continue where applicable

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


        // Checks the block for the structure of an obfuscated for loop and then extracts its components

        private bool TryGetForLoopComponents(BlockSyntax block, out ForLoopComponents? components)
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
            if (!firstLabelText.Contains("forLoop") || !firstLabelText.Contains("outerStartLabel"))
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


        // Parses the inner block(the actual for loop, not including the while-true loop wrapper) to extract for loop components

        private ForLoopComponents ParseInnerBlock(BlockSyntax innerBlock, string breakLabel)
        {
            SyntaxList<StatementSyntax> statements = innerBlock.Statements;
            int index = 0;

            // Extract variable declaration
            VariableDeclarationSyntax? declaration = null;
            if (index < statements.Count && statements[index] is LocalDeclarationStatementSyntax declStmt)
            {
                declaration = declStmt.Declaration;
                index++;
            }

            // Extract initializers
            List<ExpressionSyntax> initializers = new List<ExpressionSyntax>();
            while (index < statements.Count && statements[index] is ExpressionStatementSyntax exprStmt)
            {
                initializers.Add(exprStmt.Expression);
                index++;
            }

            // Extract start label
            LabeledStatementSyntax startLabel = (LabeledStatementSyntax)statements[index];
            index++;

            // Extract condition
            IfStatementSyntax conditionIf = (IfStatementSyntax)statements[index];
            PrefixUnaryExpressionSyntax notExpr = (PrefixUnaryExpressionSyntax)conditionIf.Condition;
            ExpressionSyntax condition = notExpr.Operand;

            if (condition is ParenthesizedExpressionSyntax parenCond)
            {
                condition = parenCond.Expression;
            }

            index++;

            // Collect body statements until continue label
            List<StatementSyntax> bodyStatements = new List<StatementSyntax>();
            string? continueLabel = null;

            while (index < statements.Count)
            {
                if (statements[index] is LabeledStatementSyntax label)
                {
                    continueLabel = label.Identifier.Text;
                    index++;
                    break;
                }
                bodyStatements.Add(statements[index]);
                index++;
            }

            // Extract incrementors
            List<ExpressionSyntax> incrementors = new List<ExpressionSyntax>();
            while (index < statements.Count && statements[index] is ExpressionStatementSyntax incrStmt)
            {
                incrementors.Add(incrStmt.Expression);
                index++;
            }

            // Build the body statement
            StatementSyntax body = BuildBodyStatement(bodyStatements);

            return new ForLoopComponents(
                Declaration: declaration,
                Initializers: SyntaxFactory.SeparatedList(initializers),
                Condition: condition,
                Incrementors: SyntaxFactory.SeparatedList(incrementors),
                Body: body,
                ContinueLabel: continueLabel ?? string.Empty,
                BreakLabel: breakLabel
            );
        }


        // Constructs the body statement for the for loop

        private StatementSyntax BuildBodyStatement(List<StatementSyntax> bodyStatements)
        {
            if (bodyStatements.Count == 0)
            {
                return SyntaxFactory.Block();
            }
            else if (bodyStatements.Count == 1 && bodyStatements[0] is BlockSyntax singleBlock)
            {
                return singleBlock;
            }
            else if (bodyStatements.Count == 1)
            {
                return bodyStatements[0];
            }
            else
            {
                return SyntaxFactory.Block(bodyStatements);
            }
        }

        private sealed record ForLoopComponents(
            VariableDeclarationSyntax? Declaration,
            SeparatedSyntaxList<ExpressionSyntax> Initializers,
            ExpressionSyntax Condition,
            SeparatedSyntaxList<ExpressionSyntax> Incrementors,
            StatementSyntax Body,
            string ContinueLabel,
            string BreakLabel
        );
    }
}