using System.Collections.Immutable;
using System.Text;

namespace RecursiveParsing;

public enum NodePrecedence
{
    Expression = 1, // not 0 to avoid implicit conversion from 0
    Choice,
    Sequence,
    Postfix,
    Primary,
}

public abstract record class Expression(Range Span, NodePrecedence Precedence) : TreeNode(Span);

public sealed record class String(string S, Range Span) : Expression(Span, NodePrecedence.Primary)
{
    public override void Print(StringBuilder sb)
    => sb.Append('"').Append(Token.String.Escape(S)).Append('"');

    public override void PrintTree(ReadOnlySpan<char> input, int indentation)
    => PrintTreeImpl(input, indentation, isTerminal: true);
}

public sealed record class Id(string Name, Range Span) : Expression(Span, NodePrecedence.Primary)
{
    public override void Print(StringBuilder sb)
    => sb.Append(Name);

    public override void PrintTree(ReadOnlySpan<char> input, int indentation)
    => PrintTreeImpl(input, indentation, isTerminal: true);
}

public sealed record class Terminal(string Name, Range Span) : Expression(Span, NodePrecedence.Primary)
{
    public override void Print(StringBuilder sb)
    => sb.Append(Name);

    public override void PrintTree(ReadOnlySpan<char> input, int indentation)
    => PrintTreeImpl(input, indentation, isTerminal: true);
}

public sealed record class Choice(ImmutableArray<Expression> Expressions) : Expression(Expressions[0].Span.Start..Expressions[^1].Span.End, NodePrecedence.Choice)
{
    public override void Print(StringBuilder sb)
    {
        for (var i = 0; i < Expressions.Length; i++)
        {
            if (i > 0) sb.Append(" | ");
            if (Expressions[i].Precedence <= Precedence)
                sb.Append('(');
            Expressions[i].Print(sb);
            if (Expressions[i].Precedence <= Precedence)
                sb.Append(')');
        }
    }

    public override void PrintTree(ReadOnlySpan<char> input, int indentation = 0)
    {
        PrintTreeImpl(input, indentation, isTerminal: false);
        foreach (var expr in Expressions)
            expr.PrintTree(input, indentation + 1);
    }
}

public sealed record class Sequence(ImmutableArray<Expression> Expressions) : Expression(Expressions[0].Span.Start..Expressions[^1].Span.End, NodePrecedence.Sequence)
{
    public override void Print(StringBuilder sb)
    {
        for (var i = 0; i < Expressions.Length; i++)
        {
            if (i > 0) sb.Append(' ');
            if (Expressions[i].Precedence <= Precedence)
                sb.Append('(');
            Expressions[i].Print(sb);
            if (Expressions[i].Precedence <= Precedence)
                sb.Append(')');
        }
    }

    public override void PrintTree(ReadOnlySpan<char> input, int indentation = 0)
    {
        PrintTreeImpl(input, indentation, isTerminal: false);
        foreach (var expr in Expressions)
            expr.PrintTree(input, indentation + 1);
    }
}
