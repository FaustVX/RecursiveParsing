using System.Text;

namespace RecursiveParsing;

public abstract record class UnaryNode(Expression Node, Range Span, NodePrecedence Precedence) : Expression(Span, Precedence)
{
    public override void PrintTree(ReadOnlySpan<char> input, int indentation)
    {
        PrintTreeImpl(input, indentation, isTerminal: false);
        Node.PrintTree(input, indentation + 1);
    }
}

public sealed record class Optional(Expression Node, Range Span) : UnaryNode(Node, Span, NodePrecedence.Postfix)
{
    public override void Print(StringBuilder sb)
    {
        if (Node.Precedence < Precedence)
            sb.Append('(');
        Node.Print(sb);
        if (Node.Precedence < Precedence)
            sb.Append(')');
        sb.Append('?');
    }
}

public sealed record class Multiple(Expression Node, Range Span) : UnaryNode(Node, Span, NodePrecedence.Postfix)
{
    public override void Print(StringBuilder sb)
    {
        if (Node.Precedence < Precedence)
            sb.Append('(');
        Node.Print(sb);
        if (Node.Precedence < Precedence)
            sb.Append(')');
        sb.Append('+');
    }
}

public sealed record class Any(Expression Node, Range Span) : UnaryNode(Node, Span, NodePrecedence.Postfix)
{
    public override void Print(StringBuilder sb)
    {
        if (Node.Precedence < Precedence)
            sb.Append('(');
        Node.Print(sb);
        if (Node.Precedence < Precedence)
            sb.Append(')');
        sb.Append('*');
    }
}
