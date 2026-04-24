using System.Text;

namespace RecursiveParsing;

public abstract record class StatementNode(Range Span) : TreeNode(Span);

public record class ExpressionStatement(ExpressionNode Expression, Range Span) : StatementNode(Span)
{
    public override void Print(StringBuilder sb)
    {
        Expression.Print(sb);
        sb.Append(';');
    }

    public override void PrintTree(ReadOnlySpan<char> input, int indentation)
    {
        PrintTreeImpl(input, indentation, isTerminal: false);
        Expression.PrintTree(input, indentation + 1);
    }
}
