using System.Collections.Immutable;
using EBNFParser.Phases.Tokenize;

namespace EBNFParser.Phases.Parse;

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
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        for (int i = 0; i < Expressions.Length; i++)
        {
            if (i > 0)
                visitor.Visit(this);
            Expressions[i].Accept(visitor);
        }

        visitor.Exit(this);
    }
}

/// <summary>
/// sequence := postfix+
/// </summary>
public sealed record class Sequence(ImmutableArray<Expression> Expressions) : Expression(Expressions[0].Span.Start..Expressions[^1].Span.End, NodePrecedence.Sequence)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        for (int i = 0; i < Expressions.Length; i++)
        {
            if (i > 0)
                visitor.Visit(this);
            Expressions[i].Accept(visitor);
        }

        visitor.Exit(this);
    }
}

/// <summary>
/// postfix := primary ("?" | "+" | "*")?
/// </summary>
public record class Postfix(Expression Node, TokenSpan Operator, Range Span) : Expression(Span, NodePrecedence.Postfix)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        Node.Accept(visitor);
        visitor.Exit(this);
    }
}

public sealed record class String(string S, Range Span) : Expression(Span, NodePrecedence.Primary)
{
    public override void Accept(IVisitor visitor)
    => visitor.Visit(this);
}

public sealed record class Id(string Name, Range Span) : Expression(Span, NodePrecedence.Primary)
{
    public override void Accept(IVisitor visitor)
    => visitor.Visit(this);
}

public sealed record class Terminal(string Name, Range Span) : Expression(Span, NodePrecedence.Primary)
{
    public override void Accept(IVisitor visitor)
    => visitor.Visit(this);
}
