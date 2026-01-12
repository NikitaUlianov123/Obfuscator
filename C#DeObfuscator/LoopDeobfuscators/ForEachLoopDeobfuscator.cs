using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace CSharp_DeObfuscator.LoopDeobfuscators
{
    public class ForEachLoopDeobfuscator : CSharpSyntaxRewriter
    {

        // Visits blocks to identify and revert obfuscated foreach loops
        public override SyntaxNode? VisitBlock(BlockSyntax node)
        {
            // Try to extract foreach loop components from the block
            ForEachLoopComponents? components;
            if (TryGetForEachLoopComponents(node, out components) && components != null)
            {
                // Process the body of the loop for possible nested loops
                StatementSyntax processedBody = (StatementSyntax)Visit(components.Body)!;

                // Reconstruct the foreach statement

                ForEachStatementSyntax forEachStatement = SyntaxFactory.ForEachStatement(
                    type: components.LoopVariableType,
                    identifier: components.LoopVariableIdentifier,
                    expression: components.Collection,
                    statement: processedBody
                );

                return forEachStatement.WithTriviaFrom(node);
            }

            return base.VisitBlock(node);
        }


        private bool TryGetForEachLoopComponents(BlockSyntax block, out ForEachLoopComponents? components)
        {
            components = null;
            SyntaxList<StatementSyntax> statements = block.Statements;

            if (statements.Count < 2)
            {
                return false;
            }

            if (!(statements[0] is LocalDeclarationStatementSyntax listDecl))
            {
                return false;
            }

            VariableDeclaratorSyntax? listVar = listDecl.Declaration.Variables.FirstOrDefault();
            if (listVar == null || listVar.Initializer == null)
            {
                return false;
            }

            if (!(listVar.Initializer.Value is InvocationExpressionSyntax toListCall))
            {
                return false;
            }

            if (!(toListCall.Expression is MemberAccessExpressionSyntax memberAccess))
            {
                return false;
            }

            string methodName = memberAccess.Name.Identifier.Text;
            if (methodName != "ToList" && methodName != "ToArray")
            {
                return false;
            }

            string encodedListVarName = listVar.Identifier.Text;

            string decodedListVarName = LabelDecoder.Decrypt(encodedListVarName);
            if (!decodedListVarName.Contains("foreach"))
            {
                return false;
            }

            ExpressionSyntax collection = memberAccess.Expression;

            if (!(statements[1] is ForStatementSyntax forLoop))
            {
                return false;
            }

            components = ParseForLoop(forLoop, encodedListVarName, collection);
            return components != null;
        }


        private ForEachLoopComponents? ParseForLoop(ForStatementSyntax forLoop, string encodedListVarName, ExpressionSyntax collection)
        {
            if (forLoop.Declaration == null || forLoop.Declaration.Variables.Count == 0)
            {
                return null;
            }

            VariableDeclaratorSyntax indexVar = forLoop.Declaration.Variables[0];

            string decodedIndexVarName = LabelDecoder.Decrypt(indexVar.Identifier.Text);
            if (!decodedIndexVarName.Contains("foreach"))
            {
                return null;
            }

            if (!(forLoop.Statement is BlockSyntax body))
            {
                return null;
            }

            if (body.Statements.Count == 0)
            {
                return null;
            }

            if (!(body.Statements[0] is LocalDeclarationStatementSyntax itemDecl))
            {
                return null;
            }

            VariableDeclaratorSyntax? itemVar = itemDecl.Declaration.Variables.FirstOrDefault();
            if (itemVar == null || itemVar.Initializer == null)
            {
                return null;
            }

            if (!(itemVar.Initializer.Value is ElementAccessExpressionSyntax elementAccess))
            {
                return null;
            }

            if (!(elementAccess.Expression is IdentifierNameSyntax accessedList))
            {
                return null;
            }

            if (accessedList.Identifier.Text != encodedListVarName)
            {
                return null;
            }

            TypeSyntax loopVarType = itemDecl.Declaration.Type;
            SyntaxToken loopVarIdentifier = itemVar.Identifier;

            List<StatementSyntax> bodyStatements = body.Statements.Skip(1).ToList();
            StatementSyntax processedBody = BuildBodyStatement(bodyStatements);

            return new ForEachLoopComponents(
                LoopVariableType: loopVarType,
                LoopVariableIdentifier: loopVarIdentifier,
                Collection: collection,
                Body: processedBody
            );
        }

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

        private sealed record ForEachLoopComponents(
            TypeSyntax LoopVariableType,
            SyntaxToken LoopVariableIdentifier,
            ExpressionSyntax Collection,
            StatementSyntax Body
        );
    }
}