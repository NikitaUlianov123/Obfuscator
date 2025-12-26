using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace C_DeObfuscator
{
    public class ForEachLoopDeobfuscator : CSharpSyntaxRewriter
    {
        private Stack<LoopContext> loopContextStack = new Stack<LoopContext>();

        public override SyntaxNode VisitBlock(BlockSyntax node)
        {
            ForEachLoopComponents? components;
            if (IsObfuscatedForEachLoop(node, out components) && components != null)
            {
                loopContextStack.Push(new LoopContext
                (
                    ContinueLabel: components.ContinueLabel,
                    BreakLabel: components.BreakLabel
                ));

                StatementSyntax processedBody = (StatementSyntax)Visit(components.Body);

                loopContextStack.Pop();

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

        private bool IsObfuscatedForEachLoop(BlockSyntax block, out ForEachLoopComponents? components)
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

            string listVarName = listVar.Identifier.Text;

            if (!listVarName.Contains("foreach"))
            {
                return false;
            }

            ExpressionSyntax collection = memberAccess.Expression;

            if (!(statements[1] is ForStatementSyntax forLoop))
            {
                return false;
            }

            return ParseForLoop(forLoop, listVarName, collection, out components);
        }

        private bool ParseForLoop(ForStatementSyntax forLoop, string listVarName, ExpressionSyntax collection, out ForEachLoopComponents? components)
        {
            components = null;

            if (forLoop.Declaration == null || forLoop.Declaration.Variables.Count == 0)
            {
                return false;
            }

            VariableDeclaratorSyntax? indexVar = forLoop.Declaration.Variables[0];
            string indexVarName = indexVar.Identifier.Text;

            if (!indexVarName.Contains("foreach"))
            {
                return false;
            }

            if (!(forLoop.Statement is BlockSyntax body))
            {
                return false;
            }

            if (body.Statements.Count == 0)
            {
                return false;
            }

            if (!(body.Statements[0] is LocalDeclarationStatementSyntax itemDecl))
            {
                return false;
            }

            VariableDeclaratorSyntax? itemVar = itemDecl.Declaration.Variables.FirstOrDefault();
            if (itemVar == null || itemVar.Initializer == null)
            {
                return false;
            }

            if (!(itemVar.Initializer.Value is ElementAccessExpressionSyntax elementAccess))
            {
                return false;
            }

            if (!(elementAccess.Expression is IdentifierNameSyntax accessedList))
            {
                return false;
            }

            if (accessedList.Identifier.Text != listVarName)
            {
                return false;
            }

            TypeSyntax loopVarType = itemDecl.Declaration.Type;
            SyntaxToken loopVarIdentifier = itemVar.Identifier;

            List<StatementSyntax> bodyStatements = body.Statements.Skip(1).ToList();

            StatementSyntax processedBody;
            if (bodyStatements.Count == 0)
            {
                processedBody = SyntaxFactory.Block();
            }
            else if (bodyStatements.Count == 1 && bodyStatements[0] is BlockSyntax singleBlock)
            {
                processedBody = singleBlock;
            }
            else if (bodyStatements.Count == 1)
            {
                processedBody = bodyStatements[0];
            }
            else
            {
                processedBody = SyntaxFactory.Block(bodyStatements);
            }

            components = new ForEachLoopComponents
            {
                LoopVariableType = loopVarType,
                LoopVariableIdentifier = loopVarIdentifier,
                Collection = collection,
                Body = processedBody,
                ContinueLabel = null,  // For loop handles this
                BreakLabel = null      // For loop handles this
            };

            return true;
        }

        private class ForEachLoopComponents
        {
            public TypeSyntax LoopVariableType { get; set; }
            public SyntaxToken LoopVariableIdentifier { get; set; }
            public ExpressionSyntax Collection { get; set; }
            public StatementSyntax Body { get; set; }
            public string ContinueLabel { get; set; }
            public string BreakLabel { get; set; }
        }
    }
}