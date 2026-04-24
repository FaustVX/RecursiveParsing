using System.Collections.Immutable;
using System.Text;

namespace RecursiveParsing;

public enum NodePrecedence
{
    Expression = 1, // not 0 to avoid implicit conversion from 0
    Conditionnal = Expression,
    Equation,
    Relational,
    Additive,
    Term,
    Unary,
    Exponentiation,
    Postfix,
    Primary,
}

public abstract record class ExpressionNode(Range Span, NodePrecedence Precedence) : TreeNode(Span)
{
    public abstract RTObject Evaluate(Context ctx);
}

public sealed record class Number(decimal I, Range Span) : ExpressionNode(Span, NodePrecedence.Unary)
{
    public override RTObject Evaluate(Context ctx)
    => I;

    public override void Print(StringBuilder sb)
    => sb.Append(I);

    public override void PrintTree(ReadOnlySpan<char> input, int indentation)
    => PrintTreeImpl(input, indentation, isTerminal: true);
}

public sealed record class Id(string Name, Range Span) : ExpressionNode(Span, NodePrecedence.Unary)
{
    public override RTObject Evaluate(Context ctx)
    => ctx.Variables[Name];

    public override void Print(StringBuilder sb)
    => sb.Append(Name);

    public override void PrintTree(ReadOnlySpan<char> input, int indentation)
    => PrintTreeImpl(input, indentation, isTerminal: true);
}

public sealed record class Invocation(ExpressionNode Function, ImmutableArray<ExpressionNode> Args, Range Span) : ExpressionNode(Span, NodePrecedence.Postfix)
{
    public override RTObject Evaluate(Context ctx)
    {
        if (Function.Evaluate(ctx) is not Delegate func)
            throw new RunTimeException();
        var evaluatedArgs = Args.Select(arg => arg.Evaluate(ctx).Value).ToArray();
        var result = func.DynamicInvoke(evaluatedArgs)!;
        return RTObject.FromObject(result);
    }

    public override void Print(StringBuilder sb)
    {
        if (Function.Precedence < Precedence)
            sb.Append('(');
        Function.Print(sb);
        if (Function.Precedence < Precedence)
            sb.Append(')');
        sb.Append('(');
        for (var i = 0; i < Args.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            Args[i].Print(sb);
        }
        sb.Append(')');
    }

    public override void PrintTree(ReadOnlySpan<char> input, int indentation)
    {
        PrintTreeImpl(input, indentation, isTerminal: false);
        Function.PrintTree(input, indentation + 1);
        foreach (var arg in Args)
            arg.PrintTree(input, indentation + 1);
    }
}
