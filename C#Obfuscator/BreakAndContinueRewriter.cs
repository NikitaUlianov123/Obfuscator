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
    // Rewriter to replace break and continue statements with goto statements
    internal class BreakAndContinueRewriter : CSharpSyntaxRewriter
    {
        public readonly string ContinueLabel;
        public readonly string EndLabel;
        public BreakAndContinueRewriter(string continueLabel, string endLabel)
        {
            ContinueLabel = continueLabel;
            EndLabel = endLabel;
        }

        // Replaces break statements with goto to the end label
        public override SyntaxNode VisitBreakStatement(BreakStatementSyntax node)
        {
            GotoStatementSyntax gotoEnd = SyntaxFactory.GotoStatement(SyntaxKind.GotoStatement, SyntaxFactory.IdentifierName(EndLabel))
                            .WithLeadingTrivia(node.GetLeadingTrivia())
                            .WithTrailingTrivia(node.GetTrailingTrivia());
            return gotoEnd;
        }

        // Replaces continue statements with goto to the continue label
        public override SyntaxNode? VisitContinueStatement(ContinueStatementSyntax node)
        {
            GotoStatementSyntax gotoContinue = SyntaxFactory.GotoStatement(SyntaxKind.GotoStatement, SyntaxFactory.IdentifierName(ContinueLabel))
                                .WithLeadingTrivia(node.GetLeadingTrivia())
                                .WithTrailingTrivia(node.GetTrailingTrivia());
            return gotoContinue;
        }
    }
}