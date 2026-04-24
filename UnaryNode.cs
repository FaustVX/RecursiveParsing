using System.Text;

namespace RecursiveParsing;

public abstract record class UnaryNode(ExpressionNode Node, Range Span, NodePrecedence Precedence) : ExpressionNode(Span, Precedence)
{
    public override void PrintTree(ReadOnlySpan<char> input, int indentation)
    {
        PrintTreeImpl(input, indentation, isTerminal: false);
        Node.PrintTree(input, indentation + 1);
    }
}

public sealed record class Negate(ExpressionNode Node, Range Span) : UnaryNode(Node, Span, NodePrecedence.Unary)
{
    public override RTObject Evaluate(Context ctx)
    => Node.Evaluate(ctx) is decimal d ? -d : throw new RunTimeException();

    public override void Print(StringBuilder sb)
    {
        sb.Append('-');
        if (Node.Precedence < Precedence)
            sb.Append('(');
        Node.Print(sb);
        if (Node.Precedence < Precedence)
            sb.Append(')');
    }
}

public sealed record class Factorial(ExpressionNode Node, Range Span) : UnaryNode(Node, Span, NodePrecedence.Postfix)
{
    public override RTObject Evaluate(Context ctx)
    => F(Node.Evaluate(ctx) is decimal d ? d : throw new RunTimeException());

    static decimal F(decimal a)
    => a switch
    {
        <= 1 => 1,
        _ => a * F(a - 1),
    };

    public override void Print(StringBuilder sb)
    {
        if (Node.Precedence < Precedence)
            sb.Append('(');
        Node.Print(sb);
        if (Node.Precedence < Precedence)
            sb.Append(')');
        sb.Append('!');
    }
}
