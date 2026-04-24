using System.Text;

namespace RecursiveParsing;

public abstract record class TernaryNode(ExpressionNode Left, ExpressionNode Middle, ExpressionNode Right, NodePrecedence Precedence) : ExpressionNode(Left.Span.Start..Right.Span.End, Precedence)
{
    public override void PrintTree(ReadOnlySpan<char> input, int indentation)
    {
        PrintTreeImpl(input, indentation, isTerminal: false);
        Left.PrintTree(input, indentation + 1);
        Middle.PrintTree(input, indentation + 1);
        Right.PrintTree(input, indentation + 1);
    }
}

public sealed record class Conditionnal(ExpressionNode Condition, ExpressionNode True, ExpressionNode False) : TernaryNode(Condition, True, False, NodePrecedence.Conditionnal)
{
    public override RTObject Evaluate(Context ctx)
    => Left.Evaluate(ctx) is bool b
        ? (b ? Middle : Right).Evaluate(ctx)
        : throw new RunTimeException();

    public override void Print(StringBuilder sb)
    {
        if (Left.Precedence <= Precedence)
            sb.Append('(');
        Left.Print(sb);
        if (Left.Precedence <= Precedence)
            sb.Append(')');
        sb.Append(" ? ");
        if (Middle.Precedence < Precedence)
            sb.Append('(');
        Middle.Print(sb);
        if (Middle.Precedence < Precedence)
            sb.Append(')');
        sb.Append(" : ");
        if (Right.Precedence < Precedence)
            sb.Append('(');
        Right.Print(sb);
        if (Right.Precedence < Precedence)
            sb.Append(')');
    }
}
