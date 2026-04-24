using System.Collections.Immutable;
using System.Text;

namespace RecursiveParsing;

public abstract record class StatementNode(Range Span) : TreeNode(Span)
{
    public sealed override void Print(StringBuilder sb)
    => Print(sb, 0);

    public abstract void Print(StringBuilder sb, int indentation);
}

public record class ExpressionStatement(ExpressionNode Expression, Range Span) : StatementNode(Span)
{
    public override void Print(StringBuilder sb, int indentation)
    {
        sb.Append(IndentSpaces(indentation));
        Expression.Print(sb);
        sb.Append(';');
    }

    public override void PrintTree(ReadOnlySpan<char> input, int indentation)
    {
        PrintTreeImpl(input, indentation, isTerminal: false);
        Expression.PrintTree(input, indentation + 1);
    }
}

public record class BlockStatement(ImmutableArray<StatementNode> Statements, Range Span) : StatementNode(Span)
{
    public override void Print(StringBuilder sb, int indentation)
    {
        sb.Append(IndentSpaces(indentation));
        sb.AppendLine("{");
        for (var i = 0; i < Statements.Length; i++)
        {
            Statements[i].Print(sb, indentation + 1);
            sb.AppendLine();
        }
        sb.Append(IndentSpaces(indentation));
        sb.AppendLine("}");
    }

    public override void PrintTree(ReadOnlySpan<char> input, int indentation)
    {
        PrintTreeImpl(input, indentation, isTerminal: false);
        foreach (var statement in Statements)
            statement.PrintTree(input, indentation + 1);
    }
}
