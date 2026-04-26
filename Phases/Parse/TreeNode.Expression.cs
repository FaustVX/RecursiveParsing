using System.Collections.Immutable;
using System.Text;
using RecursiveParsing.Phases.Tokenize;

namespace RecursiveParsing.Phases.Parse;

public enum NodePrecedence
{
    Expression = 1, // not 0 to avoid implicit conversion from 0
    Choice,
    Sequence,
    Postfix,
    Primary,
}

public abstract record class Expression(Range Span, NodePrecedence Precedence) : TreeNode(Span);

/// <summary>
/// choice := sequence ("|" sequence)*
/// </summary>
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

    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        foreach (var expr in Expressions)
            expr.Accept(visitor);
        visitor.Exit(this);
    }
}

/// <summary>
/// sequence := postfix+
/// </summary>
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

    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        foreach (var expr in Expressions)
            expr.Accept(visitor);
        visitor.Exit(this);
    }
}

/// <summary>
/// postfix := primary ("?" | "+" | "*")?
/// </summary>
public record class Postfix(Expression Node, TokenSpan Operator, Range Span) : Expression(Span, NodePrecedence.Postfix)
{
    public override void Print(StringBuilder sb)
    {
        if (Node.Precedence < Precedence)
            sb.Append('(');
        Node.Print(sb);
        if (Node.Precedence < Precedence)
            sb.Append(')');
        sb.Append(Operator.Token.TokenString());
    }

    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        Node.Accept(visitor);
        visitor.Exit(this);
    }
}

public sealed record class String(string S, Range Span) : Expression(Span, NodePrecedence.Primary)
{
    public override void Print(StringBuilder sb)
    => sb.Append('"').Append(Token.String.Escape(S)).Append('"');

    public override void Accept(IVisitor visitor)
    => visitor.Visit(this);
}

public sealed record class Id(string Name, Range Span) : Expression(Span, NodePrecedence.Primary)
{
    public override void Print(StringBuilder sb)
    => sb.Append(Name);

    public override void Accept(IVisitor visitor)
    => visitor.Visit(this);
}

public sealed record class Terminal(string Name, Range Span) : Expression(Span, NodePrecedence.Primary)
{
    public override void Print(StringBuilder sb)
    => sb.Append(Name);

    public override void Accept(IVisitor visitor)
    => visitor.Visit(this);
}
