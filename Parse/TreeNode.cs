using System.Diagnostics;
using RecursiveParsing.Tokenize;

namespace RecursiveParsing.Parse;

public enum ExpressionType
{
    None = -1,
    Unknown = 0,
    Id,
    String,
    Int,
    Bool,
}

public enum ExpressionPrecedence
{
    Expression = 1, // not 0 to avoid implicit conversion from 0
    Conditionnal,
    Equation,
    Relational,
    Additive,
    Term,
    Unary,
    Exponentiation,
    Postfix,
    Primary,
}

partial record class Expression
{
    public required ExpressionPrecedence Precedence { get; init; }
    public ExpressionType Type { get; set; }
}

sealed partial record class Primary
{
    public required TokenSpan TokenSpan { get; init; }

    public string Name => TokenSpan.Token switch
    {
        Token.Id { Value: string v} => v,
        Token.String { Value: string v} => v,
        _ => throw new UnreachableException(),
    };
    public override void Accept(IVisitor visitor)
    => visitor.Visit(this);

    public bool Equals(Primary? other)
    => other?.TokenSpan.Token.Equals(TokenSpan.Token) ?? false;

    public override int GetHashCode()
    => HashCode.Combine(TokenSpan.Token);
}
