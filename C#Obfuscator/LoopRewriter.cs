using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace C_Obfuscation
{
    internal class LoopRewriter : CSharpSyntaxRewriter
    {
        private int nextLabel = 0;
        private SemanticModel model;

        public LoopRewriter(SemanticModel model)
        {
            this.model = model;
        }

        public override SyntaxNode? VisitForStatement(ForStatementSyntax node)
        {
            if (IsInsideFinally(node)) return base.VisitForStatement(node);

            VariableDeclarationSyntax visitedDecl = (VariableDeclarationSyntax)Visit(node.Declaration)!;
            SeparatedSyntaxList<ExpressionSyntax> visitedInitializers = VisitExpressionList(node.Initializers);
            ExpressionSyntax visitedCondition = (ExpressionSyntax)Visit(node.Condition)! ?? LiteralExpression(SyntaxKind.TrueLiteralExpression);
            SeparatedSyntaxList<ExpressionSyntax> visitedIncrementors = VisitExpressionList(node.Incrementors);
            StatementSyntax visitedBody = (StatementSyntax)Visit(node.Statement);

            string startLabel = LabelEncoder.Encrypt("forLoop_startLabel" + nextLabel);
            string continueLabel = LabelEncoder.Encrypt("forLoop_continueLabel" + nextLabel);
            string outerStart = LabelEncoder.Encrypt("forLoop_outerStartLabel" + nextLabel);
            string outerEnd = LabelEncoder.Encrypt("forLoop_outerEndLabel" + nextLabel);

            StatementSyntax bodyWithGotos = (StatementSyntax)new BreakAndContinueRewriter(continueLabel, outerEnd).Visit(visitedBody);

            List<StatementSyntax> stmts = new();

            if (visitedDecl != null)
            {
                stmts.Add(LocalDeclarationStatement(visitedDecl));
            }
            foreach (var init in visitedInitializers)
            {
                stmts.Add(ExpressionStatement(init));
            }

            stmts.Add(LabeledStatement(startLabel, EmptyStatement()));

            stmts.Add(IfStatement(
                PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, ParenthesizedExpression(visitedCondition)),
                GotoStatement(SyntaxKind.GotoStatement, IdentifierName(outerEnd))
            ));

            stmts.Add(bodyWithGotos);

            stmts.Add(LabeledStatement(continueLabel, EmptyStatement()));
            foreach (ExpressionSyntax incr in visitedIncrementors)
            {
                stmts.Add(ExpressionStatement(incr));
            }
            stmts.Add(GotoStatement(SyntaxKind.GotoStatement, IdentifierName(startLabel)));

            BlockSyntax innerBlock = Block(stmts);

            BlockSyntax outerBlock = WrapInWhileTrueLoop(innerBlock, outerStart, outerEnd);

            nextLabel++;
            return outerBlock.WithTriviaFrom(node);
        }

        public override SyntaxNode? VisitWhileStatement(WhileStatementSyntax node)
        {
            if (IsInsideFinally(node))
            {
                return base.VisitWhileStatement(node);
            }

            ExpressionSyntax visitedCondition = (ExpressionSyntax)(Visit(node.Condition) ?? LiteralExpression(SyntaxKind.TrueLiteralExpression));
            StatementSyntax visitedBody = (StatementSyntax)Visit(node.Statement);

            string startLabel = LabelEncoder.Encrypt("whileLoop_startLabel" + nextLabel);
            string continueLabel = LabelEncoder.Encrypt("whileLoop_continueLabel" + nextLabel);
            string outerStart = LabelEncoder.Encrypt("whileLoop_outerStartLabel" + nextLabel);
            string outerEnd = LabelEncoder.Encrypt("whileLoop_outerEndLabel" + nextLabel);

            StatementSyntax bodyWithGotos = (StatementSyntax)new BreakAndContinueRewriter(continueLabel, outerEnd).Visit(visitedBody);

            List<StatementSyntax> stmts = new()
            {
                LabeledStatement(startLabel, EmptyStatement()),
                IfStatement(
                    PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, ParenthesizedExpression(visitedCondition)),
                    GotoStatement(SyntaxKind.GotoStatement, IdentifierName(outerEnd))
                ),
                bodyWithGotos,
                LabeledStatement(continueLabel, EmptyStatement()),
                GotoStatement(SyntaxKind.GotoStatement, IdentifierName(startLabel))
            };

            BlockSyntax innerBlock = Block(stmts);
            BlockSyntax outerBlock = WrapInWhileTrueLoop(innerBlock, outerStart, outerEnd);

            nextLabel++;
            return outerBlock.WithTriviaFrom(node);
        }

        public override SyntaxNode? VisitDoStatement(DoStatementSyntax node)
        {
            if (IsInsideFinally(node)) return base.VisitDoStatement(node);

            ExpressionSyntax visitedCondition = (ExpressionSyntax)(Visit(node.Condition) ?? LiteralExpression(SyntaxKind.TrueLiteralExpression));
            StatementSyntax visitedBody = (StatementSyntax)Visit(node.Statement);

            string startLabel = LabelEncoder.Encrypt("dowhileloop_startLabel" + nextLabel);
            string continueLabel = LabelEncoder.Encrypt("dowhileloop_continueLabel" + nextLabel);
            string outerStart = LabelEncoder.Encrypt("dowhile_outerStartLabel" + nextLabel);
            string outerEnd = LabelEncoder.Encrypt("dowhile_outerEndLabel" + nextLabel);

            StatementSyntax bodyWithGotos = (StatementSyntax)new BreakAndContinueRewriter(continueLabel, outerEnd).Visit(visitedBody);

            List<StatementSyntax> stmts = new()
            {
                LabeledStatement(startLabel, EmptyStatement()),
                bodyWithGotos,
                LabeledStatement(continueLabel, EmptyStatement()),
                IfStatement(
                    visitedCondition,
                    GotoStatement(SyntaxKind.GotoStatement, IdentifierName(startLabel))
                )
            };

            BlockSyntax innerBlock = Block(stmts);
            BlockSyntax outerBlock = WrapInWhileTrueLoop(innerBlock, outerStart, outerEnd);
            nextLabel++;
            return outerBlock.WithTriviaFrom(node);
        }

        public override SyntaxNode? VisitForEachStatement(ForEachStatementSyntax node)
        {
            if (IsInsideFinally(node)) return base.VisitForEachStatement(node);

            ExpressionSyntax visitedExpr = (ExpressionSyntax)Visit(node.Expression);
            StatementSyntax visitedBody = (StatementSyntax)Visit(node.Statement);

            // Generate unique variable names
            string listVarName = LabelEncoder.Encrypt("foreachList" + nextLabel);
            string indexVarName = LabelEncoder.Encrypt("foreachIndex" + nextLabel);

            nextLabel++;

            // Create: var list = collection.ToList();
            var listDeclaration = LocalDeclarationStatement(
                VariableDeclaration(IdentifierName("var"))
                    .WithVariables(SingletonSeparatedList(
                        VariableDeclarator(Identifier(listVarName))
                        .WithInitializer(EqualsValueClause(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    visitedExpr,
                                    IdentifierName("ToList")))))))
            );

            // Create: var item = list[i];
            // Use var instead of trying to get the type from semantic model
            var itemDeclaration = LocalDeclarationStatement(
                VariableDeclaration(IdentifierName("var"))
                    .WithVariables(SingletonSeparatedList(
                        VariableDeclarator(node.Identifier)
                        .WithInitializer(EqualsValueClause(
                            ElementAccessExpression(
                                IdentifierName(listVarName),
                                BracketedArgumentList(
                                    SingletonSeparatedList(
                                        Argument(IdentifierName(indexVarName)))))))))
            );

            // Wrap body with item declaration
            StatementSyntax newBody;
            if (visitedBody is BlockSyntax blockBody)
            {
                newBody = blockBody.WithStatements(
                    blockBody.Statements.Insert(0, itemDeclaration));
            }
            else
            {
                newBody = Block(itemDeclaration, visitedBody);
            }

            // Create the for loop: for (int i = 0; i < list.Count; i++)
            var forLoop = ForStatement(newBody)
                .WithDeclaration(
                    VariableDeclaration(
                        PredefinedType(Token(SyntaxKind.IntKeyword)))
                    .WithVariables(SingletonSeparatedList(
                        VariableDeclarator(Identifier(indexVarName))
                        .WithInitializer(EqualsValueClause(
                            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)))))))
                .WithCondition(
                    BinaryExpression(
                        SyntaxKind.LessThanExpression,
                        IdentifierName(indexVarName),
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(listVarName),
                            IdentifierName("Count"))))
                .WithIncrementors(
                    SingletonSeparatedList<ExpressionSyntax>(
                        PostfixUnaryExpression(
                            SyntaxKind.PostIncrementExpression,
                            IdentifierName(indexVarName))));

            // Wrap in a block with the list declaration
            var result = Block(
                listDeclaration,
                forLoop
            ).WithTriviaFrom(node);

            // Visit the result to apply for loop obfuscation
            return Visit(result);
        }

        private BlockSyntax WrapInWhileTrueLoop(BlockSyntax block, string outerLoopStartLabel, string outerLoopEndLabel)
        {
            List<StatementSyntax> outer = new()
            {
                LabeledStatement(outerLoopStartLabel, EmptyStatement()),
                IfStatement(
                    PrefixUnaryExpression(SyntaxKind.LogicalNotExpression,
                        LiteralExpression(SyntaxKind.TrueLiteralExpression)),
                    GotoStatement(SyntaxKind.GotoStatement, IdentifierName(outerLoopStartLabel))
                ),
                block,
                GotoStatement(SyntaxKind.GotoStatement, IdentifierName(outerLoopStartLabel)),
                LabeledStatement(outerLoopEndLabel, EmptyStatement())
            };

            return Block(outer);
        }

        private static bool IsInsideFinally(SyntaxNode node)
        {
            return node.Ancestors().OfType<FinallyClauseSyntax>().Any();
        }

        private SeparatedSyntaxList<ExpressionSyntax> VisitExpressionList(SeparatedSyntaxList<ExpressionSyntax> list)
        {
            var builder = new SeparatedSyntaxList<ExpressionSyntax>();
            foreach (ExpressionSyntax e in list)
            {
                builder = builder.Add((ExpressionSyntax)Visit(e));
            }
            return builder;
        }
    }
}