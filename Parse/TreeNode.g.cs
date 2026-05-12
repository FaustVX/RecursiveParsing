#nullable enable
using System.Collections;
using System.Collections.Immutable;
using RecursiveParsing.Tokenize;

namespace RecursiveParsing.Parse;

public abstract partial record class TreeNode(Range Span)
{
    public abstract void Accept(IVisitor visitor);
    protected static bool StructuralEquals<T>(T lhs, T? rhs)
    where T : struct, IStructuralEquatable
    => rhs?.Equals(lhs, EqualityComparer.Instance) ?? false;
    protected static bool StructuralEquals<T>(T lhs, T? rhs)
    where T : class, IStructuralEquatable
    => rhs?.Equals(lhs, EqualityComparer.Instance) ?? false;
    public bool Equals(object? other, IEqualityComparer comparer)
    => other is TreeNode node && comparer.Equals(this, node);
    public int GetHashCode(IEqualityComparer comparer)
    => comparer.GetHashCode(this);

    public sealed class EqualityComparer : IEqualityComparer
    {
        private EqualityComparer() {}
        public static EqualityComparer Instance { get; set; } = new();

        public new bool Equals(object? x, object? y)
        => (x, y) switch
        {
            (Primary lhs, Primary rhs) => lhs.Equals(rhs),
            (PrefixExpr lhs, PrefixExpr rhs) => lhs.Equals(rhs),
            (PostfixExpr lhs, PostfixExpr rhs) => lhs.Equals(rhs),
            (BinaryExpr lhs, BinaryExpr rhs) => lhs.Equals(rhs),
            (TernaryExpr lhs, TernaryExpr rhs) => lhs.Equals(rhs),
            (CallExpr lhs, CallExpr rhs) => lhs.Equals(rhs),
            (ExpressionStatement lhs, ExpressionStatement rhs) => lhs.Equals(rhs),
            (BlockStatement lhs, BlockStatement rhs) => lhs.Equals(rhs),
            (File lhs, File rhs) => lhs.Equals(rhs),
            _ => false,
        };

        public int GetHashCode(object obj)
        => obj.GetHashCode();
    }
}

/// <summary>
/// <c>expression* : tree-node</c>
/// </summary>
public abstract partial record class Expression(Range Span) : TreeNode(Span);

/// <summary>
/// <c>primary? : expression</c>
/// </summary>
public sealed partial record class Primary(Range Span) : Expression(Span)
{
}

/// <summary>
/// <c>unary-expr* (token-span operator, expression expression) : expression</c>
/// </summary>
public abstract partial record class UnaryExpr(TokenSpan Operator, Expression Expression, Range Span) : Expression(Span);

/// <summary>
/// <c>prefix-expr? (token-span operator, expression expression) : unary-expr (operator, expression)</c>
/// </summary>
public sealed partial record class PrefixExpr(TokenSpan Operator, Expression Expression, Range Span) : UnaryExpr(Operator, Expression, Span)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        Expression.Accept(visitor);
        visitor.Exit(this);
    }

    public bool Equals(PrefixExpr? other)
    => (other?.Operator.Equals(Operator) ?? false) && (other?.Expression.Equals(Expression) ?? false);

    public override int GetHashCode()
    => HashCode.Combine(Operator, Expression);
}

/// <summary>
/// <c>postfix-expr? (expression expression, token-span operator) : unary-expr (operator, expression)</c>
/// </summary>
public sealed partial record class PostfixExpr(Expression Expression, TokenSpan Operator, Range Span) : UnaryExpr(Operator, Expression, Span)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        Expression.Accept(visitor);
        visitor.Exit(this);
    }

    public bool Equals(PostfixExpr? other)
    => (other?.Expression.Equals(Expression) ?? false) && (other?.Operator.Equals(Operator) ?? false);

    public override int GetHashCode()
    => HashCode.Combine(Expression, Operator);
}

/// <summary>
/// <c>binary-expr? (expression left, token-span operator, expression right) : expression</c>
/// </summary>
public sealed partial record class BinaryExpr(Expression Left, TokenSpan Operator, Expression Right, Range Span) : Expression(Span)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        Left.Accept(visitor);
        visitor.Visit(this);
        Right.Accept(visitor);
        visitor.Exit(this);
    }

    public bool Equals(BinaryExpr? other)
    => (other?.Left.Equals(Left) ?? false) && (other?.Operator.Equals(Operator) ?? false) && (other?.Right.Equals(Right) ?? false);

    public override int GetHashCode()
    => HashCode.Combine(Left, Operator, Right);
}

/// <summary>
/// <c>ternary-expr? (expression left, token-span op-left, expression center, token-span op-right, expression right) : expression</c>
/// </summary>
public sealed partial record class TernaryExpr(Expression Left, TokenSpan OpLeft, Expression Center, TokenSpan OpRight, Expression Right, Range Span) : Expression(Span)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        Left.Accept(visitor);
        visitor.Visit(this);
        Center.Accept(visitor);
        visitor.Visit(this);
        Right.Accept(visitor);
        visitor.Exit(this);
    }

    public bool Equals(TernaryExpr? other)
    => (other?.Left.Equals(Left) ?? false) && (other?.OpLeft.Equals(OpLeft) ?? false) && (other?.Center.Equals(Center) ?? false) && (other?.OpRight.Equals(OpRight) ?? false) && (other?.Right.Equals(Right) ?? false);

    public override int GetHashCode()
    => HashCode.Combine(Left, OpLeft, Center, OpRight, Right);
}

/// <summary>
/// <c>call-expr? (expression expression, expression* args) : expression</c>
/// </summary>
public sealed partial record class CallExpr(Expression Expression, System.Collections.Immutable.ImmutableArray<Expression> Args, Range Span) : Expression(Span)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        Expression.Accept(visitor);
        for (var i = 0; i < Args.Length; i++)
        {
            visitor.Visit(this);
            Args[i].Accept(visitor);
        }
        visitor.Exit(this);
    }

    public bool Equals(CallExpr? other)
    => (other?.Expression.Equals(Expression) ?? false) && StructuralEquals(Args, other?.Args);

    public override int GetHashCode()
    => HashCode.Combine(Expression, Args);
}

/// <summary>
/// <c>statement* : tree-node</c>
/// </summary>
public abstract partial record class Statement(Range Span) : TreeNode(Span);

/// <summary>
/// <c>expression-statement? (expression expression) : statement</c>
/// </summary>
public sealed partial record class ExpressionStatement(Expression Expression, Range Span) : Statement(Span)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        Expression.Accept(visitor);
        visitor.Exit(this);
    }

    public bool Equals(ExpressionStatement? other)
    => (other?.Expression.Equals(Expression) ?? false);

    public override int GetHashCode()
    => HashCode.Combine(Expression);
}

/// <summary>
/// <c>block-statement? (statement* statements) : statement</c>
/// </summary>
public sealed partial record class BlockStatement(System.Collections.Immutable.ImmutableArray<Statement> Statements, Range Span) : Statement(Span)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        for (var i = 0; i < Statements.Length; i++)
        {
            if (i > 0)
                visitor.Visit(this);
            Statements[i].Accept(visitor);
        }
        visitor.Exit(this);
    }

    public bool Equals(BlockStatement? other)
    => StructuralEquals(Statements, other?.Statements);

    public override int GetHashCode()
    => HashCode.Combine(Statements);
}

/// <summary>
/// <c>file? (statement* statements) : tree-node</c>
/// </summary>
public sealed partial record class File(System.Collections.Immutable.ImmutableArray<Statement> Statements, Range Span) : TreeNode(Span)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        for (var i = 0; i < Statements.Length; i++)
        {
            if (i > 0)
                visitor.Visit(this);
            Statements[i].Accept(visitor);
        }
        visitor.Exit(this);
    }

    public bool Equals(File? other)
    => StructuralEquals(Statements, other?.Statements);

    public override int GetHashCode()
    => HashCode.Combine(Statements);
}
