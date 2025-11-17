using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_Obfuscation
{
    internal class BreakAndContinueRewriter : CSharpSyntaxRewriter
    {
        public readonly string ContinueLabel;
        public readonly string EndLabel;
        public BreakAndContinueRewriter(string continueLabel, string endLabel)
        {
            ContinueLabel = continueLabel;
            EndLabel = endLabel;
        }

        public override SyntaxNode VisitBreakStatement(BreakStatementSyntax node)
        {
            var gotoEnd = SyntaxFactory.GotoStatement(SyntaxKind.GotoStatement, SyntaxFactory.IdentifierName(EndLabel))
                            .WithLeadingTrivia(node.GetLeadingTrivia())
                            .WithTrailingTrivia(node.GetTrailingTrivia());
            return gotoEnd;
        }

        public override SyntaxNode? VisitContinueStatement(ContinueStatementSyntax node)
        {
            var gotoContinue = SyntaxFactory.GotoStatement(SyntaxKind.GotoStatement, SyntaxFactory.IdentifierName(ContinueLabel))
                                .WithLeadingTrivia(node.GetLeadingTrivia())
                                .WithTrailingTrivia(node.GetTrailingTrivia());
            return gotoContinue;
        }
    }
}