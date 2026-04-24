using System.Text;

namespace RecursiveParsing;

public abstract record class BinaryNode(ExpressionNode Left, ExpressionNode Right, NodePrecedence Precedence) : ExpressionNode(Left.Span.Start..Right.Span.End, Precedence)
{
    public override void PrintTree(ReadOnlySpan<char> input, int indentation)
    {
        PrintTreeImpl(input, indentation, isTerminal: false);
        Left.PrintTree(input, indentation + 1);
        Right.PrintTree(input, indentation + 1);
    }
}

public sealed record class Add(ExpressionNode Left, ExpressionNode Right) : BinaryNode(Left, Right, NodePrecedence.Additive)
{
    public override RTObject Evaluate(Context ctx)
    => (Left.Evaluate(ctx), Right.Evaluate(ctx)) switch
    {
        (decimal l, decimal r) => l + r,
        (string l, string r) => l + r,
        _ => throw new RunTimeException(),
    };

    public override void Print(StringBuilder sb)
    {
        if (Left.Precedence < Precedence)
            sb.Append('(');
        Left.Print(sb);
        if (Left.Precedence < Precedence)
            sb.Append(')');
        sb.Append(" + ");
        if (Right.Precedence < Precedence)
            sb.Append('(');
        Right.Print(sb);
        if (Right.Precedence < Precedence)
            sb.Append(')');
    }
}

public sealed record class Substract(ExpressionNode Left, ExpressionNode Right) : BinaryNode(Left, Right, NodePrecedence.Additive)
{
    public override RTObject Evaluate(Context ctx)
    => (Left.Evaluate(ctx), Right.Evaluate(ctx)) switch
    {
        (decimal l, decimal r) => l - r,
        _ => throw new RunTimeException(),
    };

    public override void Print(StringBuilder sb)
    {
        if (Left.Precedence < Precedence)
            sb.Append('(');
        Left.Print(sb);
        if (Left.Precedence < Precedence)
            sb.Append(')');
        sb.Append(" - ");
        if (Right.Precedence < Precedence)
            sb.Append('(');
        Right.Print(sb);
        if (Right.Precedence < Precedence)
            sb.Append(')');
    }
}

public sealed record class Multiply(ExpressionNode Left, ExpressionNode Right) : BinaryNode(Left, Right, NodePrecedence.Term)
{
    public override RTObject Evaluate(Context ctx)
    => (Left.Evaluate(ctx), Right.Evaluate(ctx)) switch
    {
        (decimal l, decimal r) => l * r,
        _ => throw new RunTimeException(),
    };

    public override void Print(StringBuilder sb)
    {
        if (Left.Precedence < Precedence)
            sb.Append('(');
        Left.Print(sb);
        if (Left.Precedence < Precedence)
            sb.Append(')');
        sb.Append(" * ");
        if (Right.Precedence < Precedence)
            sb.Append('(');
        Right.Print(sb);
        if (Right.Precedence < Precedence)
            sb.Append(')');
    }
}

public sealed record class Divide(ExpressionNode Left, ExpressionNode Right) : BinaryNode(Left, Right, NodePrecedence.Term)
{
    public override RTObject Evaluate(Context ctx)
    => (Left.Evaluate(ctx), Right.Evaluate(ctx)) switch
    {
        (decimal l, decimal r) => l / r,
        _ => throw new RunTimeException(),
    };

    public override void Print(StringBuilder sb)
    {
        if (Left.Precedence < Precedence)
            sb.Append('(');
        Left.Print(sb);
        if (Left.Precedence < Precedence)
            sb.Append(')');
        sb.Append(" / ");
        if (Right.Precedence < Precedence)
            sb.Append('(');
        Right.Print(sb);
        if (Right.Precedence < Precedence)
            sb.Append(')');
    }
}

public sealed record class Power(ExpressionNode Left, ExpressionNode Right) : BinaryNode(Left, Right, NodePrecedence.Exponentiation)
{
    public override RTObject Evaluate(Context ctx)
    => (Left.Evaluate(ctx), Right.Evaluate(ctx)) switch
    {
        (decimal l, decimal r) => (decimal)double.Pow((double)l, (double)r),
        _ => throw new RunTimeException(),
    };

    public override void Print(StringBuilder sb)
    {
        if (Left.Precedence <= Precedence)
            sb.Append('(');
        Left.Print(sb);
        if (Left.Precedence <= Precedence)
            sb.Append(')');
        sb.Append(" ^ ");
        if (Right.Precedence < Precedence)
            sb.Append('(');
        Right.Print(sb);
        if (Right.Precedence < Precedence)
            sb.Append(')');
    }
}

public sealed record class Equal(ExpressionNode Left, ExpressionNode Right) : BinaryNode(Left, Right, NodePrecedence.Equation)
{
    public override RTObject Evaluate(Context ctx)
    => (Left.Evaluate(ctx), Right.Evaluate(ctx)) switch
    {
        (decimal l, decimal r) => l == r,
        (bool l, bool r) => l == r,
        (string l, string r) => l == r,
        (Delegate l, Delegate r) => l == r,
        _ => throw new RunTimeException(),
    };

    public override void Print(StringBuilder sb)
    {
        if (Left.Precedence < Precedence)
            sb.Append('(');
        Left.Print(sb);
        if (Left.Precedence < Precedence)
            sb.Append(')');
        sb.Append(" == ");
        if (Right.Precedence < Precedence)
            sb.Append('(');
        Right.Print(sb);
        if (Right.Precedence < Precedence)
            sb.Append(')');
    }
}

public sealed record class NotEqual(ExpressionNode Left, ExpressionNode Right) : BinaryNode(Left, Right, NodePrecedence.Equation)
{
    public override RTObject Evaluate(Context ctx)
    => (Left.Evaluate(ctx), Right.Evaluate(ctx)) switch
    {
        (decimal l, decimal r) => l != r,
        (bool l, bool r) => l != r,
        (string l, string r) => l != r,
        (Delegate l, Delegate r) => l != r,
        _ => throw new RunTimeException(),
    };

    public override void Print(StringBuilder sb)
    {
        if (Left.Precedence < Precedence)
            sb.Append('(');
        Left.Print(sb);
        if (Left.Precedence < Precedence)
            sb.Append(')');
        sb.Append(" != ");
        if (Right.Precedence < Precedence)
            sb.Append('(');
        Right.Print(sb);
        if (Right.Precedence < Precedence)
            sb.Append(')');
    }
}

public sealed record class LessThan(ExpressionNode Left, ExpressionNode Right) : BinaryNode(Left, Right, NodePrecedence.Relational)
{
    public override RTObject Evaluate(Context ctx)
    => (Left.Evaluate(ctx), Right.Evaluate(ctx)) switch
    {
        (decimal l, decimal r) => l < r,
        _ => throw new RunTimeException(),
    };

    public override void Print(StringBuilder sb)
    {
        if (Left.Precedence < Precedence)
            sb.Append('(');
        Left.Print(sb);
        if (Left.Precedence < Precedence)
            sb.Append(')');
        sb.Append(" < ");
        if (Right.Precedence < Precedence)
            sb.Append('(');
        Right.Print(sb);
        if (Right.Precedence < Precedence)
            sb.Append(')');
    }
}

public sealed record class LessThanOrEqual(ExpressionNode Left, ExpressionNode Right) : BinaryNode(Left, Right, NodePrecedence.Relational)
{
    public override RTObject Evaluate(Context ctx)
    => (Left.Evaluate(ctx), Right.Evaluate(ctx)) switch
    {
        (decimal l, decimal r) => l <= r,
        _ => throw new RunTimeException(),
    };

    public override void Print(StringBuilder sb)
    {
        if (Left.Precedence < Precedence)
            sb.Append('(');
        Left.Print(sb);
        if (Left.Precedence < Precedence)
            sb.Append(')');
        sb.Append(" <= ");
        if (Right.Precedence < Precedence)
            sb.Append('(');
        Right.Print(sb);
        if (Right.Precedence < Precedence)
            sb.Append(')');
    }
}

public sealed record class GreaterThan(ExpressionNode Left, ExpressionNode Right) : BinaryNode(Left, Right, NodePrecedence.Relational)
{
    public override RTObject Evaluate(Context ctx)
    => (Left.Evaluate(ctx), Right.Evaluate(ctx)) switch
    {
        (decimal l, decimal r) => l > r,
        _ => throw new RunTimeException(),
    };

    public override void Print(StringBuilder sb)
    {
        if (Left.Precedence < Precedence)
            sb.Append('(');
        Left.Print(sb);
        if (Left.Precedence < Precedence)
            sb.Append(')');
        sb.Append(" > ");
        if (Right.Precedence < Precedence)
            sb.Append('(');
        Right.Print(sb);
        if (Right.Precedence < Precedence)
            sb.Append(')');
    }
}

public sealed record class GreaterThanOrEqual(ExpressionNode Left, ExpressionNode Right) : BinaryNode(Left, Right, NodePrecedence.Relational)
{
    public override RTObject Evaluate(Context ctx)
    => (Left.Evaluate(ctx), Right.Evaluate(ctx)) switch
    {
        (decimal l, decimal r) => l >= r,
        _ => throw new RunTimeException(),
    };

    public override void Print(StringBuilder sb)
    {
        if (Left.Precedence < Precedence)
            sb.Append('(');
        Left.Print(sb);
        if (Left.Precedence < Precedence)
            sb.Append(')');
        sb.Append(" >= ");
        if (Right.Precedence < Precedence)
            sb.Append('(');
        Right.Print(sb);
        if (Right.Precedence < Precedence)
            sb.Append(')');
    }
}
