using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Collections.Generic;

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

    private bool IsObfuscatedForLoop(BlockSyntax block, out ForLoopComponents components)
    {
        components = null;
        var statements = block.Statements;

        if (statements.Count < 5)
        {
            return false;
        }

        if (statements[0] is not LabeledStatementSyntax outerStartLabel)
        {
            return false;
        }

        if (statements[1] is not IfStatementSyntax outerIf)
        {
            return false;
        }

        if (outerIf.Condition is not PrefixUnaryExpressionSyntax outerNot ||
            outerNot.Kind() != SyntaxKind.LogicalNotExpression ||
            outerNot.Operand is not LiteralExpressionSyntax outerLit ||
            outerLit.Kind() != SyntaxKind.TrueLiteralExpression)
        {
            return false;
        }

        if (statements[2] is not BlockSyntax innerBlock)
        {
            return false;
        }

        if (statements[3] is not GotoStatementSyntax)
        {
            return false;
        }

        if (statements[4] is not LabeledStatementSyntax outerEndLabel)
        {
            return false;
        }

        string breakLabel = outerEndLabel.Identifier.Text;

        return ParseInnerBlock(innerBlock, breakLabel, out components);
    }

    private bool ParseInnerBlock(BlockSyntax innerBlock, string breakLabel, out ForLoopComponents components)
    {
        components = null;
        var stmts = innerBlock.Statements;
        int index = 0;

        VariableDeclarationSyntax declaration = null;
        if (index < stmts.Count && stmts[index] is LocalDeclarationStatementSyntax declStmt)
        {
            declaration = declStmt.Declaration;
            index++;
        }

        List<ExpressionSyntax> initializers = new List<ExpressionSyntax>();
        while (index < stmts.Count && stmts[index] is ExpressionStatementSyntax exprStmt)
        {
            initializers.Add(exprStmt.Expression);
            index++;
        }

        if (index >= stmts.Count || stmts[index] is not LabeledStatementSyntax startLabel)
        {
            return false;
        }

        string startLabelName = startLabel.Identifier.Text;
        index++;
        
        if (index >= stmts.Count || stmts[index] is not IfStatementSyntax conditionIf)
        {
            return false;
        }

        if (conditionIf.Condition is not PrefixUnaryExpressionSyntax notExpr || notExpr.Kind() != SyntaxKind.LogicalNotExpression)
        {
            return false;
        }

        ExpressionSyntax condition = notExpr.Operand;

        if (condition is ParenthesizedExpressionSyntax parenCond)
        {
            condition = parenCond.Expression;
        }

        if (conditionIf.Statement is not GotoStatementSyntax condGoto)
        {
            return false;
        }

        index++;

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
        {
            return false;
        }

        List<ExpressionSyntax> incrementors = new List<ExpressionSyntax>();
        while (index < stmts.Count && stmts[index] is ExpressionStatementSyntax incrStmt)
        {
            incrementors.Add(incrStmt.Expression);
            index++;
        }

        if (index >= stmts.Count || stmts[index] is not GotoStatementSyntax finalGoto)
        {
            return false;
        }

        if (finalGoto.Expression is not IdentifierNameSyntax finalGotoId || finalGotoId.Identifier.Text != startLabelName)
        {
            return false;
        }

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