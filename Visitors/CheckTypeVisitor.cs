using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using RecursiveParsing.Parse;
using RecursiveParsing.Tokenize;

namespace RecursiveParsing.Visitors;

[Serializable]
public abstract class CheckTypeVisitorException : EBNFException;

file sealed class InvalidOperationException(TokenSpan span) : CheckTypeVisitorException
{
    public TokenSpan Span { get; } = span;

    public override Range Range => Span.Span;

    public override string ErrorCode => "EB_0005";

    public override string SubCategory => "Invalid Operation";

    public override string Message => $"Invalid operation given types";
}

file sealed class InvalidOperandTypeException(TokenSpan span, Expression expression, ExpressionTypeUnion expected) : CheckTypeVisitorException
{
    public TokenSpan Span { get; } = span;
    public Expression Expression { get; } = expression;
    public ExpressionTypeUnion Expected { get; } = expected;

    public override Range Range => Span.Span;

    public override string ErrorCode => "EB_0006";

    public override string SubCategory => "Invalid Operand Type";

    public override string Message => $"Invalid operand type, should be {Expected}, but got {Expression.Type}";
}

file sealed class InvalidExpressionType(Expression expression, ExpressionTypeUnion expected) : CheckTypeVisitorException
{
    public Expression Expression { get; } = expression;
    public ExpressionTypeUnion Expected { get; } = expected;

    public override Range Range => Expression.Span;

    public override string ErrorCode => "EB_0007";

    public override string SubCategory => "Invalid Expression Type";

    public override string Message => $"Invalid expression type, should be {Expected}, but got {Expression.Type}";
}

file sealed class InvalidCallException(Call callExpr) : CheckTypeVisitorException
{
    public Call CallExpr { get; } = callExpr;

    public override Range Range => CallExpr.Span;

    public override string ErrorCode => "EB_0008";

    public override string SubCategory => "Invalid Call";

    public override string Message => "Invalid Call";
}

file sealed class InexistantFunctionException(Call call) : CheckTypeVisitorException
{
    public Call Call { get; } = call;

    public override Range Range => Call.Span;

    public override string ErrorCode => "EB_0009";

    public override string SubCategory => "Inexistant Function Name";

    public override string Message => $"Inexistant function name, got {Call.Name}";
}

file sealed class InvalidArgsCountException(Call call, ImmutableHashSet<int> counts) : CheckTypeVisitorException
{
    public Call Call { get; } = call;
    public ImmutableHashSet<int> Counts { get; } = counts;

    public override Range Range => Call.Span;

    public override string ErrorCode => "EB_0010";

    public override string SubCategory => "Invalid Args Count";

    public override string Message => $"Invalid args count, should be any of [{string.Join(", ", Counts)}], but got {Call.ArgsLength}";
}

file sealed class InvalidArgumentTypeException(Call call, ImmutableHashSet<ImmutableArray<ExpressionTypeUnion>> expected) : CheckTypeVisitorException
{
    public Call Call { get; } = call;
    public ImmutableHashSet<ImmutableArray<ExpressionTypeUnion>> Expected { get; } = expected;

    public override Range Range => Call.Span;

    public override string ErrorCode => "EB_0011";

    public override string SubCategory => "Invalid Argument Type";

    public override string Message => $"Invalid argument type, should be {string.Join(" or ", Expected.Select(s => $"({string.Join(", ", s)})"))}, but got ({string.Join(", ", Call.Args.Select(a => a.Type))})";
}

readonly union Call(CallExpr, BinaryExpr, PrefixExpr)
{
    public readonly Range Span => ((Expression)Value!).Span;

    public readonly string Name => this switch
    {
        CallExpr { Expression: Primary { TokenSpan.Token: Token.Id { Value: var name } } } => name,
        UnaryExpr { Operator.Token: Token.Symbol { Value: var op } } => op,
        BinaryExpr { Operator.Token: Token.Symbol { Value: var op } } => op,
        CallExpr or UnaryExpr or BinaryExpr or null => throw new UnreachableException(),
    };

    public readonly IEnumerable<Expression> Args => this switch
    {
        CallExpr c => c.Args,
        UnaryExpr u => [u.Expression],
        BinaryExpr b => [b.Left, b.Right],
        null => throw new UnreachableException(),
    };

    public readonly int ArgsLength => this switch
    {
        CallExpr c => c.Args.Length,
        UnaryExpr => 1,
        BinaryExpr => 2,
        null => throw new UnreachableException(),
    };

    public readonly ExpressionTypeUnion Type
    {
        get => ((Expression)Value!).Type;
        set => ((Expression)Value!).Type = value;
    }

    public readonly bool TryGetId(out Token id)
    {
        switch (this)
        {
            case CallExpr { Expression: Primary { TokenSpan.Token: var t } }:
                id = t;
                return true;
            case BinaryExpr { Operator.Token: var t }:
                id = t;
                return true;
            case UnaryExpr { Operator.Token: var t }:
                id = t;
                return true;
            case CallExpr or null:
                id = default;
                return false;
        }
    }
}

sealed class CheckTypeVisitor(Dictionary<(Token, ImmutableArray<ExpressionTypeUnion>), (ExpressionTypeUnion type, string funcName)> functions) : Visitor
{
    private readonly FrozenDictionary<(Token, ImmutableArray<ExpressionTypeUnion>), (ExpressionTypeUnion type, string funcName)> _functionsSignature = [with(new FunctionEquality()), ..functions];
    private readonly FrozenSet<Token> _functionsName = [..functions.Select(f => f.Key.Item1)];
    private readonly FrozenSet<(Token, int)> _functionsArgsCount = [..functions.Select(f => (f.Key.Item1, f.Key.Item2.Length))];

    sealed class FunctionEquality : IEqualityComparer<(Token, ImmutableArray<ExpressionTypeUnion>)>
    {
        public bool Equals((Token, ImmutableArray<ExpressionTypeUnion>) x, (Token, ImmutableArray<ExpressionTypeUnion>) y)
        => x.Item1 == y.Item1 && x.Item2.SequenceEqual(y.Item2);

        public int GetHashCode([DisallowNull] (Token, ImmutableArray<ExpressionTypeUnion>) obj)
        => HashCode.Combine(obj.Item1, ((IStructuralEquatable)obj.Item2).GetHashCode(new ImmutableArrayEquality()));

        sealed class ImmutableArrayEquality : System.Collections.IEqualityComparer
        {
            public new bool Equals(object? x, object? y)
            => (x, y) is (ExpressionTypeUnion l, ExpressionTypeUnion r) ? l == r : throw new Exception();

            public int GetHashCode(object obj)
            => obj is ExpressionTypeUnion t ? t.GetHashCode() : throw new Exception();
        }
    }

    public override void Visit(Primary primary)
    {
        primary.Type = primary.TokenSpan.Token switch
        {
            Token.Id { Value: "true" or "false" } => ExpressionType.Bool,
            Token.String => ExpressionType.String,
            Token.Int => ExpressionType.Int,
            Token.Id id => _functionsName.Contains(id) ? GetFunctionCandidates(id).ToImmutableArray() : ExpressionType.Unknown,
            _ => throw new UnreachableException(),
        };

        IEnumerable<FunctionSignature> GetFunctionCandidates(Token.Id id)
        {
            foreach (var ((name, args), (type, _)) in _functionsSignature)
                if (name == id)
                    yield return new(type, args);
        }
    }

    public override void Exit(ExpressionStatement expressionStatement)
    {
        if (expressionStatement.Expression.Type is not ExpressionType.None)
            throw new InvalidExpressionType(expressionStatement.Expression, ExpressionType.None);
    }

    private void EnterCall(Call call)
    {
        if (call.TryGetId(out var id))
        {
            if (!_functionsName.Contains(id))
                throw new InexistantFunctionException(call);
            if (!_functionsArgsCount.Contains((id, call.ArgsLength)))
                throw new InvalidArgsCountException(call, [.. _functionsArgsCount.Where(f => f.Item1 == id).Select(f => f.Item2)]);
        }
    }

    private void ExitCall(Call call)
    {
        if (call.TryGetId(out var id))
            if (_functionsSignature.TryGetValue((id, [.. call.Args.Select(a => a.Type)]), out var type))
                call.Type = type.type;
            else
                throw new InvalidArgumentTypeException(call, [.._functionsSignature.Where(f => f.Key.Item1 == id && f.Key.Item2.Length == call.ArgsLength).Select(f => f.Key.Item2)]);
    }

    public override void Enter(PrefixExpr prefixExpr)
    => EnterCall(prefixExpr);

    public override void Exit(PrefixExpr prefixExpr)
    => ExitCall(prefixExpr);

    public override void Enter(BinaryExpr binaryExpr)
    {
        EnterCall(binaryExpr);
        if (binaryExpr.Operator.Token is Token.Symbol id)
            binaryExpr.Signatures = _functionsName.Contains(id) ? [..GetFunctionCandidates(id, 2)] : throw new UnreachableException();

        IEnumerable<FunctionSignature> GetFunctionCandidates(Token.Symbol id, int argsCount)
        {
            foreach (var ((name, args), (type, _)) in _functionsSignature)
                if (argsCount == args.Length && name == id)
                    yield return new(type, args);
        }
    }

    public override void Exit(BinaryExpr binaryExpr)
    {
        if (binaryExpr.Signatures is not ImmutableArray<FunctionSignature> { Length: > 0 } sig )
            throw new InvalidExpressionType(binaryExpr, ExpressionType.Function);
        binaryExpr.Signatures = [..sig.Where(s => s.IsCompatibleWith([binaryExpr.Left, binaryExpr.Right]))];
        // ExitCall(binaryExpr);
        if (binaryExpr.Type is not null)
            return;
        if (binaryExpr.Signatures is [var func])
            binaryExpr.Type = func.Return;
        else
            throw new InvalidArgumentTypeException(binaryExpr, [..sig.Select(s => s.Args)]);
    }

    public override void Enter(CallExpr callExpr)
    => EnterCall(callExpr);

    public override void Exit(CallExpr callExpr)
    {
        if (callExpr.Expression.Type is not ImmutableArray<FunctionSignature> { Length: > 0 } sig )
            throw new InvalidExpressionType(callExpr.Expression, ExpressionType.Function);
        sig = [..sig.Where(s => s.Args.Length == callExpr.Args.Length)];
        callExpr.Expression.Type = sig.Where(s => s.IsCompatibleWith(callExpr.Args)).ToImmutableArray();
        ExitCall(callExpr);
        if (callExpr.Type is not null)
            return;
        if (callExpr.Expression.Type is ImmutableArray<FunctionSignature> and [var func])
            callExpr.Type = func.Return;
        else
            throw new InvalidArgumentTypeException(callExpr, [..sig.Select(s => s.Args)]);

        BackPropagateType(callExpr.Expression);

        static void BackPropagateType(Expression expr)
        {
            switch (expr)
            {
                case TernaryExpr ternary:
                    ternary.Center.Type = ternary.Right.Type = expr.Type;
                    BackPropagateType(ternary.Center);
                    BackPropagateType(ternary.Right);
                    break;
                case Primary primary:
                    primary.Type = expr.Type;
                    break;
            }
        }
    }

    public override void Exit(TernaryExpr ternaryExpr)
    {
        switch (ternaryExpr.OpLeft.Token, ternaryExpr.OpRight.Token)
        {
            case (Token.Symbol { Value: "?" }, Token.Symbol { Value: ":" }):
                if (ternaryExpr.Left.Type is not ExpressionType.Bool)
                    throw new InvalidExpressionType(ternaryExpr.Left, ExpressionType.Bool);
                if (ternaryExpr.Center.Type is ImmutableArray<FunctionSignature> { Length: > 0 } lhs && ternaryExpr.Right.Type is ImmutableArray<FunctionSignature> { Length: > 0 } rhs && FunctionSignature.CommonSignature(lhs, rhs) is { Length: > 0 } commons)
                    ternaryExpr.Type = commons;
                else if (ternaryExpr.Center.Type == ternaryExpr.Right.Type)
                    ternaryExpr.Type = ternaryExpr.Center.Type;
                else
                    throw new InvalidExpressionType(ternaryExpr.Right, ternaryExpr.Center.Type);
                break;
        }
    }
}
