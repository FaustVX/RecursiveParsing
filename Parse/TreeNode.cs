using System.Collections;
using System.Collections.Immutable;
using RecursiveParsing.Tokenize;

namespace RecursiveParsing.Parse;

public enum ExpressionType
{
    None = -1,
    Unknown = 0,
    String,
    Int,
    Bool,
    Function,
}

public readonly record struct FunctionSignature(ExpressionTypeUnion Return, ImmutableArray<ExpressionTypeUnion> Args)
{
    public override string ToString()
    => $"({string.Join(", ", Args)}): {Return}";

    public bool Equals(FunctionSignature other)
    => Return == other.Return && ((IStructuralEquatable)Args).Equals(other.Args, Equatable.Instance);

    public override int GetHashCode()
    => HashCode.Combine(Return, Args);

    public bool IsCompatibleWith(ImmutableArray<Expression> args)
    => args.Length == Args.Length && Args.Zip(args.Select(a => a.Type)).All(a => a.First == a.Second);

    public static ImmutableArray<FunctionSignature> CommonSignature(ImmutableArray<FunctionSignature> lhs, ImmutableArray<FunctionSignature> rhs)
    {
        return [..Impl(lhs, rhs)];

        static IEnumerable<FunctionSignature> Impl(ImmutableArray<FunctionSignature> lhs, ImmutableArray<FunctionSignature> rhs)
        {
            foreach (var sig in lhs)
                if (rhs.Contains(sig))
                    yield return sig;
        }
    }

    public sealed class Equatable : IEqualityComparer
    {
        private Equatable() {}
        public static Equatable Instance { get; } = new();
        bool IEqualityComparer.Equals(object? x, object? y)
        => (x, y) switch
        {
            (ExpressionTypeUnion lhs, ExpressionTypeUnion rhs) => lhs.Equals(rhs),
            (ExpressionType lhs, ExpressionType rhs) => lhs.Equals(rhs),
            (FunctionSignature lhs, FunctionSignature rhs) => lhs.Equals(rhs),
            (null, null) => true,
            _ => false,
        };

        public int GetHashCode(object obj)
        => obj is FunctionSignature f ? f.GetHashCode() : 0;
    }
}

public readonly union ExpressionTypeUnion(ExpressionType, ImmutableArray<FunctionSignature>)
{
    public static bool operator !=(ExpressionTypeUnion lhs, ExpressionTypeUnion rhs)
    => !lhs.Equals(rhs);
    public static bool operator ==(ExpressionTypeUnion lhs, ExpressionTypeUnion rhs)
    => lhs.Equals(rhs);
    public override bool Equals(object? obj)
    => obj is ExpressionTypeUnion other && Equals(other);
    public bool Equals(ExpressionTypeUnion other)
    => (this, other) switch
    {
        (ExpressionType l, ExpressionType r) => l == r,
        (ImmutableArray<FunctionSignature> l, ImmutableArray<FunctionSignature> r) => ((IStructuralEquatable)l).Equals(r, FunctionSignature.Equatable.Instance),
        (null, null) => true,
        _ => false,
    };
    public override int GetHashCode()
    => Value?.GetHashCode() ?? 0;

    public override string ToString()
    => this switch
    {
        ExpressionType t => t.ToString(),
        ImmutableArray<FunctionSignature> sigs => string.Join(" or ", sigs),
        null => "",
    };
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
    public ExpressionTypeUnion Type { get; set; }
}

partial record class BinaryExpr
{
    public ImmutableArray<FunctionSignature> Signatures { get; set; } = [];
}

sealed partial record class Primary
{
    public required TokenSpan TokenSpan { get; init; }

    public override void Accept(IVisitor visitor)
    => visitor.Visit(this);

    public bool Equals(Primary? other)
    => other?.TokenSpan.Token.Equals(TokenSpan.Token) ?? false;

    public override int GetHashCode()
    => HashCode.Combine(TokenSpan.Token);
}
