using System.Collections.Frozen;
using System.Runtime.InteropServices;
using System.Text;

namespace RecursiveParsing;

public abstract class TreeNode
{
    private static readonly Dictionary<int, string> _indent = [];
    public abstract void Print(StringBuilder sb);
    public abstract decimal Evaluate(Context ctx);
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

public class Context(params IEnumerable<KeyValuePair<string, decimal>> variables)
{
    public FrozenDictionary<string, decimal> Variables { get; } = variables.ToFrozenDictionary();
}

public sealed class Number(decimal i) : TreeNode
{
    public decimal I { get; } = i;

    public override decimal Evaluate(Context ctx)
    => I;

    public override void Print(StringBuilder sb)
    => sb.Append(I);

    public override void PrintTree(int indentation)
    => Console.WriteLine($"{IndentSpaces(indentation)}{GetType().Name}: {I}");
}

public sealed class Id(string name) : TreeNode
{
    public string Name { get; } = name;

    public override decimal Evaluate(Context ctx)
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
    public override decimal Evaluate(Context ctx)
    => -Node.Evaluate(ctx);

    public override void Print(StringBuilder sb)
    {
        sb.Append('-');
        Node.Print(sb);
    }
}

public sealed class Factorial(TreeNode node) : UnaryNode(node)
{
    public override decimal Evaluate(Context ctx)
    => F(Node.Evaluate(ctx));

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
    public override decimal Evaluate(Context ctx)
    => Left.Evaluate(ctx) + Right.Evaluate(ctx);

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
    public override decimal Evaluate(Context ctx)
    => Left.Evaluate(ctx) - Right.Evaluate(ctx);

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
    public override decimal Evaluate(Context ctx)
    => Left.Evaluate(ctx) * Right.Evaluate(ctx);

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
    public override decimal Evaluate(Context ctx)
    => Left.Evaluate(ctx) / Right.Evaluate(ctx);

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
    public override decimal Evaluate(Context ctx)
    => (decimal)double.Pow((double)Left.Evaluate(ctx), (double)Right.Evaluate(ctx));

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
    public override decimal Evaluate(Context ctx)
    => Left.Evaluate(ctx) == Right.Evaluate(ctx) ? 1 : 0;

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
    public override decimal Evaluate(Context ctx)
    => Left.Evaluate(ctx) != Right.Evaluate(ctx) ? 1 : 0;

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
    public override decimal Evaluate(Context ctx)
    => Left.Evaluate(ctx) < Right.Evaluate(ctx) ? 1 : 0;

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
    public override decimal Evaluate(Context ctx)
    => Left.Evaluate(ctx) <= Right.Evaluate(ctx) ? 1 : 0;

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
    public override decimal Evaluate(Context ctx)
    => Left.Evaluate(ctx) > Right.Evaluate(ctx) ? 1 : 0;

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
    public override decimal Evaluate(Context ctx)
    => Left.Evaluate(ctx) >= Right.Evaluate(ctx) ? 1 : 0;

    public override void Print(StringBuilder sb)
    {
        sb.Append('(');
        Left.Print(sb);
        sb.Append(">=");
        Right.Print(sb);
        sb.Append(')');
    }
}
