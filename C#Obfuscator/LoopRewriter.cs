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
        private SeededLabelGen labelGen = new SeededLabelGen(12345);

        public override SyntaxNode? VisitForStatement(ForStatementSyntax node)
        {
            if (IsInsideFinally(node)) return base.VisitForStatement(node);

            VariableDeclarationSyntax visitedDecl = (VariableDeclarationSyntax)Visit(node.Declaration)!;
            SeparatedSyntaxList<ExpressionSyntax> visitedInitializers = VisitExpressionList(node.Initializers);
            ExpressionSyntax visitedCondition = (ExpressionSyntax)Visit(node.Condition)! ?? LiteralExpression(SyntaxKind.TrueLiteralExpression);
            SeparatedSyntaxList<ExpressionSyntax> visitedIncrementors = VisitExpressionList(node.Incrementors);
            StatementSyntax visitedBody = (StatementSyntax)Visit(node.Statement);

            string startLabel = labelGen.NextLabel();
            string continueLabel = labelGen.NextLabel();
            string outerStart = labelGen.NextLabel();
            string outerEnd = labelGen.NextLabel();

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

            string startLabel = labelGen.NextLabel();
            string continueLabel = labelGen.NextLabel();

            string outerStart = labelGen.NextLabel();
            string outerEnd = labelGen.NextLabel();

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

                GotoStatement(SyntaxKind.GotoStatement, IdentifierName(startLabel)),

                LabeledStatement(outerEnd, EmptyStatement()),
            };

            BlockSyntax innerBlock = Block(stmts);

            BlockSyntax outerBlock = WrapInWhileTrueLoop(innerBlock, outerStart, outerEnd);

            return outerBlock.WithTriviaFrom(node);
        }

        public override SyntaxNode? VisitDoStatement(DoStatementSyntax node)
        {
            if (IsInsideFinally(node)) return base.VisitDoStatement(node);

            ExpressionSyntax visitedCondition = (ExpressionSyntax)(Visit(node.Condition) ?? LiteralExpression(SyntaxKind.TrueLiteralExpression));
            StatementSyntax visitedBody = (StatementSyntax)Visit(node.Statement);

            string startLabel = labelGen.NextLabel();
            string continueLabel = labelGen.NextLabel();

            string outerStart = labelGen.NextLabel();
            string outerEnd = labelGen.NextLabel();

            StatementSyntax bodyWithGotos = (StatementSyntax)new BreakAndContinueRewriter(continueLabel, outerEnd).Visit(visitedBody);

            List<StatementSyntax> stmts = new()
            {
                LabeledStatement(startLabel, EmptyStatement()),

                bodyWithGotos,

                LabeledStatement(continueLabel, EmptyStatement()),

                IfStatement(
                    visitedCondition,
                    GotoStatement(SyntaxKind.GotoStatement, IdentifierName(startLabel))
                ),

                LabeledStatement(outerEnd, EmptyStatement()),
            };

            BlockSyntax innerBlock = Block(stmts);

            BlockSyntax outerBlock = WrapInWhileTrueLoop(innerBlock, outerStart, outerEnd);

            return outerBlock.WithTriviaFrom(node);
        }

        public override SyntaxNode? VisitForEachStatement(ForEachStatementSyntax node)
        {
            if (IsInsideFinally(node)) return base.VisitForEachStatement(node);

            ExpressionSyntax visitedExpr = (ExpressionSyntax)Visit(node.Expression);
            StatementSyntax visitedBody = (StatementSyntax)Visit(node.Statement);

            string startLabel = labelGen.NextLabel();
            string continueLabel = labelGen.NextLabel();
            string endLabel = labelGen.NextLabel();

            StatementSyntax bodyWithGotos =
                (StatementSyntax)new BreakAndContinueRewriter(continueLabel, endLabel).Visit(visitedBody);

            string enumeratorId = labelGen.NextLabel();
            string loopVarName = node.Identifier.Text;

            List<StatementSyntax> stmts = new();


            stmts.Add(LocalDeclarationStatement(
                VariableDeclaration(IdentifierName("var"))
                    .WithVariables(SingletonSeparatedList(
                        VariableDeclarator(Identifier(enumeratorId))
                        .WithInitializer(EqualsValueClause(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    visitedExpr,
                                    IdentifierName("GetEnumerator"))))))))
            );

            stmts.Add(LabeledStatement(startLabel, EmptyStatement()));

            stmts.Add(IfStatement(
                PrefixUnaryExpression(SyntaxKind.LogicalNotExpression,
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(enumeratorId),
                            IdentifierName("MoveNext")))),
                GotoStatement(SyntaxKind.GotoStatement, IdentifierName(endLabel)))
            );

            stmts.Add(LocalDeclarationStatement(
                VariableDeclaration(node.Type)
                    .WithVariables(SingletonSeparatedList(
                        VariableDeclarator(node.Identifier)
                        .WithInitializer(EqualsValueClause(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(enumeratorId),
                                IdentifierName("Current"))))))));

            stmts.Add(bodyWithGotos);

            stmts.Add(LabeledStatement(continueLabel, EmptyStatement()));

            stmts.Add(GotoStatement(SyntaxKind.GotoStatement, IdentifierName(startLabel)));

            stmts.Add(LabeledStatement(endLabel, EmptyStatement()));

            return Block(stmts).WithTriviaFrom(node);
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