using System.Text;
using RecursiveParsing.Phases.Tokenize;

namespace RecursiveParsing.Phases.Parse;

/// <summary>
/// postfix := primary ("?" | "+" | "*")?
/// </summary>
public record class Postfix(Expression Node, TokenSpan Operator, Range Span) : Expression(Span, NodePrecedence.Postfix)
{
    public override void PrintTree(ReadOnlySpan<char> input, int indentation)
    {
        PrintTreeImpl(input, indentation, isTerminal: false);
        Node.PrintTree(input, indentation + 1);
    }

    public override void Print(StringBuilder sb)
    {
        if (Node.Precedence < Precedence)
            sb.Append('(');
        Node.Print(sb);
        if (Node.Precedence < Precedence)
            sb.Append(')');
        sb.Append(Operator.Token.TokenString());
    }
}
