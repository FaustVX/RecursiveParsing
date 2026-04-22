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
        decimal d => d,
        bool b => b,
        Delegate d => d,
        _ => throw new RunTimeException(),
    };
    public override string ToString()
    => Value?.ToString() ?? "null";
}

[Serializable]
public class RunTimeException : Exception
{
    public RunTimeException() { }
    public RunTimeException(string message) : base(message) { }
    public RunTimeException(string message, Exception inner) : base(message, inner) { }
}

public abstract class TreeNode
{
    private static readonly Dictionary<int, string> _indent = [];
    public abstract void Print(StringBuilder sb);
    public abstract Object Evaluate(Context ctx);
    public abstract void PrintTree(int indentation = 0);
    protected string IndentSpaces(int depth)
    {
        ref var indent = ref CollectionsMarshal.GetValueRefOrAddDefault(_indent, depth, out var exists);
        if (exists)
            return indent!;
        var s = (stackalloc char[depth * 2]);
        s.Fill(' ');
        return indent = new string(s);
    }
}

public class Context(params IEnumerable<KeyValuePair<string, Object>> variables)
{
    public FrozenDictionary<string, Object> Variables { get; } = variables.ToFrozenDictionary();
}

public sealed class Number(decimal i) : TreeNode
{
    public decimal I { get; } = i;

    public override Object Evaluate(Context ctx)
    => I;

    public override void Print(StringBuilder sb)
    => sb.Append(I);

    public override void PrintTree(int indentation)
    => Console.WriteLine($"{IndentSpaces(indentation)}{GetType().Name}: {I}");
}

public sealed class Id(string name) : TreeNode
{
    public string Name { get; } = name;

    public override Object Evaluate(Context ctx)
    => ctx.Variables[Name];

    public override void Print(StringBuilder sb)
    => sb.Append(Name);

    public override void PrintTree(int indentation)
    => Console.WriteLine($"{IndentSpaces(indentation)}{GetType().Name}: {Name}");
}

public abstract class UnaryNode(TreeNode node) : TreeNode
{
    public TreeNode Node { get; } = node;

    public override void PrintTree(int indentation)
    {
        Console.WriteLine($"{IndentSpaces(indentation)}{GetType().Name}:");
        Node.PrintTree(indentation + 1);
    }
}

public sealed class Negate(TreeNode node) : UnaryNode(node)
{
    public override Object Evaluate(Context ctx)
    => Node.Evaluate(ctx) is decimal d ? -d : throw new RunTimeException();

    public override void Print(StringBuilder sb)
    {
        sb.Append('-');
        Node.Print(sb);
    }
}

public sealed class Factorial(TreeNode node) : UnaryNode(node)
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
        Node.Print(sb);
        sb.Append('!');
    }
}

public abstract class BinaryNode(TreeNode left, TreeNode right) : TreeNode
{
    public TreeNode Left { get; } = left;
    public TreeNode Right { get; } = right;

    public override void PrintTree(int indentation)
    {
        Console.WriteLine($"{IndentSpaces(indentation)}{GetType().Name}:");
        Left.PrintTree(indentation + 1);
        Right.PrintTree(indentation + 1);
    }
}

public sealed class Add(TreeNode left, TreeNode right) : BinaryNode(left, right)
{
    public override Object Evaluate(Context ctx)
    => (Left.Evaluate(ctx), Right.Evaluate(ctx)) switch
    {
        (decimal l, decimal r) => l + r,
        _ => throw new RunTimeException(),
    };

    public override void Print(StringBuilder sb)
    {
        sb.Append('(');
        Left.Print(sb);
        sb.Append('+');
        Right.Print(sb);
        sb.Append(')');
    }
}

public sealed class Substract(TreeNode left, TreeNode right) : BinaryNode(left, right)
{
    public override Object Evaluate(Context ctx)
    => (Left.Evaluate(ctx), Right.Evaluate(ctx)) switch
    {
        (decimal l, decimal r) => l - r,
        _ => throw new RunTimeException(),
    };

    public override void Print(StringBuilder sb)
    {
        sb.Append('(');
        Left.Print(sb);
        sb.Append('-');
        Right.Print(sb);
        sb.Append(')');
    }
}

public sealed class Multiply(TreeNode left, TreeNode right) : BinaryNode(left, right)
{
    public override Object Evaluate(Context ctx)
    => (Left.Evaluate(ctx), Right.Evaluate(ctx)) switch
    {
        (decimal l, decimal r) => l * r,
        _ => throw new RunTimeException(),
    };

    public override void Print(StringBuilder sb)
    {
        sb.Append('(');
        Left.Print(sb);
        sb.Append('*');
        Right.Print(sb);
        sb.Append(')');
    }
}

public sealed class Divide(TreeNode left, TreeNode right) : BinaryNode(left, right)
{
    public override Object Evaluate(Context ctx)
    => (Left.Evaluate(ctx), Right.Evaluate(ctx)) switch
    {
        (decimal l, decimal r) => l / r,
        _ => throw new RunTimeException(),
    };

    public override void Print(StringBuilder sb)
    {
        sb.Append('(');
        Left.Print(sb);
        sb.Append('/');
        Right.Print(sb);
        sb.Append(')');
    }
}

public sealed class Power(TreeNode left, TreeNode right) : BinaryNode(left, right)
{
    public override Object Evaluate(Context ctx)
    => (Left.Evaluate(ctx), Right.Evaluate(ctx)) switch
    {
        (decimal l, decimal r) => (decimal)double.Pow((double)l, (double)r),
        _ => throw new RunTimeException(),
    };

    public override void Print(StringBuilder sb)
    {
        sb.Append('(');
        Left.Print(sb);
        sb.Append('^');
        Right.Print(sb);
        sb.Append(')');
    }
}

public sealed class Equal(TreeNode left, TreeNode right) : BinaryNode(left, right)
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
        sb.Append('(');
        Left.Print(sb);
        sb.Append("==");
        Right.Print(sb);
        sb.Append(')');
    }
}

public sealed class NotEqual(TreeNode left, TreeNode right) : BinaryNode(left, right)
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
        sb.Append('(');
        Left.Print(sb);
        sb.Append("!=");
        Right.Print(sb);
        sb.Append(')');
    }
}

public sealed class LessThan(TreeNode left, TreeNode right) : BinaryNode(left, right)
{
    public override Object Evaluate(Context ctx)
    => (Left.Evaluate(ctx), Right.Evaluate(ctx)) switch
    {
        (decimal l, decimal r) => l < r,
        _ => throw new RunTimeException(),
    };

    public override void Print(StringBuilder sb)
    {
        sb.Append('(');
        Left.Print(sb);
        sb.Append('<');
        Right.Print(sb);
        sb.Append(')');
    }
}

public sealed class LessThanOrEqual(TreeNode left, TreeNode right) : BinaryNode(left, right)
{
    public override Object Evaluate(Context ctx)
    => (Left.Evaluate(ctx), Right.Evaluate(ctx)) switch
    {
        (decimal l, decimal r) => l <= r,
        _ => throw new RunTimeException(),
    };

    public override void Print(StringBuilder sb)
    {
        sb.Append('(');
        Left.Print(sb);
        sb.Append("<=");
        Right.Print(sb);
        sb.Append(')');
    }
}

public sealed class GreaterThan(TreeNode left, TreeNode right) : BinaryNode(left, right)
{
    public override Object Evaluate(Context ctx)
    => (Left.Evaluate(ctx), Right.Evaluate(ctx)) switch
    {
        (decimal l, decimal r) => l > r,
        _ => throw new RunTimeException(),
    };

    public override void Print(StringBuilder sb)
    {
        sb.Append('(');
        Left.Print(sb);
        sb.Append('>');
        Right.Print(sb);
        sb.Append(')');
    }
}

public sealed class GreaterThanOrEqual(TreeNode left, TreeNode right) : BinaryNode(left, right)
{
    public override Object Evaluate(Context ctx)
    => (Left.Evaluate(ctx), Right.Evaluate(ctx)) switch
    {
        (decimal l, decimal r) => l >= r,
        _ => throw new RunTimeException(),
    };

    public override void Print(StringBuilder sb)
    {
        sb.Append('(');
        Left.Print(sb);
        sb.Append(">=");
        Right.Print(sb);
        sb.Append(')');
    }
}

public sealed class Invocation(TreeNode function, ImmutableArray<TreeNode> args) : TreeNode
{
    public TreeNode Function { get; } = function;
    public ImmutableArray<TreeNode> Args { get; } = args;

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
        Function.Print(sb);
        sb.Append('(');
        for (int i = 0; i < Args.Length; i++)
        {
            if (i > 0) sb.Append(',');
            Args[i].Print(sb);
        }
        sb.Append(')');
    }

    public override void PrintTree(int indentation)
    {
        Console.WriteLine($"{IndentSpaces(indentation)}{GetType().Name}:");
        Function.PrintTree(indentation + 1);
        foreach (var arg in Args)
            arg.PrintTree(indentation + 1);
    }
}
