using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text;

namespace RecursiveParsing;

public readonly union Object(decimal, bool, Delegate)
{
    public static Object FromObject(object obj)
    => obj switch
    {
        Object o => o,
        int i => i,
        float f => (decimal)f,
        double d => (decimal)d,
        decimal d => d,
        bool b => b,
        Delegate d => d,
        _ => throw new RunTimeException(),
    };
    public override string ToString()
    => Value?.ToString() ?? "null";
}

[Serializable]
public class RunTimeException() : Exception;

public enum NodePrecedence
{
    Expression = 1, // not 0 to avoid implicit conversion from 0
    Conditionnal,
    Relational,
    Additive,
    Term,
    Unary,
    Exponentiation,
    Postfix,
    Primary,
}

public abstract record class TreeNode(Range Span, NodePrecedence Precedence)
{
    private static readonly Dictionary<int, string> _indent = [];
    public abstract void Print(StringBuilder sb);
    public abstract Object Evaluate(Context ctx);
    public abstract void PrintTree(ReadOnlySpan<char> input, int indentation = 0);
    protected string IndentSpaces(int depth)
    {
        ref var indent = ref CollectionsMarshal.GetValueRefOrAddDefault(_indent, depth, out var exists);
        if (exists)
            return indent!;
        var s = (stackalloc char[depth * 2]);
        s.Fill(' ');
        return indent = new string(s);
    }
    protected void PrintTreeImpl(ReadOnlySpan<char> input, int indentation)
    => Console.WriteLine($"{IndentSpaces(indentation)}{GetType().Name} = [{Span}]{input[Span]}:");
}

public class Context(params IEnumerable<KeyValuePair<string, Object>> variables)
{
    public FrozenDictionary<string, Object> Variables { get; } = variables.ToFrozenDictionary();
}

public sealed record class Number(decimal I, Range Span, NodePrecedence Precedence) : TreeNode(Span, Precedence)
{
    public override Object Evaluate(Context ctx)
    => I;

    public override void Print(StringBuilder sb)
    => sb.Append(I);

    public override void PrintTree(ReadOnlySpan<char> input, int indentation)
    => PrintTreeImpl(input, indentation);
}

public sealed record class Id(string Name, Range Span, NodePrecedence Precedence) : TreeNode(Span, Precedence)
{
    public override Object Evaluate(Context ctx)
    => ctx.Variables[Name];

    public override void Print(StringBuilder sb)
    => sb.Append(Name);

    public override void PrintTree(ReadOnlySpan<char> input, int indentation)
    => PrintTreeImpl(input, indentation);
}

public sealed record class Invocation(TreeNode Function, ImmutableArray<TreeNode> Args, Range Span, NodePrecedence Precedence) : TreeNode(Span, Precedence)
{
    public override Object Evaluate(Context ctx)
    {
        if (Function.Evaluate(ctx) is not Delegate func)
            throw new RunTimeException();
        var evaluatedArgs = Args.Select(arg => arg.Evaluate(ctx).Value).ToArray();
        var result = func.DynamicInvoke(evaluatedArgs)!;
        return Object.FromObject(result);
    }

    public override void Print(StringBuilder sb)
    {
        if (Function.Precedence < Precedence)
            sb.Append('(');
        Function.Print(sb);
        if (Function.Precedence < Precedence)
            sb.Append(')');
        sb.Append('(');
        for (int i = 0; i < Args.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            Args[i].Print(sb);
        }
        sb.Append(')');
    }

    public override void PrintTree(ReadOnlySpan<char> input, int indentation)
    {
        PrintTreeImpl(input, indentation);
        Function.PrintTree(input, indentation + 1);
        foreach (var arg in Args)
            arg.PrintTree(input, indentation + 1);
    }
}

public abstract record class UnaryNode(TreeNode Node, Range Span, NodePrecedence Precedence) : TreeNode(Span, Precedence)
{
    public override void PrintTree(ReadOnlySpan<char> input, int indentation)
    {
        PrintTreeImpl(input, indentation);
        Node.PrintTree(input, indentation + 1);
    }
}

public sealed record class Negate(TreeNode Node, Range Span, NodePrecedence Precedence) : UnaryNode(Node, Span, Precedence)
{
    public override Object Evaluate(Context ctx)
    => Node.Evaluate(ctx) is decimal d ? -d : throw new RunTimeException();

    public override void Print(StringBuilder sb)
    {
        sb.Append('-');
        if (Node.Precedence < Precedence)
            sb.Append('(');
        Node.Print(sb);
        if (Node.Precedence < Precedence)
            sb.Append(')');
    }
}

public sealed record class Factorial(TreeNode Node, Range Span, NodePrecedence Precedence) : UnaryNode(Node, Span, Precedence)
{
    public override Object Evaluate(Context ctx)
    => F(Node.Evaluate(ctx) is decimal d ? d : throw new RunTimeException());

    static decimal F(decimal a)
    => a switch
    {
        <= 1 => 1,
        _ => a * F(a - 1),
    };

    public override void Print(StringBuilder sb)
    {
        if (Node.Precedence < Precedence)
            sb.Append('(');
        Node.Print(sb);
        if (Node.Precedence < Precedence)
            sb.Append(')');
        sb.Append('!');
    }
}

public abstract record class BinaryNode(TreeNode Left, TreeNode Right, NodePrecedence Precedence) : TreeNode(Left.Span.Start..Right.Span.End, Precedence)
{
    public override void PrintTree(ReadOnlySpan<char> input, int indentation)
    {
        PrintTreeImpl(input, indentation);
        Left.PrintTree(input, indentation + 1);
        Right.PrintTree(input, indentation + 1);
    }
}

public sealed record class Add(TreeNode Left, TreeNode Right, NodePrecedence Precedence) : BinaryNode(Left, Right, Precedence)
{
    public override Object Evaluate(Context ctx)
    => (Left.Evaluate(ctx), Right.Evaluate(ctx)) switch
    {
        (decimal l, decimal r) => l + r,
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

public sealed record class Substract(TreeNode Left, TreeNode Right, NodePrecedence Precedence) : BinaryNode(Left, Right, Precedence)
{
    public override Object Evaluate(Context ctx)
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

public sealed record class Multiply(TreeNode Left, TreeNode Right, NodePrecedence Precedence) : BinaryNode(Left, Right, Precedence)
{
    public override Object Evaluate(Context ctx)
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

public sealed record class Divide(TreeNode Left, TreeNode Right, NodePrecedence Precedence) : BinaryNode(Left, Right, Precedence)
{
    public override Object Evaluate(Context ctx)
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

public sealed record class Power(TreeNode Left, TreeNode Right, NodePrecedence Precedence) : BinaryNode(Left, Right, Precedence)
{
    public override Object Evaluate(Context ctx)
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

public sealed record class Equal(TreeNode Left, TreeNode Right, NodePrecedence Precedence) : BinaryNode(Left, Right, Precedence)
{
    public override Object Evaluate(Context ctx)
    => (Left.Evaluate(ctx), Right.Evaluate(ctx)) switch
    {
        (decimal l, decimal r) => l == r,
        (bool l, bool r) => l == r,
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

public sealed record class NotEqual(TreeNode Left, TreeNode Right, NodePrecedence Precedence) : BinaryNode(Left, Right, Precedence)
{
    public override Object Evaluate(Context ctx)
    => (Left.Evaluate(ctx), Right.Evaluate(ctx)) switch
    {
        (decimal l, decimal r) => l != r,
        (bool l, bool r) => l != r,
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

public sealed record class LessThan(TreeNode Left, TreeNode Right, NodePrecedence Precedence) : BinaryNode(Left, Right, Precedence)
{
    public override Object Evaluate(Context ctx)
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

public sealed record class LessThanOrEqual(TreeNode Left, TreeNode Right, NodePrecedence Precedence) : BinaryNode(Left, Right, Precedence)
{
    public override Object Evaluate(Context ctx)
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

public sealed record class GreaterThan(TreeNode Left, TreeNode Right, NodePrecedence Precedence) : BinaryNode(Left, Right, Precedence)
{
    public override Object Evaluate(Context ctx)
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

public sealed record class GreaterThanOrEqual(TreeNode Left, TreeNode Right, NodePrecedence Precedence) : BinaryNode(Left, Right, Precedence)
{
    public override Object Evaluate(Context ctx)
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

public abstract record class TernaryNode(TreeNode Left, TreeNode Middle, TreeNode Right, NodePrecedence Precedence) : TreeNode(Left.Span.Start..Right.Span.End, Precedence)
{
    public override void PrintTree(ReadOnlySpan<char> input, int indentation)
    {
        PrintTreeImpl(input, indentation);
        Left.PrintTree(input, indentation + 1);
        Middle.PrintTree(input, indentation + 1);
        Right.PrintTree(input, indentation + 1);
    }
}

public sealed record class Conditionnal(TreeNode Condition, TreeNode True, TreeNode False, NodePrecedence Precedence) : TernaryNode(Condition, True, False, Precedence)
{
    public override Object Evaluate(Context ctx)
    => Left.Evaluate(ctx) is bool b
        ? (b ? Middle : Right).Evaluate(ctx)
        : throw new RunTimeException();

    public override void Print(StringBuilder sb)
    {
        if (Left.Precedence <= Precedence)
            sb.Append('(');
        Left.Print(sb);
        if (Left.Precedence <= Precedence)
            sb.Append(')');
        sb.Append(" ? ");
        if (Middle.Precedence < Precedence)
            sb.Append('(');
        Middle.Print(sb);
        if (Middle.Precedence < Precedence)
            sb.Append(')');
        sb.Append(" : ");
        if (Right.Precedence < Precedence)
            sb.Append('(');
        Right.Print(sb);
        if (Right.Precedence < Precedence)
            sb.Append(')');
    }
}
